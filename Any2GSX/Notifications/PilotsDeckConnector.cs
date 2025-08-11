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
        public virtual string VarDeckMenu => Config.DeckVarMenu;
        public virtual string VarDeckLine => Config.DeckVarLine;
        public virtual string VarDeckState  => Config.DeckVarState;
        public virtual string VarDeckCall => Config.DeckVarCall;
        public virtual string VarDeckInfoPax => Config.DeckVarInfoPax;
        public virtual string VarDeckInfoCargo => Config.DeckVarInfoCargo;
        public virtual CancellationToken Token => AppService.Instance.Token;
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
        };
        public virtual string StateColorOther { get; } = "[[#BFBFBF";

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
            try
            {
                if (!IsInitialized || !Sys.GetProcessRunning(BinaryPlugin))
                    return;

                try
                {
                    await HttpClient.GetStringAsync(string.Format(Config.DeckMessageUnregister, VarDeckMenu), Token);
                    for (int i = 1; i <= 10; i++)
                        await HttpClient.GetStringAsync(string.Format(Config.DeckMessageUnregister, $"{VarDeckLine}{i}"), Token);
                    await HttpClient.GetStringAsync(string.Format(Config.DeckMessageUnregister, VarDeckState), Token);
                    await HttpClient.GetStringAsync(string.Format(Config.DeckMessageUnregister, VarDeckCall), Token);
                    await HttpClient.GetStringAsync(string.Format(Config.DeckMessageUnregister, VarDeckInfoPax), Token);
                    await HttpClient.GetStringAsync(string.Format(Config.DeckMessageUnregister, VarDeckInfoCargo), Token);
                }
                catch { }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
        }

        protected virtual async Task WriteVariable(string name, string value)
        {
            if (!IsInitialized || string.IsNullOrWhiteSpace(Config?.DeckMessageWrite))
                return;

            try
            {
                if (!string.IsNullOrWhiteSpace(value))
                    value = value.Replace("\t", "").Replace("\r", "").Replace("\n", "[[n");

                await HttpClient.GetStringAsync(string.Format(Config.DeckMessageWrite, name, WebUtility.UrlEncode(value)), Token);
            }
            catch (Exception ex)
            {
                if (ex is HttpRequestException)
                    Logger.Error("HttpRequestException while writing PilotsDeck Variable");
                else if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
        }

        public override async Task SetConnected(bool connected, string profile)
        {
            if (connected)
                await WriteVariable($"{VarDeckConnected}", "1");
            else
                await WriteVariable($"{VarDeckConnected}", "0");
        }

        public override Task SetCouatlVars(string state)
        {
            return Task.CompletedTask;
        }

        protected override async Task SetMenuLine(int line, string value)
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
                else if (line > 7)
                    value = $"{StateColorOther}{value}";
            }

            await WriteVariable($"{VarDeckLine}{line}", value);
        }

        public override async Task SetMenuTitle(string title)
        {
            if (GsxController.Menu.IsGateMenu)
                title = title.Replace(GsxConstants.MenuGate, "", StringComparison.InvariantCultureIgnoreCase).TrimStart();

            await WriteVariable($"{VarDeckMenu}", title);
        }

        public override async Task SetSmartCall(SmartButtonCall call, string callInfo)
        {
            string text = call switch
            {
                SmartButtonCall.None => "",
                SmartButtonCall.Connect => "Connect[[nJetway/Stairs",
                SmartButtonCall.NextService => $"Call Next:[[n{callInfo}",
                SmartButtonCall.PushCall => "Call[[nPushback",
                SmartButtonCall.PushStop => "Stop[[nPushback",
                SmartButtonCall.PushConfirm => "Confirm[[nPush",
                SmartButtonCall.Deice => "Start[[nDe-icing",
                SmartButtonCall.ClearGate => "Clear[[nGate",
                SmartButtonCall.Deboard => "Call[[nDeboard",
                SmartButtonCall.SkipTurn => "Skip[[nTurnaround",
                _ => "",
            };
            await WriteVariable($"{VarDeckCall}", text);
        }

        public override Task SetDepartureServices(int completed, int running, int total)
        {
            return Task.CompletedTask;
        }

        public override Task ClearDepartureServices()
        {
            return Task.CompletedTask;
        }

        public override async Task SetState(AutomationState phase, string status)
        {
            if (!string.IsNullOrEmpty(status))
            {
                if (status.Contains("loading ...", StringComparison.InvariantCultureIgnoreCase) || status.Contains("boarding ...", StringComparison.InvariantCultureIgnoreCase))
                    await WriteVariable(VarDeckState, status.Replace(" ...", "[[n..."));
                else if (status.StartsWith('[') && status.EndsWith(']'))
                    await WriteVariable(VarDeckState, status);
                else if (status.StartsWith("Final in ") || status.StartsWith("Chocks in "))
                    await WriteVariable(VarDeckState, status.Replace("in ", "in[[n"));
                else if (status.Contains("OFP"))
                    await WriteVariable(VarDeckState, status.Replace("OFP", "[[nOFP"));
                else
                    await WriteVariable(VarDeckState, status.Replace(" ", "[[n").Replace("[[n...", " ..."));
            }
            else
                await WriteVariable(VarDeckState, phase.ToString().Replace("Taxi", "Taxi-"));
        }

        public override async Task SetBoardPaxInfo(int pax)
        {
            if (!Profile.PilotsDeckIntegration)
                return;

            await SetPaxInfo(pax, true);
        }

        public override async Task SetBoardCargoInfo(int percent)
        {
            if (!Profile.PilotsDeckIntegration)
                return;

            await SetCargoInfo(percent, true);
        }

        public override async Task SetDeboardPaxInfo(int pax)
        {
            if (!Profile.PilotsDeckIntegration)
                return;

            await SetPaxInfo(pax, false);
        }

        public override async Task SetDeboardCargoInfo(int percent)
        {
            if (!Profile.PilotsDeckIntegration)
                return;

            await SetCargoInfo(percent, false);
        }

        protected virtual async Task SetPaxInfo(int num, bool board)
        {
            string value = num >= 0 ? num.ToString() : "";
            if (num >= 0)
            {
                if (board)
                    value = $"{value} >";
                else
                    value = $"< {value}";
            }

            await WriteVariable(VarDeckInfoPax, value);
        }

        protected virtual async Task SetCargoInfo(int num, bool board)
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

            await WriteVariable(VarDeckInfoCargo, value);
        }
    }
}
