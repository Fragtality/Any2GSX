using Any2GSX.AppConfig;
using Any2GSX.GSX.Automation;
using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using CFIT.AppTools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Any2GSX.Aircraft
{
    public class Flightplan : IFlightplan
    {
        public virtual Config Config => AppService.Instance.Config;
        public virtual AircraftBase Aircraft => AppService.Instance.AircraftController.Aircraft;
        public virtual GsxAutomationController AutomationController => AppService.Instance.GsxController.AutomationController;
        public virtual SettingProfile SettingProfile => AppService.Instance.SettingProfile;
        public virtual IWeightConverter WeightConverter => AppService.Instance.WeightConverter;
        public virtual CancellationToken Token => AppService.Instance.Token;
        protected virtual HttpClient HttpClient { get; }

        public virtual long Id { get; protected set; } = 0;
        public virtual long LastId { get; protected set; } = 0;
        public virtual bool IsLoaded => Id != 0 && !string.IsNullOrWhiteSpace(Number);
        public virtual bool LastOnlineCheck { get; protected set; } = false;
        public virtual string SimbriefUser => AppService.Instance.Config.SimbriefUser;
        public virtual DisplayUnit Unit { get; set; } = AppService.Instance.Config.DisplayUnitDefault;
        public virtual double WeightPerPaxKg { get; set; }
        public virtual double WeightPerBagKg { get; set; }
        public virtual string Number { get; set; }
        public virtual string AircraftType { get; set; }
        public virtual string AircraftReg { get; set; }
        public virtual string Origin { get; set; }
        public virtual string Destination { get; set; }
        public virtual TimeSpan Duration { get; set; }
        public virtual DateTime ScheduledOutTime { get; protected set; } = DateTime.MinValue;
        public virtual double FuelRampKg { get; set; }
        public virtual int CountPaxPlanned { get; set; }
        public virtual int CountPax => CountPaxPlanned + DiffPax;
        public virtual int MaxPax { get; set; }
        public virtual int DiffPax { get; set; }
        public virtual int DiffBags { get; set; }
        public virtual int CountBagsPlanned { get; set; }
        public virtual int CountBags => CountBagsPlanned + DiffBags;
        public virtual double WeightPaxKg => (CountPax) * WeightPerPaxKg;
        public virtual double WeightBagKg => (CountBags) * WeightPerBagKg;
        public virtual double WeightFreightKg { get; set; }
        public virtual double WeightCargoKg => WeightBagKg + WeightFreightKg + (SettingProfile.ApplyPaxToCargo && Aircraft.GetIsCargo().RunSync() ? WeightPaxKg : 0);
        public virtual double WeightCargoPlannedKg => (CountBagsPlanned * WeightPerBagKg) + WeightFreightKg + (SettingProfile.ApplyPaxToCargo && Aircraft.GetIsCargo().RunSync() ? WeightPaxKg : 0);
        public virtual double WeightPayloadKg => WeightPaxKg + WeightCargoKg;
        public virtual double AircraftEmptyOewKg { get; set; }
        public virtual double ZeroFuelRampKg => AircraftEmptyOewKg + WeightPayloadKg;
        public virtual double WeightTotalRampKg => AircraftEmptyOewKg + WeightPayloadKg + FuelRampKg;
        public virtual double DiffPayloadKg => (DiffPax * WeightPerPaxKg) + (DiffBags * WeightPerBagKg);

        public event Func<IFlightplan, Task> OnImport;
        public event Func<Task> OnUnload;

        public Flightplan()
        {
            HttpClient = new()
            {
                BaseAddress = new(Config.SimbriefUrlBase),
                Timeout = TimeSpan.FromMilliseconds(Config.HttpRequestTimeoutMs)
            };
            HttpClient.DefaultRequestHeaders.Accept.Clear();
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        protected virtual async Task<JsonNode> GetJsonNode()
        {
            if (long.TryParse(SimbriefUser, out _))
            {
                Logger.Verbose($"Requesting SimBrief (via Userid) ...");
                return JsonNode.Parse(await HttpClient.GetStringAsync(string.Format(Config.SimbriefUrlPathId, SimbriefUser), Token));
            }
            else
            {
                Logger.Verbose($"Requesting SimBrief (via Username) ...");
                return JsonNode.Parse(await HttpClient.GetStringAsync(string.Format(Config.SimbriefUrlPathName, SimbriefUser), Token));
            }
        }

        protected virtual bool GetJsonInt(JsonNode node, out int value)
        {
            value = 0;
            if (node!.GetValueKind() == System.Text.Json.JsonValueKind.String && int.TryParse(node!.GetValue<string>(), out value))
                return true;
            else
                return false;
        }

        protected virtual bool GetJsonLong(JsonNode node, out long value)
        {
            value = 0;
            if (node!.GetValueKind() == System.Text.Json.JsonValueKind.String && long.TryParse(node!.GetValue<string>(), out value))
                return true;
            else
                return false;
        }

        protected virtual bool GetJsonDouble(JsonNode node, out double value)
        {
            value = 0;
            if (node!.GetValueKind() == System.Text.Json.JsonValueKind.String && double.TryParse(node!.GetValue<string>(), CultureInfo.InvariantCulture, out value))
                return true;
            else
                return false;
        }

        protected virtual bool GetJsonString(JsonNode node, out string value)
        {
            value = "";
            if (node!.GetValueKind() == System.Text.Json.JsonValueKind.String)
            {
                value = node!.GetValue<string>();
                return true;
            }
            else
                return false;
        }

        public virtual async Task<long> CheckIdOnline()
        {
            try
            {
                var json = await GetJsonNode();
                if (GetJsonLong(json["params"]!["request_id"], out long id))
                    return id;
            }
            catch
            {
                Logger.Warning($"Error while fetching SimBrief OFP ID");
            }

            return 0;
        }

        public virtual async Task<bool> CheckNewOfp()
        {
            long id = await CheckIdOnline();
            LastOnlineCheck = id > 0 && id != LastId;
            Logger.Debug($"Simbrief OFP ID checked: {LastOnlineCheck} (id: {id} | last: {LastId})");
            return LastOnlineCheck;
        }

        public virtual async Task<bool> Import(bool notify = true)
        {
            try
            {
                if (IsLoaded)
                {
                    Logger.Debug($"No Import - OFP already Loaded! ({Id})");
                    return true;
                }

                Logger.Information($"Importing OFP from SimBrief ...");
                var json = await GetJsonNode();

                if (GetJsonLong(json["params"]!["request_id"], out long id) && id == LastId)
                {
                    Logger.Information($"No Import - same OFP ID: {id}");
                    return false;
                }

                if (GetJsonString(json["params"]!["units"], out string units))
                {
                    if (units == "kgs")
                        Unit = DisplayUnit.KG;
                    else if (units == "lbs")
                        Unit = DisplayUnit.LB;
                    else
                        return false;
                }
                else
                    return false;

                if (GetJsonString(json["times"]!["sched_block"], out string blockTime))
                    Duration = TimeSpan.Parse(blockTime);
                else
                    return false;

                if (GetJsonString(json["times"]!["sched_out"], out string estOut))
                    ScheduledOutTime = DateTime.Parse(estOut).ToUniversalTime();
                else
                    return false;

                if (GetJsonDouble(json["weights"]!["pax_weight"], out double paxWeight))
                    WeightPerPaxKg = ToKg(paxWeight);
                else
                    return false;

                if (GetJsonDouble(json["weights"]!["bag_weight"], out double bagWeight))
                    WeightPerBagKg = ToKg(bagWeight);
                else
                    return false;

                if (GetJsonInt(json["weights"]!["pax_count"], out int pax))
                {
                    DiffPax = 0;
                    CountPaxPlanned = pax;
                }
                else
                    return false;

                if (GetJsonInt(json["weights"]!["bag_count"], out int bag))
                {
                    DiffBags = 0;
                    CountBagsPlanned = bag;
                }
                else
                    return false;

                if (GetJsonDouble(json["weights"]!["oew"], out double oew))
                    AircraftEmptyOewKg = ToKg(oew);
                else
                    return false;

                if (GetJsonDouble(json["fuel"]!["plan_ramp"], out double fuel))
                {
                    if (SettingProfile.FuelRoundUp100)
                    {
                        fuel = RoundUp100(fuel);
                        Logger.Debug($"Planned Fuel Round Up: {fuel}{json["params"]!["units"]}");
                    }
                    FuelRampKg = ToKg(fuel);
                }
                else
                    return false;

                if (GetJsonDouble(json["weights"]!["freight_added"], out double freight))
                    WeightFreightKg = ToKg(freight);
                else
                    return false;

                if (GetJsonString(json["general"]!["icao_airline"], out string airline) && GetJsonString(json["general"]!["flight_number"], out string number))
                    Number = $"{airline}{number}";
                else
                    return false;

                if (GetJsonString(json["origin"]!["icao_code"], out string origin) && GetJsonString(json["destination"]!["icao_code"], out string destination))
                {
                    Origin = origin;
                    Destination = destination;
                }
                else
                    return false;

                if (GetJsonString(json["aircraft"]!["icao_code"], out string aircraftType))
                    AircraftType = aircraftType;
                else
                    return false;

                if (GetJsonString(json["aircraft"]!["reg"], out string aircraftReg))
                    AircraftReg = aircraftReg;
                else
                    return false;

                if (GetJsonInt(json["aircraft"]!["max_passengers"], out int maxPax) && maxPax > 0)
                    MaxPax = maxPax;
                else
                {
                    MaxPax = -1;
                    Logger.Warning("Maximum Passengers could not be parsed from SimBrief");
                }

                Id = id;

                if (SettingProfile.RandomizePax && !AutomationController.IsCargo)
                {
                    DiffPax = Random.Shared.Next(SettingProfile.RandomizePaxMaxDiff * -1, SettingProfile.RandomizePaxMaxDiff);
                    if (MaxPax > 0 && CountPaxPlanned + DiffPax > MaxPax)
                        DiffPax = MaxPax - CountPaxPlanned;
                    DiffBags = DiffPax;

                    if (CountPax + DiffPax > 0)
                    {
                        Logger.Debug($"Randomized Pax Count - Diff: {DiffPax}");
                    }
                }

                if (Config.DisplayUnitSource == DisplayUnitSource.Simbrief && Config.DisplayUnitCurrent != Unit)
                {
                    Logger.Debug($"Switching DisplayUnit to Simbrief Source");
                    Config.SetDisplayUnit(Unit);
                }

                if (notify)
                    _ = TaskTools.RunPool(() => OnImport?.Invoke(this));

                Logger.Information($"Flightplan imported from SimBrief:");
                var strings = GetInfoStrings();
                foreach (string str in strings)
                    Logger.Information(str);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            return false;
        }

        public virtual double RoundUp100(double value)
        {
            return Math.Ceiling(value / 100.0) * 100.0;
        }

        protected const int col0 = -20;
        protected const int colS = -20;
        protected const int colN = -9;
        public virtual string[] GetInfoStrings()
        {
            if (Id == 0)
                return ["No OFP imported"];
            List<string> infoStrings = [];

            infoStrings.Add($"{"OFP ID",col0}{Id,colS}");
            infoStrings.Add($"{"Flight (Leg)",col0}{$"{Number} ({Origin} => {Destination})",colS}");
            infoStrings.Add($"{"STD",col0}{ScheduledOutTime,colS}");
            infoStrings.Add($"{"AC Type / Reg",col0}{$"{AircraftType} / {AircraftReg}",colS}");
            if (SettingProfile.RandomizePax && !Aircraft.GetIsCargo().RunSync())
            {
                infoStrings.Add($"{"Passenger (Diff)",col0}{$"{CountPaxPlanned} ({DiffPax})",colS}");
                infoStrings.Add($"{"Payload Diff",col0}{GetDisplayNumber(DiffPayloadKg),colN} {Config.DisplayUnitCurrentString}");
            }
            else
                infoStrings.Add($"{"Passenger",col0}{CountPax,colS}");
            infoStrings.Add($"{"Fuel Ramp",col0}{GetDisplayNumber(FuelRampKg),colN} {Config.DisplayUnitCurrentString}");
            infoStrings.Add($"{"Passenger Weight",col0}{GetDisplayNumber(WeightPaxKg),colN} {Config.DisplayUnitCurrentString}");
            infoStrings.Add($"{"Bag",col0}{GetDisplayNumber(WeightBagKg),colN} {Config.DisplayUnitCurrentString}");
            infoStrings.Add($"{"Freight",col0}{GetDisplayNumber(WeightFreightKg),colN} {Config.DisplayUnitCurrentString}");
            infoStrings.Add($"{"Payload Total",col0}{GetDisplayNumber(WeightPayloadKg),colN} {Config.DisplayUnitCurrentString}");
            infoStrings.Add($"{"OEW",col0}{GetDisplayNumber(AircraftEmptyOewKg),colN} {Config.DisplayUnitCurrentString}");
            infoStrings.Add($"{"ZFW",col0}{GetDisplayNumber(ZeroFuelRampKg),colN} {Config.DisplayUnitCurrentString}");
            infoStrings.Add($"{"GW Ramp",col0}{GetDisplayNumber(WeightTotalRampKg),colN} {Config.DisplayUnitCurrentString}");

            return [.. infoStrings];
        }

        protected virtual string GetDisplayNumber(double numKg)
        {
            return Conversion.ToString(Math.Round(Config.ConvertKgToDisplayUnit(numKg), 2), 2);
        }

        protected virtual double ToKg(double value)
        {
            if (Unit == DisplayUnit.KG)
                return value;
            else
                return WeightConverter.ToKg(value);
        }

        public virtual Task Unload()
        {
            if (Id != 0)
            {
                LastId = Id;
                if (OnUnload != null)
                    _ = TaskTools.RunPool(() => OnUnload?.Invoke());
            }
            Id = 0;
            Number = "";
            LastOnlineCheck = false;
            ScheduledOutTime = DateTime.MinValue;
            return Task.CompletedTask;
        }

        public virtual Task Reset()
        {
            LastId = 0;
            Id = 0;
            return Unload();
        }

        public virtual Task<bool> ImportOfp()
        {
            return Import(false);
        }

        public virtual void UpdatePlannedFuelKg(double fuelRampKg)
        {
            FuelRampKg = fuelRampKg;
        }

        public virtual void UpdatePassengerMax(int count)
        {
            MaxPax = count;
        }

        public virtual void UpdatePassengerCount(int count, int diff = 0)
        {
            CountPaxPlanned = count;
            DiffPax = diff;
        }

        public virtual void UpdateBagCount(int count, int diff = 0)
        {
            CountBagsPlanned = count;
            DiffBags = diff;
        }

        public virtual void UpdateFreightKg(double freightKg)
        {
            WeightFreightKg = freightKg;
        }

        public virtual void UpdateScheduledOut(DateTime outTime)
        {
            ScheduledOutTime = outTime;
        }
    }
}
