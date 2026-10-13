using Any2GSX.GSX.Menu;
using Any2GSX.PluginInterface.Interfaces.GSX;
using Any2GSX.Tools;
using CFIT.AppLogger;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Any2GSX.CommBus
{
    public class CommBusServer() : AppServer()
    {
        public override List<string> SupportedVersions => ["v1"];

        protected virtual ModuleCommBus CommBus => AppService.Instance.CommBus;
        protected virtual GsxMenu GsxMenu => AppService.Instance.GsxController.Menu;

        protected override void RegisterEndpoints()
        {
            SupportedEndpoints["v1"].Add("pushevent", ProcessPushEvent);
            SupportedEndpoints["v1"].Add("ping-reply", ProcessPingReply);
        }

        protected override Task AfterStart()
        {
            CommBus.Port = Port;

            return Task.CompletedTask;
        }

        protected virtual Task ProcessPushEvent(RequestEnvelope request)
        {
            var message = DeserializeRequest<MessageReceive>(request);
            if (Config.VerboseLogging)
                Logger.Verbose($"Received Push Event: {message.@event}");

            if (message.@event == GsxConstants.EventCommBus)
                GsxMenu.OnToolbarEvent(message.data);
            else
                CommBus.PushEvent(message.@event, message.data);

            request.SetOk();
            return Task.CompletedTask;
        }

        protected virtual Task ProcessPingReply(RequestEnvelope request)
        {
            var message = DeserializeRequest<MessageReceive>(request);
            Logger.Debug($"CommBus WASM Version: {message.data}");
            CommBus.IsPingReceived = true;
            request.SetOk();

            return Task.CompletedTask;
        }
    }
}
