using Any2GSX.Notifications;
using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.PluginInterface.Interfaces.GSX.API.Payload;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Any2GSX.API
{
#pragma warning disable IDE1006
    public class ReponsePayload(ApiServer server)
    {
        [JsonIgnore]
        protected ApiServer Server { get; } = server;
        [JsonIgnore]
        protected static AppService AppService => AppService.Instance;
    }

    public class ResponseStatus(ApiServer server) : ReponsePayload(server)
    {
        public virtual string state => AppService.IsSessionInitialized ? "Session" : "Running";
        public virtual string phase => Server.GsxController.AutomationState.ToString();
        public virtual string profile => AppService.ProfileName;
        public virtual string profileGsx => Server.GsxController.ApiData?.HandlerData?.aircraft?.dataSourceUsed ?? "";
        public virtual string aircraftPath => AppService.AircraftString;
        public virtual string aircraftTitle => AppService.AircraftTitle;
        public virtual long ofpId => Server.Flightplan?.Id ?? 0;
        public virtual bool externalControl => Server.AutomationController.ExternalServiceControl;
        public virtual List<string> servicesQueued => Server.AutomationController.DepartureQueue.GetQueuedServicesTypeNames();
        public virtual List<string> servicesCalled => Server.AutomationController.DepartureQueue.GetCalledServicesTypeNames();
        public virtual List<string> servicesSkipped => Server.AutomationController.DepartureQueue.GetSkippedServicesTypeNames();
    }

    public class ResponsePushback(ApiServer server) : ReponsePayload(server)
    {
        public virtual GsxServiceState state => Server.GsxController?.ServicePushBack?.State ?? GsxServiceState.Unknown;
        public virtual int status => Server.GsxController?.ServicePushBack?.PushStatus ?? 0;
        public virtual bool bypassPin => Server.GsxController?.ServicePushBack?.IsPinInserted ?? false;
        public virtual bool releaseBrake => AppService?.NotificationTracker?.IsActive(AppNotification.PushReleaseBrake) == true;
        public virtual List<string> directions => Server.GsxController.ApiData?.HandlerData?.gate?.PushbackDirections ?? [];
        public virtual string directionSet => Server.GsxGate?.AutoSelectionPushback ?? "";
    }

    public class ResponseGates(ApiServer server) : ReponsePayload(server)
    {
        public virtual Dictionary<string, GsxPayloadHandlerGateInfo> gates => Server.GsxController?.ApiData?.HandlerData?.airport?.GetParkings() ?? [];
    }
#pragma warning restore IDE1006
}
