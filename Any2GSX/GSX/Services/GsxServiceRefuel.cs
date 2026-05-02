using Any2GSX.GSX.Menu;
using Any2GSX.Notifications;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimResources;
using System;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Services
{
    public class GsxServiceRefuel(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Refuel;
        protected override double NumStateCompleted { get; } = 1;
        public virtual ISimResourceSubscription SubRefuelService { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubRefuelService;
        public virtual ISimResourceSubscription SubRefuelHose { get; protected set; }
        public virtual ISimResourceSubscription SubRefuelUnderground { get; protected set; }
        public virtual bool IsHoseConnected => IsActive && SubRefuelHose.GetNumber() == 1;
        public virtual bool IsUnderground => SubRefuelUnderground?.GetNumber() == 1;
        public virtual bool WasHoseConnected { get; protected set; } = false;

        public event Func<bool, Task> OnHoseConnection;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(GsxMenuCommand.Open());
            sequence.Commands.Add(GsxMenuCommand.Select(3, GsxConstants.MenuGate));
            sequence.Commands.Add(GsxMenuCommand.Operator());
            sequence.EnableMenuAfterResetCheck = () => Profile.EnableMenuForSelection && AppService.Instance.AircraftController.HasFuelDialog;

            return sequence;
        }

        protected override GsxMenuSequence InitCancelSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(GsxMenuCommand.Open());
            sequence.Commands.Add(GsxMenuCommand.Select(3, GsxConstants.MenuGate));

            return sequence;
        }

        public override void InitSubscriptions()
        {
            SubRefuelService = SimStore.AddVariable(GsxConstants.VarServiceRefuel);
            SubRefuelHose = SimStore.AddVariable(GsxConstants.VarServiceRefuelHose);
            SubRefuelUnderground = SimStore.AddVariable(GsxConstants.VarServiceRefuelUnderground);

            SubRefuelService?.OnReceived += OnStateChange;
            SubRefuelHose?.OnReceived += OnHoseChange;
        }

        public override async Task Call()
        {
            await base.Call();
            if (IsCalled && AppService.Instance.AircraftController.HasFuelDialog)
                Controller.Tracker.TrackMessage(AppNotification.UpdatesBlocked, "Fuel Dialog");
        }

        protected override void RunStateRequested()
        {
            base.RunStateRequested();
            WasHoseConnected = false;
        }

        protected override void RunStateActive()
        {
            base.RunStateActive();
            int delay = AppService.Instance.AircraftController.HasFuelDialog ? Controller.Config.MenuOpenTimeout : Controller.Config.OperatorSelectTimeout;
            _ = TaskTools.RunDelayed(() => Controller.Tracker.Clear(AppNotification.UpdatesBlocked), delay, Controller.Token);
        }

        protected virtual Task OnHoseChange(ISimResourceSubscription sub, object data)
        {
            if (!Controller.IsGsxRunning)
                return Task.CompletedTask;

            if (sub.GetNumber() == 1 && State != GsxServiceState.Unknown && State != GsxServiceState.Completed)
            {
                Logger.Information($"Fuel Hose connected");
                if (State == GsxServiceState.Active)
                {
                    WasHoseConnected = true;
                    ActivationTime = DateTime.Now;
                    _ = TaskTools.RunPool(() => OnHoseConnection?.Invoke(true), Controller.Token);
                }
            }
            else if (sub.GetNumber() == 0 && State != GsxServiceState.Unknown && WasActive)
            {
                Logger.Information($"Fuel Hose disconnected");
                _ = TaskTools.RunPool(() => OnHoseConnection?.Invoke(false), Controller.Token);
            }

            return Task.CompletedTask;
        }

        protected override Task DoReset()
        {
            WasHoseConnected = false;

            return Task.CompletedTask;
        }

        public override void FreeResources()
        {
            SubRefuelService?.OnReceived -= OnStateChange;
            SubRefuelHose?.OnReceived -= OnHoseChange;

            SimStore.Remove(GsxConstants.VarServiceRefuel);
            SimStore.Remove(GsxConstants.VarServiceRefuelHose);
            SimStore.Remove(GsxConstants.VarServiceRefuelUnderground);
        }
    }
}
