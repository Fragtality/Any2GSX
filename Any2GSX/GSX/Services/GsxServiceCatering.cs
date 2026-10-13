using Any2GSX.Notifications;
using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.PluginInterface.Interfaces.GSX;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimResources;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Services
{
    public class GsxServiceCatering() : GsxService()
    {
        public override GsxServiceType Type => GsxServiceType.Catering;
        protected override int MenuSelectionService => 2;
        protected override string StateVarName => GsxConstants.VarServiceCatering;
        protected override List<string> ServiceVariables => [GsxConstants.VarVehicleCateringFront, GsxConstants.VarVehicleCateringRear];
        public override GsxMenuCommandOperatorType OperatorType => GsxMenuCommandOperatorType.Catering;
        public override bool HasStairConflict => Controller.HasGateStair && Controller.Aircraft.HasRearStair() && Controller.IsLavatoryOnStairSide && Controller.ApiData?.HandlerData?.aircraft?.HasLavatoryConflict() == true;

        protected virtual ConcurrentDictionary<GsxVehicleLocation, ISimResourceSubscription> SubVehicleTrucks { get; } = [];
        protected virtual ConcurrentDictionary<GsxVehicleLocation, Func<ISimResourceSubscription, object, Task>> SubVehicleCallbacks { get; } = [];

        public event Func<GsxVehicleLocation, GsxVehicleCateringState, Task> OnVehicleChanged;

        protected override void DoInit()
        {
            SubVehicleTrucks.Add(GsxVehicleLocation.Front, SimStore[GsxConstants.VarVehicleCateringFront]);
            SubVehicleTrucks.Add(GsxVehicleLocation.Rear, SimStore[GsxConstants.VarVehicleCateringRear]);

            SubVehicleCallbacks.Add(GsxVehicleLocation.Front, (s, v) => OnVehicleChange(GsxVehicleLocation.Front, s?.GetNumber() ?? 0));
            SubVehicleCallbacks.Add(GsxVehicleLocation.Rear, (s, v) => OnVehicleChange(GsxVehicleLocation.Rear, s?.GetNumber() ?? 0));

            SubVehicleTrucks[GsxVehicleLocation.Front]?.OnReceived += SubVehicleCallbacks[GsxVehicleLocation.Front];
            SubVehicleTrucks[GsxVehicleLocation.Rear]?.OnReceived += SubVehicleCallbacks[GsxVehicleLocation.Rear];
        }

        protected override Task DoReset()
        {
            return Task.CompletedTask;
        }

        protected override void DoRelease()
        {
            SubVehicleTrucks[GsxVehicleLocation.Front]?.OnReceived -= SubVehicleCallbacks[GsxVehicleLocation.Front];
            SubVehicleTrucks[GsxVehicleLocation.Rear]?.OnReceived -= SubVehicleCallbacks[GsxVehicleLocation.Rear];
            SubVehicleCallbacks.Clear();
            SubVehicleTrucks.Clear();
        }

        protected virtual Task OnVehicleChange(GsxVehicleLocation truck, double state)
        {
            if (InhibitStateUpdates)
                return Task.CompletedTask;

            Logger.Debug($"Vehicle State Change for Catering {truck}: {(GsxVehicleCateringState)(int)state}");
            _ = TaskTools.RunPool(() => OnVehicleChanged?.Invoke(truck, (GsxVehicleCateringState)(int)state), Controller.RequestToken);
            return Task.CompletedTask;
        }

        public virtual GsxVehicleCateringState GetTruckState(GsxVehicleLocation truck)
        {
            try
            {
                return (GsxVehicleCateringState)(int)SubVehicleTrucks[truck].GetNumber();
            }
            catch
            {
                return GsxVehicleCateringState.Unknown;
            }
        }

        public virtual bool IsAnyTruck(Func<GsxVehicleCateringState, bool> func)
        {
            return Enum.GetValues<GsxVehicleLocation>().Any((s) => func(GetTruckState(s)));
        }

        public override async Task ResetVehicleState(bool force = false)
        {
            InhibitStateUpdates = true;
            try
            {
                if (force || (SubStateVar.GetNumber() <= (int)GsxServiceState.Bypassed && IsAnyTruck((state) => state > GsxVehicleCateringState.Idle)) || Controller.Tracker.IsActive(AppNotification.GsxRestart))
                {
                    Logger.Debug($"Reset Vehicle State for {Type}");
                    await SubVehicleTrucks[GsxVehicleLocation.Front]?.WriteValue(0);
                    await SubVehicleTrucks[GsxVehicleLocation.Rear]?.WriteValue(0);
                }
            }
            catch { }
            InhibitStateUpdates = false;
        }
    }
}
