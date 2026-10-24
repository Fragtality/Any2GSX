using Any2GSX.Aircraft;
using Any2GSX.AppConfig;
using Any2GSX.GSX.Menu;
using Any2GSX.GSX.Services;
using Any2GSX.Notifications;
using Any2GSX.PluginInterface.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Automation.SmartCalls
{
    public abstract class SmartButton(SmartButtonCall id)
    {
        public virtual SmartButtonCall Id { get; } = id;
        protected virtual CancellationToken RequestToken => AppService.Instance.RequestToken;
        protected virtual GsxController GsxController => AppService.Instance.GsxController;
        protected virtual GsxMenu GsxMenu => GsxController.Menu;
        protected virtual GsxGate GsxGate => GsxController.Gate;
        protected virtual Config Config => AppService.Instance.Config;
        protected virtual SettingProfile Profile => AppService.Instance.SettingProfile;
        protected virtual GsxAutomationController AutomationController => GsxController.AutomationController;
        protected virtual AutomationState AutomationState => AutomationController.State;
        protected virtual bool AutomationStateValid => AutomationController.AutomationStateValid;
        protected virtual bool ExternalServiceControl => AutomationController.ExternalServiceControl;
        protected virtual DepartureServiceQueue DepartureQueue => AutomationController.DepartureQueue;
        protected virtual Flightplan Flightplan => AppService.Instance.Flightplan;
        protected virtual AircraftState AircraftState => AutomationController.AircraftState;
        protected virtual GroundEquipManager EquipManager => AutomationController.EquipManager;
        protected virtual NotificationTracker Tracker => AppService.Instance.NotificationTracker;
        protected virtual GsxServiceJetway ServiceJetway => GsxController.ServiceJetway;
        protected virtual GsxServiceStairs ServiceStairs => GsxController.ServiceStairs;
        protected virtual GsxServicePushback ServicePushBack => GsxController.ServicePushBack;

        public abstract bool CheckActive();
        public abstract bool CanExecute();
        public abstract Task<bool> Run();

        public static SmartButton Create(SmartButtonCall id)
        {
            return id switch
            {
                SmartButtonCall.WarpGate => new ButtonWarpGate(),
                SmartButtonCall.Connect => new ButtonConnect(),
                SmartButtonCall.NextService => new ButtonNextService(),
                SmartButtonCall.PushCall => new ButtonPushCall(),
                SmartButtonCall.PushStop => new ButtonPushStop(),
                SmartButtonCall.PushConfirm => new ButtonPushConfirm(),
                SmartButtonCall.Deice => new ButtonDeice(),
                SmartButtonCall.GateAction => new ButtonGateAction(),
                SmartButtonCall.Deboard => new ButtonDeboard(),
                SmartButtonCall.SkipTurn => new ButtonSkipTurn(),
                _ => new ButtonNone(),
            };
        }
    }
}
