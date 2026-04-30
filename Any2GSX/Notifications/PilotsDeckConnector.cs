using Any2GSX.AppConfig;
using Any2GSX.GSX;
using Any2GSX.GSX.Menu;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using CFIT.AppTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Any2GSX.Notifications
{
    public class PilotsDeckConnector : ExternalConnector
    {
        public virtual string DirPlugin => Path.Join(Sys.FolderAppDataRoaming(), @"Elgato\StreamDeck\Plugins\com.extension.pilotsdeck.sdPlugin");
        public static string BinaryPlugin => "PilotsDeck";
        public virtual string BinaryExectuable => $"{BinaryPlugin}.exe";
        public virtual GsxMenu Menu => GsxController.Menu;

        public virtual string VarDeckConnected => Config.DeckVarConnected;
        public virtual string VarDeckAircraftConnected => Config.DeckVarAircraftConnected;
        public virtual string VarDeckMenu => Config.DeckVarMenu;
        public virtual string VarDeckLine => Config.DeckVarLine;
        public virtual string VarDeckState => Config.DeckVarState;
        public virtual string VarDeckCall => Config.DeckVarCall;
        public virtual string VarDeckInfoPax => Config.DeckVarInfoPax;
        public virtual string VarDeckInfoCargo => Config.DeckVarInfoCargo;
        public virtual CancellationToken Token => AppService.Instance.Token;
        protected virtual DateTime NextAliveCheck { get; set; } = DateTime.MaxValue;
        protected virtual HttpClient HttpClient { get; }
        public virtual Dictionary<GsxServiceState, string> StateColors { get; } = new()
        {
            { GsxServiceState.Unknown, "[[#727272" },
            { GsxServiceState.Callable, "[[#0094FF" },
            { GsxServiceState.NotAvailable, "[[#727272" },
            { GsxServiceState.Bypassed, "[[#727272" },
            { GsxServiceState.Requested, "[[#FF940A" },
            { GsxServiceState.Active, "[[#FF940A" },
            { GsxServiceState.Completed, "[[#03C900" },
            { GsxServiceState.Completing, "[[#FF940A" },
        };
        public virtual string StateColorOther { get; } = "[[#BFBFBF";

        public static readonly Dictionary<GsxChangePark, string> ClearGateOptions = new()
        {
            { GsxChangePark.ChangeFacility, "Change[[nFacility" },
            { GsxChangePark.FollowMe, "Request[[nFollowMe" },
            { GsxChangePark.ProgTaxi, "Prog.[[nTaxi" },
            { GsxChangePark.TowIn, "Push-In[[nTowing" },
            { GsxChangePark.Revoke, "Revoke[[nServices" },
            { GsxChangePark.ClearAI, "Remove[[nAI" },
            { GsxChangePark.Warp, "Warp[[nGate" },
            { GsxChangePark.ShowMe, "Show[[nGate" },
            { GsxChangePark.Map, "Moving[[nMap" },
        };

        public PilotsDeckConnector()
        {
            HttpClient = new()
            {
                BaseAddress = new(Config.DeckUrlBase),
                Timeout = TimeSpan.FromMilliseconds(Config.HttpRequestTimeoutMs)
            };
            HttpClient.DefaultRequestHeaders.Accept.Clear();
        }

        public override async Task Init()
        {
            if (IsInitialized)
                return;

            if (Sys.GetProcessRunning(BinaryPlugin))
                await RegisterVariables();

            Logger.Debug($"PilotsDeck Connector initialized");
        }

        public override async Task CheckState()
        {
            if (NextAliveCheck < DateTime.Now)
            {
                NextAliveCheck = DateTime.Now + TimeSpan.FromMilliseconds(Config.DeckAliveCheckInterval);
                string state = await HttpClient.GetStringAsync(string.Format(Config.DeckMessageGet, VarDeckConnected), Token);
                if (state != "1")
                {
                    await SetConnected(true, AppService.Instance.ProfileName);
                    await SetAircraftConnected(AppService.Instance.AircraftController.IsConnected);
                }
            }
        }

        protected virtual async Task RegisterVariables()
        {
            await Task.Delay(Config.DeckRegisterDelay, Token);
            Logger.Debug($"Register PilotsDeck Variables");
            try
            {
                await HttpClient.GetStringAsync(string.Format(Config.DeckMessageRegister, VarDeckMenu), Token);
                for (int i = 1; i <= 10; i++)
                    await HttpClient.GetStringAsync(string.Format(Config.DeckMessageRegister, $"{VarDeckLine}{i}"), Token);
                await HttpClient.GetStringAsync(string.Format(Config.DeckMessageRegister, VarDeckState), Token);
                await HttpClient.GetStringAsync(string.Format(Config.DeckMessageRegister, VarDeckCall), Token);
                await HttpClient.GetStringAsync(string.Format(Config.DeckMessageRegister, VarDeckInfoPax), Token);
                await HttpClient.GetStringAsync(string.Format(Config.DeckMessageRegister, VarDeckInfoCargo), Token);
            }
            catch { }
        }

        public override async Task FreeRessources()
        {
            if (!IsInitialized || !Sys.GetProcessRunning(BinaryPlugin))
                return;

            try
            {
                NextAliveCheck = DateTime.MaxValue;
                await HttpClient.GetStringAsync(string.Format(Config.DeckMessageUnregister, VarDeckMenu), Token);
                for (int i = 1; i <= 10; i++)
                    await HttpClient.GetStringAsync(string.Format(Config.DeckMessageUnregister, $"{VarDeckLine}{i}"), Token);
                await HttpClient.GetStringAsync(string.Format(Config.DeckMessageUnregister, VarDeckState), Token);
                await HttpClient.GetStringAsync(string.Format(Config.DeckMessageUnregister, VarDeckCall), Token);
                await HttpClient.GetStringAsync(string.Format(Config.DeckMessageUnregister, VarDeckInfoPax), Token);
                await HttpClient.GetStringAsync(string.Format(Config.DeckMessageUnregister, VarDeckInfoCargo), Token);
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.Debug($"Error '{ex.GetType().Name}': {ex.Message}");
            }

        }

        protected virtual Task WriteVariable(string name, string value)
        {
            if (!IsInitialized || string.IsNullOrWhiteSpace(Config?.DeckMessageWrite))
                return Task.CompletedTask;

            try
            {
                if (!string.IsNullOrWhiteSpace(value))
                    value = value.Replace("\t", "").Replace("\r", "").Replace("\n", "[[n");

                return HttpClient.GetStringAsync(string.Format(Config.DeckMessageWrite, name, WebUtility.UrlEncode(value)), Token);
            }
            catch (Exception ex)
            {
                if (ex is HttpRequestException)
                    Logger.Error("HttpRequestException while writing PilotsDeck Variable");
                else if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }

            return Task.CompletedTask;
        }

        public override Task SetConnected(bool connected, string profile)
        {
            if (connected)
            {
                NextAliveCheck = DateTime.Now + TimeSpan.FromMilliseconds(Config.DeckAliveCheckInterval);
                NeedsRefresh = true;
                return WriteVariable($"{VarDeckConnected}", "1");
            }
            else
            {
                NextAliveCheck = DateTime.MaxValue;
                return WriteVariable($"{VarDeckConnected}", "0");
            }
        }

        public override Task SetAircraftConnected(bool connected)
        {
            if (connected)
                return WriteVariable($"{VarDeckAircraftConnected}", "1");
            else
                return WriteVariable($"{VarDeckAircraftConnected}", "0");
        }

        public override Task SetCouatlVars(string state)
        {
            return Task.CompletedTask;
        }

        protected override Task SetMenuLine(int line, string value)
        {
            line++;
            if (Menu.IsGateMenu && !string.IsNullOrWhiteSpace(value))
            {
                if (line == 1)
                {
                    if (AutomationController.State < AutomationState.TaxiOut)
                        value = $"{StateColorOther}{value}";
                    else if (AutomationController.State >= AutomationState.TaxiIn)
                        value = $"{StateColors[GsxController.GsxServices[GsxServiceType.Deboarding].State]}{value}";
                }
                else if (line == 2)
                {
                    if (AutomationController.State < AutomationState.TaxiOut)
                        value = $"{StateColors[GsxController.GsxServices[GsxServiceType.Catering].State]}{value}";
                    else if (AutomationController.State >= AutomationState.TaxiIn)
                        value = $"{StateColorOther}{value}";
                }
                else if (line == 3)
                {
                    if (AutomationController.State < AutomationState.TaxiOut)
                        value = $"{StateColors[GsxController.GsxServices[GsxServiceType.Refuel].State]}{value}";
                    else if (AutomationController.State >= AutomationState.TaxiIn)
                        value = $"{StateColorOther}{value}";
                }
                else if (line == 4)
                {
                    if (AutomationController.State < AutomationState.TaxiOut)
                        value = $"{StateColors[GsxController.GsxServices[GsxServiceType.Boarding].State]}{value}";
                    else if (AutomationController.State >= AutomationState.TaxiIn)
                        value = $"{StateColorOther}{value}";
                }
                else if (line == 5)
                {
                    if (AutomationController.State < AutomationState.TaxiOut)
                        value = $"{StateColors[GsxController.GsxServices[GsxServiceType.Pushback].State]}{value}";
                    else if (AutomationController.State >= AutomationState.TaxiIn)
                        value = $"{StateColorOther}{value}";
                }
                else if (line == 6)
                {
                    if (GsxController.GsxServices[GsxServiceType.Jetway].State == GsxServiceState.Active)
                        value = $"{StateColors[GsxServiceState.Completed]}{value}";
                    else
                        value = $"{StateColors[GsxController.GsxServices[GsxServiceType.Jetway].State]}{value}";
                }
                else if (line == 7)
                {
                    if (GsxController.GsxServices[GsxServiceType.Stairs].State == GsxServiceState.Active)
                        value = $"{StateColors[GsxServiceState.Completed]}{value}";
                    else
                        value = $"{StateColors[GsxController.GsxServices[GsxServiceType.Stairs].State]}{value}";
                }
                else if (line > 8)
                    value = $"{StateColorOther}{value}";
            }

            return WriteVariable($"{VarDeckLine}{line}", value);
        }

        public override Task SetMenuTitle(string title)
        {
            if (GsxController.Menu.IsGateMenu)
                title = title.Replace(GsxConstants.MenuGate, "", StringComparison.InvariantCultureIgnoreCase).TrimStart();
            else if (GsxController.Menu.IsSelectGateMenu)
                title = title.Replace(GsxConstants.MenuParkingSelect, "", StringComparison.InvariantCultureIgnoreCase).TrimStart();

            return WriteVariable($"{VarDeckMenu}", title);
        }

        public override async Task SetSmartCall(SmartButtonCall call, string callInfo, bool force = false)
        {
            string text = call switch
            {
                SmartButtonCall.None => "",
                SmartButtonCall.Connect => "Connect[[nJetway/Stairs",
                SmartButtonCall.NextService => $"Call Next:[[n{callInfo}",
                SmartButtonCall.PushCall => "Call[[nPushback",
                SmartButtonCall.PushStop => $"{callInfo}[[nPushback",
                SmartButtonCall.PushConfirm => "Confirm[[nStart",
                SmartButtonCall.Deice => $"{callInfo}[[nDeIce",
                SmartButtonCall.ClearGate => callInfo,
                SmartButtonCall.Deboard => "Call[[nDeboard",
                SmartButtonCall.SkipTurn => "Skip[[nTurnaround",
                SmartButtonCall.WarpGate => "Warp[[nGate",
                _ => "",
            };

            if (ReportedCall != call || ReportedCallInfo != text || force || NeedsRefresh)
                await WriteVariable($"{VarDeckCall}", text);
            ReportedCall = call;
            ReportedCallInfo = text;
        }

        public override Task SetSmartCall(SmartButtonCall call, GsxChangePark callInfo, bool force = false)
        {
            return SetSmartCall(call, ClearGateOptions[Profile.ClearGateMenuOption], force);
        }

        public override Task SetDepartureServices(int completed, int running, int total)
        {
            return Task.CompletedTask;
        }

        public override Task ClearDepartureServices()
        {
            return Task.CompletedTask;
        }

        public override async Task SetState(AutomationState phase, Notification notification)
        {
            string status = "";

            switch (notification.Id)
            {
                case AppNotification.UpdatesBlocked:
                    status = $"Wait:[[n{notification.Message}";
                    break;
                case AppNotification.GsxRestart:
                    status = "GSX Restart[[n...";
                    break;
                case AppNotification.GsxRefresh:
                    status = "OFP Refresh[[n...";
                    break;
                case AppNotification.GsxQuestion:
                    if (notification.HasMessage)
                        status = notification.Message;
                    break;
                case AppNotification.ServiceCall:
                    if (notification.HasMessage)
                        status = $"Call {notification.Message}[[n...";
                    break;
                case AppNotification.MenuCommand:
                    if (notification.Message.StartsWith("Open", StringComparison.InvariantCultureIgnoreCase))
                        status = $"Open Menu[[n...";
                    break;
                case AppNotification.MenuSequence:
                    status = $"Menu Call[[n...";
                    break;
                case AppNotification.GateSelect:
                    if (GsxController.IsDeiceAvail && GsxController.AutomationState == AutomationState.TaxiOut)
                        status = $"Select Pad!";
                    else
                        status = $"Select Gate!";
                    break;
                case AppNotification.GateMove:
                    if (Tracker.HasCapture)
                    {
                        if (phase <= AutomationState.Departure)
                            status = "Move to Gate!";
                        else if (phase == AutomationState.TaxiOut)
                            status = $"Pad: {Tracker.LastCapturedGate}";
                        else if (phase == AutomationState.TaxiIn)
                            status = $"Gate: {Tracker.LastCapturedGate}";
                    }
                    else
                        status = "Move to Gate!";
                    break;
                case AppNotification.GateEquip:
                    int countdown = AutomationController.EquipManager.GetCountdown();
                    if (countdown > 0)
                        status = $"Equipment in[[n{countdown}s";
                    break;
                case AppNotification.GateDepart:
                    status = "Trigger Departure!";
                    break;
                case AppNotification.OfpImported:
                    status = "OFP Imported";
                    break;
                case AppNotification.OfpCheck:
                    if (notification.ClearTime > DateTime.Now)
                        status = $"OFP Checkin[[n{(int)((notification.ClearTime - DateTime.Now).TotalSeconds)}s";
                    break;
                case AppNotification.OperateJetway:
                    status = "Jetway Operating[[n...";
                    break;
                case AppNotification.OperateStairs:
                    status = "Stairs Operating[[n...";
                    break;
                case AppNotification.ServiceComplete:
                    if (notification.HasMessage)
                        status = $"{notification.Message} completed";
                    break;
                case AppNotification.ServiceActive:
                    if (notification.HasMessage)
                        status = $"{notification.Message} active";
                    break;
                case AppNotification.ServiceDeboard:
                    if (notification.HasMessage)
                        status = notification.Message.Replace(" ...", "[[n...");
                    break;
                case AppNotification.ServiceBoard:
                    if (notification.HasMessage)
                        status = notification.Message.Replace(" ...", "[[n...");
                    break;
                case AppNotification.ServiceRefuel:
                    if (notification.HasMessage)
                        status = notification.Message;
                    break;
                case AppNotification.SheetFinal:
                    if (notification.ClearTime > DateTime.Now)
                        status = $"Final in[[n{(int)((notification.ClearTime - DateTime.Now).TotalSeconds)}s";
                    break;
                case AppNotification.PushPhase:
                    if (notification.HasMessage)
                        status = notification.Message.Replace(" ...", "[[n...");
                    break;
                default:
                    if (notification.Id != AppNotification.None)
                        status = notification.Id.ToString();
                    break;
            }

            if (ReportedPhase != phase || ReportedStatusMessage != status || NeedsRefresh)
            {
                if (!string.IsNullOrEmpty(status))
                    await WriteVariable(VarDeckState, status);
                else
                    await WriteVariable(VarDeckState, phase.ToString().Replace("Taxi", "Taxi-"));
            }
            ReportedPhase = phase;
            ReportedStatusMessage = status;
        }

        public override Task SetBoardPaxInfo(int pax)
        {
            if (!Profile.PilotsDeckIntegration)
                return Task.CompletedTask;

            return SetPaxInfo(pax, true);
        }

        public override Task SetBoardCargoInfo(int percent)
        {
            if (!Profile.PilotsDeckIntegration)
                return Task.CompletedTask;

            return SetCargoInfo(percent, true);
        }

        public override Task SetDeboardPaxInfo(int pax)
        {
            if (!Profile.PilotsDeckIntegration)
                return Task.CompletedTask;

            return SetPaxInfo(pax, false);
        }

        public override Task SetDeboardCargoInfo(int percent)
        {
            if (!Profile.PilotsDeckIntegration)
                return Task.CompletedTask;

            return SetCargoInfo(percent, false);
        }

        protected virtual Task SetPaxInfo(int num, bool board)
        {
            string value = num >= 0 ? num.ToString() : "";
            if (num >= 0)
            {
                if (board)
                    value = $"{value} >";
                else
                    value = $"< {value}";
            }

            return WriteVariable(VarDeckInfoPax, value);
        }

        protected virtual Task SetCargoInfo(int num, bool board)
        {
            string value = num >= 0 ? num.ToString() : "";
            if (num > 100)
                value = "100";

            if (num >= 0)
            {
                if (board)
                    value = $"< {value}%";
                else
                    value = $"{value}% >";
            }

            return WriteVariable(VarDeckInfoCargo, value);
        }
    }
}
