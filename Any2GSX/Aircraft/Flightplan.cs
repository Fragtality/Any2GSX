using Any2GSX.AppConfig;
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
        public virtual SettingProfile SettingProfile => AppService.Instance.SettingProfile;
        public virtual IWeightConverter WeightConverter => AppService.Instance.WeightConverter;
        public virtual CancellationToken Token => AppService.Instance.Token;
        protected virtual HttpClient HttpClient { get; }

        public virtual int Id { get; set; } = 0;
        public virtual int LastId { get; set; } = 0;
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
        public virtual double FuelRampKg { get; set; }
        public virtual int CountPax { get; set; }
        public virtual int DiffPax { get; set; }
        public virtual double DiffPayloadKg { get; set; }
        public virtual int CountBags { get; set; }
        public virtual double WeightPaxKg => CountPax * WeightPerPaxKg;
        public virtual double WeightBagKg => CountBags * WeightPerBagKg;
        public virtual double WeightFreightKg { get; set; }
        public virtual double WeightCargoKg => WeightBagKg + WeightFreightKg;
        public virtual double WeightPayloadKg => WeightPaxKg + WeightCargoKg;
        public virtual double ZeroFuelRampKg { get; set; }
        public virtual double WeightTotalRampKg { get; set; }

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
                Logger.Debug($"Requesting SimBrief (via Userid) ...");
                return JsonNode.Parse(await HttpClient.GetStringAsync(string.Format(Config.SimbriefUrlPathId, SimbriefUser), Token));
            }
            else
            {
                Logger.Debug($"Requesting SimBrief (via Username) ...");
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

        public virtual async Task<int> CheckIdOnline()
        {
            try
            {
                var json = await GetJsonNode();
                if (GetJsonInt(json["params"]!["request_id"], out int id))
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
            int id = await CheckIdOnline();
            LastOnlineCheck = id > 0 && id != LastId;
            return LastOnlineCheck;
        }

        public virtual async Task<bool> Import()
        {
            try
            {
                Logger.Information($"Importing OFP from SimBrief ...");
                Unload();
                var json = await GetJsonNode();

                if (GetJsonInt(json["params"]!["request_id"], out int id) && id == LastId)
                    return false;

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

                if (GetJsonDouble(json["weights"]!["pax_weight"], out double paxWeight))
                    WeightPerPaxKg = ToKg(paxWeight);
                else
                    return false;

                if (GetJsonDouble(json["weights"]!["bag_weight"], out double bagWeight))
                    WeightPerBagKg = ToKg(bagWeight);
                else
                    return false;

                if (GetJsonInt(json["weights"]!["pax_count"], out int pax))
                    CountPax = pax;
                else
                    return false;

                if (GetJsonInt(json["weights"]!["bag_count"], out int bag))
                    CountBags = bag;
                else
                    return false;

                if (GetJsonDouble(json["weights"]!["est_ramp"], out double estRamp))
                    WeightTotalRampKg = ToKg(estRamp);
                else
                    return false;

                if (GetJsonDouble(json["weights"]!["est_zfw"], out double estZfw))
                    ZeroFuelRampKg = ToKg(estZfw);
                else
                    return false;

                if (GetJsonDouble(json["fuel"]!["plan_ramp"], out double fuel))
                {
                    if (SettingProfile.FuelRoundUp100)
                    {
                        fuel = Math.Ceiling(fuel / 100.0) * 100.0;
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

                Id = id;
                LastId = id;

                DiffPax = 0;
                if (SettingProfile.RandomizePax && AppService.Instance?.AircraftController?.Aircraft?.IsCargo == false)
                {
                    DiffPax = Random.Shared.Next(SettingProfile.RandomizePaxMaxDiff * -1, SettingProfile.RandomizePaxMaxDiff);
                    if (CountPax + DiffPax > 0)
                    {
                        CountPax += DiffPax;
                        CountBags += DiffPax;
                        DiffPayloadKg = (DiffPax * WeightPerPaxKg) + (DiffPax * WeightPerBagKg);
                        ZeroFuelRampKg += DiffPayloadKg;
                        WeightTotalRampKg += DiffPayloadKg;
                    }
                }

                if (Config.DisplayUnitSource == DisplayUnitSource.Simbrief && Config.DisplayUnitCurrent != Unit)
                {
                    Logger.Debug($"Switching DisplayUnit to Simbrief Source");
                    Config.SetDisplayUnit(Unit);
                }

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
            infoStrings.Add($"{"AC Type / Reg",col0}{$"{AircraftType} / {AircraftReg}",colS}");
            if (SettingProfile.RandomizePax && AppService.Instance?.AircraftController?.Aircraft?.IsCargo == false)
            {
                infoStrings.Add($"{"Passenger (Diff)",col0}{$"{CountPax} ({DiffPax})",colS}");
                infoStrings.Add($"{"Payload Diff",col0}{GetDisplayNumber(DiffPayloadKg),colN} {Config.DisplayUnitCurrentString}");
            }
            else
                infoStrings.Add($"{"Passenger",col0}{CountPax,colS}");
            infoStrings.Add($"{"Fuel Ramp",col0}{GetDisplayNumber(FuelRampKg),colN} {Config.DisplayUnitCurrentString}");
            infoStrings.Add($"{"Passenger Weight",col0}{GetDisplayNumber(WeightPaxKg),colN} {Config.DisplayUnitCurrentString}");
            infoStrings.Add($"{"Bag",col0}{GetDisplayNumber(WeightBagKg),colN} {Config.DisplayUnitCurrentString}");
            infoStrings.Add($"{"Freight",col0}{GetDisplayNumber(WeightFreightKg),colN} {Config.DisplayUnitCurrentString}");
            infoStrings.Add($"{"Payload Total",col0}{GetDisplayNumber(WeightPayloadKg),colN} {Config.DisplayUnitCurrentString}");
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

        public virtual void Unload()
        {
            Id = 0;
            Number = "";
            DiffPax = 0;
            DiffPayloadKg = 0;
            LastOnlineCheck = false;
        }

        public virtual void Reset()
        {
            Unload();
            LastId = 0;
        }

        public virtual void UpdatePlannedFuelKg(double fuelRampKg)
        {
            FuelRampKg = fuelRampKg;
        }

        public virtual void UpdatePassengerCount(int count)
        {
            CountPax = count;
            DiffPax = 0;
        }

        public virtual void UpdateFreightKg(double freightKg)
        {
            WeightFreightKg = freightKg;
        }
    }
}
