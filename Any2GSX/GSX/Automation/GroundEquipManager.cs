using Any2GSX.AppConfig;
using Any2GSX.GSX.Services;
using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using CFIT.AppTools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Automation
{
    public class GroundEquipManager
    {
        protected virtual CancellationToken RequestToken => AppService.Instance.RequestToken;
        protected virtual AircraftBase Aircraft => AppService.Instance?.AircraftController?.Aircraft;
        protected virtual SettingProfile Profile => AppService.Instance?.SettingProfile;
        protected virtual GsxController GsxController => AppService.Instance?.GsxController;
        protected virtual ConcurrentDictionary<GsxServiceType, GsxService> GsxServices => GsxController?.GsxServices;
        protected virtual GsxServiceGpu ServiceGpu => GsxController?.ServiceGpu;
        protected virtual int ConnectPca => Profile?.ConnectPca ?? 2;
        protected virtual bool PcaOverride => Profile?.PcaOverride ?? true;
        protected virtual bool HasGateJetway => GsxController?.HasGateJetway ?? true;
        public virtual bool HasChocks { get; protected set; } = false;
        public virtual bool EquipmentChocks { get; protected set; } = false;
        public virtual bool BrakeSet { get; protected set; } = false;
        public virtual bool HasCones { get; protected set; } = false;
        public virtual bool EquipmentCones { get; protected set; } = false;
        public virtual bool HasGpuInternal { get; protected set; } = false;
        public virtual bool GpuRequireChocks { get; protected set; } = false;
        public virtual GsxGpuUsage UseGpuGsx { get; protected set; } = GsxGpuUsage.Never;
        public virtual bool EquipmentGpu { get; protected set; } = false;
        public virtual bool AvionicsPowered { get; protected set; } = false;
        public virtual bool PowerConnected { get; protected set; } = false;
        public virtual bool HasPca { get; protected set; } = false;
        public virtual bool PcaRequirePower { get; protected set; } = false;
        public virtual bool EquipmentPca { get; protected set; } = false;
        public virtual bool ApuRunning { get; protected set; } = false;
        public virtual bool ApuBleed { get; protected set; } = false;

        protected virtual List<GroundEquipment> GroundEquipmentKeys { get; } = [GroundEquipment.GPU, GroundEquipment.Chocks, GroundEquipment.Cones, GroundEquipment.PCA];
        protected virtual ConcurrentDictionary<GroundEquipment, DateTime> GroundEquipmentTimes { get; } = new()
        {
            { GroundEquipment.Chocks, DateTime.MinValue },
            { GroundEquipment.Cones, DateTime.MinValue },
            { GroundEquipment.GPU, DateTime.MinValue },
            { GroundEquipment.PCA, DateTime.MinValue },
        };

        public virtual int GetCountdown(GroundEquipment equip)
        {
            int countdown = 0;
            if (GroundEquipmentTimes.TryGetValue(equip, out var dateTime) && dateTime > DateTime.Now)
                countdown = (dateTime - DateTime.Now).Seconds;

            return countdown;
        }

        public virtual int GetCountdown()
        {
            var result = GroundEquipmentTimes.Max(kv => kv.Value);
            if (result > DateTime.Now)
                return (result - DateTime.Now).Seconds;
            else
                return 0;
        }

        public virtual DateTime GetTime(GroundEquipment equip)
        {
            if (GroundEquipmentTimes.TryGetValue(equip, out var dateTime))
                return dateTime;
            else
                return DateTime.MinValue;
        }

        protected virtual int RollDelay(int min = 0)
        {
            return new Random().Next(min <= 0 || min >= Profile.ChockDelayMax ? Profile.ChockDelayMin : min, Profile.ChockDelayMax);
        }

        public virtual void SetRandomDelays()
        {
            GroundEquipmentTimes.Clear();
            int lastMin = Profile.ChockDelayMin;
            foreach (var key in GroundEquipmentKeys)
            {
                lastMin = RollDelay(lastMin);
                GroundEquipmentTimes.Add(key, DateTime.Now + TimeSpan.FromSeconds(lastMin));
            }
        }

        protected virtual void SetZeroDelays()
        {
            GroundEquipmentTimes.Clear();
            foreach (var key in GroundEquipmentKeys)
                GroundEquipmentTimes.Add(key, DateTime.MinValue);
        }

        public virtual void Reset()
        {
            SetZeroDelays();
            HasCones = false;
            EquipmentCones = false;
            BrakeSet = false;
            HasChocks = false;
            EquipmentChocks = false;
            HasGpuInternal = false;
            GpuRequireChocks = false;
            UseGpuGsx = GsxGpuUsage.Never;
            EquipmentGpu = false;
            AvionicsPowered = false;
            PowerConnected = false;
            HasPca = false;
            PcaRequirePower = false;
            EquipmentPca = false;
            ApuRunning = false;
            ApuBleed = false;
        }

        public virtual async Task Refresh()
        {
            HasCones = await Aircraft.GetHasCones();
            EquipmentCones = await Aircraft.GetEquipmentCones();
            BrakeSet = await Aircraft.GetBrakeSet();
            HasChocks = await Aircraft.GetHasChocks();
            EquipmentChocks = await Aircraft.GetEquipmentChocks();
            HasGpuInternal = await Aircraft.GetHasGpuInternal();
            GpuRequireChocks = await Aircraft.GetGpuRequireChocks();
            UseGpuGsx = await Aircraft.GetUseGpuGsx();
            EquipmentGpu = await Aircraft.GetExternalPowerAvailable();
            AvionicsPowered = await Aircraft.GetAvionicPowered();
            PowerConnected = await Aircraft.GetExternalPowerConnected();
            HasPca = await Aircraft.GetHasPca();
            PcaRequirePower = await Aircraft.GetPcaRequirePower();
            EquipmentPca = await Aircraft.GetEquipmentPca();
            ApuRunning = await Aircraft.GetApuRunning();
            ApuBleed = await Aircraft.GetApuBleedOn();
        }

        protected virtual async Task SetPca(string phase)
        {
            if (GroundEquipmentTimes[GroundEquipment.PCA] > DateTime.MinValue && DateTime.Now < GroundEquipmentTimes[GroundEquipment.PCA])
                return;

            if (!GsxController.IsGateConnected && !EquipmentGpu)
                return;

            if (!EquipmentPca && GetPcaAllowedOnGate())
            {
                Logger.Information($"Automation: Placing PCA on {phase}");
                await Aircraft.SetEquipmentPca(true);
                await Task.Delay(500, RequestToken);
            }
            else if (EquipmentPca && Profile.PcaOverride && !GetPcaAllowedOnGate())
            {
                Logger.Information($"Automation: Disconnecting PCA (override State)");
                await Aircraft.SetEquipmentPca(false);
                await Task.Delay(500, RequestToken);
            }
        }

        protected virtual async Task SetGpu(string phase)
        {
            if (!ServiceGpu.IsCalled && !ServiceGpu.IsRunning &&
                    (UseGpuGsx == GsxGpuUsage.Always
                    || (UseGpuGsx == GsxGpuUsage.NoJetway && !HasGateJetway)))
            {
                Logger.Information($"Automation: Calling GSX GPU on {phase}");
                await ServiceGpu.Call();
                await Task.Delay(500, RequestToken);
            }

            if (GroundEquipmentTimes[GroundEquipment.GPU] > DateTime.MinValue && DateTime.Now < GroundEquipmentTimes[GroundEquipment.GPU] && !GsxController.IsGateConnected)
                return;

            if (HasGpuInternal && ((GpuRequireChocks && EquipmentChocks) || !GpuRequireChocks) && !EquipmentGpu && GetPowerWithApuAllowed())
            {
                Logger.Information($"Automation: Placing GPU on {phase}");
                await Aircraft.SetEquipmentPower(true);
                await Task.Delay(500, RequestToken);
            }
        }

        protected virtual async Task SetCones(string phase)
        {
            if (GroundEquipmentTimes[GroundEquipment.Cones] > DateTime.MinValue && DateTime.Now < GroundEquipmentTimes[GroundEquipment.Cones])
                return;

            if (HasCones && !EquipmentCones && (!HasChocks || EquipmentChocks))
            {
                Logger.Information($"Automation: Placing Cones on {phase}");
                await Aircraft.SetEquipmentCones(true);
                await Task.Delay(500, RequestToken);
            }
        }

        protected virtual async Task SetChocks(string phase)
        {
            if (GroundEquipmentTimes[GroundEquipment.Chocks] > DateTime.MinValue && DateTime.Now < GroundEquipmentTimes[GroundEquipment.Chocks])
                return;

            if (HasChocks && !EquipmentChocks)
            {
                Logger.Information($"Automation: Placing Chocks on {phase}");
                await Aircraft.SetEquipmentChocks(true);
                await Task.Delay(500, RequestToken);
            }
        }

        public virtual async Task PlaceGroundEquipment(string phase)
        {
            await SetChocks(phase);
            await SetCones(phase);
            await SetGpu(phase);
            if (HasPca)
                await SetPca(phase);
        }

        protected virtual Task RemoveGroundPower(bool force)
        {
            if (HasGpuInternal && ((EquipmentGpu && AvionicsPowered && !PowerConnected) || (EquipmentGpu && force)))
            {
                Logger.Information($"Automation: Removing Internal GPU");
                return Aircraft.SetEquipmentPower(false);
            }

            if ((UseGpuGsx != GsxGpuUsage.Never && GsxController.ServiceGpu.IsConnected && !GsxController.ServiceGpu.WasCanceled && AvionicsPowered && !PowerConnected)
                || (GsxController.ServiceGpu.IsConnected && force))
            {
                Logger.Information($"Automation: Removing GSX GPU");
                return ServiceGpu.Cancel();
            }

            return Task.CompletedTask;
        }

        public virtual async Task RemoveDepartureEquip()
        {
            //PCA
            if (HasPca && ApuRunning && ApuBleed && EquipmentPca && GsxController.AutomationState == AutomationState.Departure)
            {
                Logger.Information($"Automation: Disconnecting PCA (APU/Bleed On)");
                await Aircraft.SetEquipmentPca(false);
            }

            //Chocks on Tug attached
            if (HasChocks && Profile.ClearChocksOnTugAttach && (GsxController.ServicePushBack.PushStatus > 1 || GsxController.ServicePushBack.IsPinInserted)
                && EquipmentChocks && BrakeSet)
            {
                if (!GpuRequireChocks || (GpuRequireChocks && !EquipmentGpu))
                {
                    Logger.Information($"Automation: Removing Chocks on Tug attached");
                    await Aircraft.SetEquipmentChocks(false);
                }
                else if (GpuRequireChocks && !PowerConnected && (!PcaRequirePower || (PcaRequirePower && !EquipmentPca)))
                {
                    Logger.Information($"Automation: Removing GPU on Tug attached");
                    await Aircraft.SetEquipmentPower(false);
                }
            }
        }

        public virtual async Task RemoveGroundEquip(string reason, bool force = false)
        {
            bool last = EquipmentRemoved();

            if (HasPca && (EquipmentPca || force))
            {
                Logger.Information($"Automation: Removing PCA on {reason}");
                await Aircraft.SetEquipmentPca(false);
            }

            if (!HasPca || (HasPca && (!PcaRequirePower || (PcaRequirePower && !EquipmentPca))))
                await RemoveGroundPower(force);

            if ((HasChocks && BrakeSet && EquipmentChocks && ((GpuRequireChocks && GetPowerRemoved()) || !GpuRequireChocks))
                || (HasChocks && force))
            {
                Logger.Information($"Automation: Removing Chocks on {reason}");
                await Aircraft.SetEquipmentChocks(false);
            }

            if ((HasCones && EquipmentCones && GetChocksRemoved()) || (HasCones && force))
            {
                Logger.Information($"Automation: Removing Cones on {reason}");
                await Aircraft.SetEquipmentCones(false);
            }

            if (!last)
                await Task.Delay(AppService.Instance.Config.StateMachineInterval, RequestToken);
        }

        public virtual bool EquipmentPlaced()
        {
            return Aircraft?.IsConnected == true && GetPowerPlaced() && GetChocksPlaced() && GetConesPlaced() && GetPcaPlaced();
        }

        protected virtual bool GetPowerWithApuAllowed()
        {
            return GsxController.AutomationState == AutomationState.Arrival || Profile.ConnectGpuWithApuRunning || (!Profile.ConnectGpuWithApuRunning && !ApuRunning);
        }

        protected virtual bool GetPowerPlaced()
        {
            return (!HasGpuInternal && UseGpuGsx == GsxGpuUsage.Never) || (HasGpuInternal && (EquipmentGpu || (!EquipmentGpu && !GetPowerWithApuAllowed()))) || (ServiceGpu.IsConnected && UseGpuGsx != GsxGpuUsage.Never);
        }

        protected virtual bool GetChocksPlaced()
        {
            return !HasChocks || (HasChocks && EquipmentChocks);
        }

        protected virtual bool GetConesPlaced()
        {
            return !HasCones || (HasCones && EquipmentCones);
        }

        protected virtual bool GetPcaAllowedOnGate()
        {
            return ConnectPca == 1 || (ConnectPca == 2 && HasGateJetway);
        }

        protected virtual bool GetPcaPlaced()
        {
            if (!HasPca)
                return true;

            if (!Profile.PcaOverride)
                return EquipmentPca;

            return EquipmentPca == GetPcaAllowedOnGate();
        }

        public virtual bool EquipmentRemoved()
        {
            return Aircraft?.IsConnected == true && GetPowerRemoved() && GetChocksRemoved() && GetConesRemoved() && GetPcaRemoved();
        }

        protected virtual bool GetPowerRemoved()
        {
            return (!HasGpuInternal && !ServiceGpu.IsConnected) || (HasGpuInternal && !EquipmentGpu);
        }

        protected virtual bool GetChocksRemoved()
        {
            return !HasChocks || (HasChocks && !EquipmentChocks);
        }

        protected virtual bool GetConesRemoved()
        {
            return !HasCones || (HasCones && !EquipmentCones);
        }

        protected virtual bool GetPcaRemoved()
        {
            return !HasPca || (HasPca && !EquipmentPca);
        }
    }
}
