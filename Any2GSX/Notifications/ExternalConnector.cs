using Any2GSX.AppConfig;
using Any2GSX.CommBus;
using Any2GSX.GSX;
using Any2GSX.GSX.Automation;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using CFIT.SimConnectLib;
using CFIT.SimConnectLib.Definitions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Any2GSX.Notifications
{
    public abstract class ExternalConnector
    {
        public virtual ModuleCommBus CommBus => AppService.Instance?.CommBus;
        public virtual SettingProfile Profile => AppService.Instance?.SettingProfile;
        public virtual Config Config => AppService.Instance?.Config;
        public virtual SimConnectManager SimConnect => AppService.Instance?.SimConnect;
        public virtual bool HasEfbApp => SimConnect?.GetSimVersion() == SimVersion.MSFS2024;
        public virtual GsxController GsxController => AppService.Instance?.GsxController;
        public virtual GsxAutomationController AutomationController => AppService.Instance?.GsxController?.AutomationController;
        public virtual NotificationTracker Tracker => AppService.Instance?.NotificationTracker;
        public virtual bool IsInitialized { get; protected set; } = false;
        public virtual AutomationState ReportedPhase { get; protected set; } = AutomationState.Unknown;
        public virtual string ReportedStatusMessage { get; protected set; } = "";
        public virtual SmartButtonCall ReportedCall { get; protected set; } = SmartButtonCall.None;
        public virtual string ReportedCallInfo { get; protected set; } = "";

        public virtual async Task Start()
        {
            if (IsInitialized)
                return;

            await Init();

            IsInitialized = true;
            Logger.Debug($"Connector {this.GetType().Name} initialized");
        }

        public abstract Task Init();

        public virtual async Task Stop()
        {
            try
            {
                if (!IsInitialized)
                    return;

                await FreeRessources();

                IsInitialized = false;
                Logger.Debug($"Connector {this.GetType().Name} stopped");
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
        }

        public abstract Task FreeRessources();

        public abstract Task SetConnected(bool connected, string profile);
        public abstract Task SetAircraftConnected(bool connected);
        public abstract Task SetBoardPaxInfo(int pax);
        public abstract Task SetBoardCargoInfo(int percent);
        public abstract Task SetDeboardPaxInfo(int pax);
        public abstract Task SetDeboardCargoInfo(int percent);
        public abstract Task SetState(AutomationState phase, Notification notification);
        public abstract Task SetCouatlVars(string state);
        public abstract Task SetSmartCall(SmartButtonCall call, string callInfo, bool force = false);

        public virtual Task SetSmartCall(SmartButtonCall call, GsxChangePark callInfo, bool force = false)
        {
            return SetSmartCall(call, NotificationManager.ClearGateOptions[Profile.ClearGateMenuOption], force);
        }

        public virtual Task SetSmartCall(SmartButtonCall call, GsxServiceType callInfo, bool force = false)
        {
            return SetSmartCall(call, callInfo.ToString(), force);
        }

        public abstract Task SetDepartureServices(int completed, int running, int total);
        public abstract Task ClearDepartureServices();
        public abstract Task SetMenuTitle(string title);
        public virtual async Task SetMenuLines(List<string> menuLines)
        {
            for (int i = 0; i < 10; i++)
                await SetMenuLine(i, (i < menuLines.Count ? menuLines[i] : ""));
        }
        protected abstract Task SetMenuLine(int index, string text);
    }
}
