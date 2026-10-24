using Any2GSX.Aircraft;
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
    public class GroundEquipManager(AircraftState aircraftState)
    {
        protected virtual CancellationToken RequestToken => AppService.Instance.RequestToken;
        protected virtual AircraftController AircraftController => AppService.Instance?.AircraftController;
        protected virtual AircraftBase Aircraft => AircraftController?.Aircraft;
        protected virtual AircraftState State { get; } = aircraftState;
        protected virtual SettingProfile Profile => AppService.Instance?.SettingProfile;
        protected virtual GsxController GsxController => AppService.Instance?.GsxController;
        protected virtual GsxServiceGpu ServiceGpu => GsxController?.ServiceGpu;
        protected virtual GsxServiceJetway ServiceJetway => GsxController?.ServiceJetway;
        protected virtual int ConnectPca => Profile?.ConnectPca ?? 2;
        protected virtual bool PcaOverride => Profile?.PcaOverride ?? true;
        protected virtual bool HasGateJetway => GsxController?.HasGateJetway ?? true;

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
        }

        protected virtual async Task SetAircraftPower(bool target)
        {
            await Aircraft.SetEquipmentPower(target);
            if (AppService.Instance.Config.GsxSetGpuOnInternal)
                await ServiceGpu.SetGpuConnection(target);
        }

        protected virtual async Task SetPca(string phase)
        {
            if (GroundEquipmentTimes[GroundEquipment.PCA] > DateTime.MinValue && DateTime.Now < GroundEquipmentTimes[GroundEquipment.PCA])
                return;

            if (!GsxController.IsGateConnected && !State.EquipmentGpu)
                return;

            if (!State.EquipmentPca && GetPcaAllowedOnGate())
            {
                Logger.Information($"Automation: Placing PCA on {phase}");
                await Aircraft.SetEquipmentPca(true);
                await Task.Delay(500, RequestToken);
            }
            else if (State.EquipmentPca && PcaOverride && !GetPcaAllowedOnGate())
            {
                Logger.Information($"Automation: Disconnecting PCA (override State)");
                await Aircraft.SetEquipmentPca(false);
                await Task.Delay(500, RequestToken);
            }
        }

        protected virtual async Task SetGpu(string phase)
        {
            if (!ServiceGpu.IsCalled && !ServiceGpu.IsRunning &&
                    (State.UseGpuGsx == GsxGpuUsage.Always
                    || (State.UseGpuGsx == GsxGpuUsage.NoJetway && !HasGateJetway)))
            {
                Logger.Information($"Automation: Calling GSX GPU on {phase}");
                await ServiceGpu.Call();
                await Task.Delay(500, RequestToken);
            }

            if (GroundEquipmentTimes[GroundEquipment.GPU] > DateTime.MinValue && DateTime.Now < GroundEquipmentTimes[GroundEquipment.GPU] && !GsxController.IsGateConnected)
                return;

            if (State.HasGpuInternal && ((State.GpuRequireChocks && State.EquipmentChocks) || !State.GpuRequireChocks) && !State.EquipmentGpu && GetPowerWithApuAllowed())
            {
                Logger.Information($"Automation: Placing GPU on {phase}");
                await SetAircraftPower(true);
                await Task.Delay(500, RequestToken);
            }
        }

        protected virtual async Task SetCones(string phase)
        {
            if (GroundEquipmentTimes[GroundEquipment.Cones] > DateTime.MinValue && DateTime.Now < GroundEquipmentTimes[GroundEquipment.Cones])
                return;

            if (State.HasCones && !State.EquipmentCones && (!State.HasChocks || State.EquipmentChocks))
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

            if (State.HasChocks && !State.EquipmentChocks)
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
            if (State.HasPca)
                await SetPca(phase);
        }

        protected virtual Task RemoveGroundPower(bool force)
        {
            if (State.HasGpuInternal && ((State.EquipmentGpu && State.AvionicsPowered && !State.PowerConnected) || (State.EquipmentGpu && force)))
            {
                Logger.Information($"Automation: Removing Internal GPU");
                return SetAircraftPower(false);
            }

            if ((State.UseGpuGsx != GsxGpuUsage.Never && ServiceGpu.IsConnected && !ServiceGpu.WasCanceled && State.AvionicsPowered && !State.PowerConnected)
                || (ServiceGpu.IsConnected && force))
            {
                Logger.Information($"Automation: Removing GSX GPU");
                return ServiceGpu.Cancel();
            }

            return Task.CompletedTask;
        }

        public virtual async Task RemoveDepartureEquip()
        {
            //PCA
            if (State.HasPca && State.ApuRunning && State.ApuBleed && State.EquipmentPca && GsxController.AutomationState == AutomationState.Departure)
            {
                Logger.Information($"Automation: Disconnecting PCA (APU/Bleed On)");
                await Aircraft.SetEquipmentPca(false);
            }

            //Chocks on Tug attached
            if (State.HasChocks && Profile.ClearChocksOnTugAttach && (GsxController.ServicePushBack.PushStatus > 1 || GsxController.ServicePushBack.IsPinInserted)
                && State.EquipmentChocks && State.BrakeSet)
            {
                if (!State.GpuRequireChocks || (State.GpuRequireChocks && !State.EquipmentGpu))
                {
                    Logger.Information($"Automation: Removing Chocks on Tug attached");
                    await Aircraft.SetEquipmentChocks(false);
                }
                else if (State.GpuRequireChocks && !State.PowerConnected && (!State.PcaRequirePower || (State.PcaRequirePower && !State.EquipmentPca)))
                {
                    Logger.Information($"Automation: Removing GPU on Tug attached");
                    await SetAircraftPower(false);
                }
            }
        }

        public virtual async Task RemoveGroundEquip(string reason, bool force = false)
        {
            bool last = EquipmentRemoved();

            if (State.HasPca && (State.EquipmentPca || force))
            {
                Logger.Information($"Automation: Removing PCA on {reason}");
                await Aircraft.SetEquipmentPca(false);
            }

            if (!State.HasPca || (State.HasPca && (!State.PcaRequirePower || (State.PcaRequirePower && !State.EquipmentPca))))
                await RemoveGroundPower(force);

            if ((State.HasChocks && State.BrakeSet && State.EquipmentChocks && ((State.GpuRequireChocks && GetPowerRemoved()) || !State.GpuRequireChocks))
                || (State.HasChocks && force))
            {
                Logger.Information($"Automation: Removing Chocks on {reason}");
                await Aircraft.SetEquipmentChocks(false);
            }

            if ((State.HasCones && State.EquipmentCones && GetChocksRemoved()) || (State.HasCones && force))
            {
                Logger.Information($"Automation: Removing Cones on {reason}");
                await Aircraft.SetEquipmentCones(false);
            }

            if (!last)
                await Task.Delay(AppService.Instance.Config.StateMachineInterval, RequestToken);
        }

        public virtual bool EquipmentPlaced()
        {
            return AircraftController.IsAircraftConnected && GetPowerPlaced() && GetChocksPlaced() && GetConesPlaced() && GetPcaPlaced();
        }

        public virtual async Task<bool> CheckSetAirstairs(bool target, int delayChange = 3000)
        {
            bool changed = false;
            if (State.HasAirStairs && (Profile.DoorPreferAirstair || (!target && State.AirstairsState)))
            {
                if (await Aircraft.GetHasAirStairForward() == true && State.AirstairForward != target)
                {
                    Logger.Information($"Automation: {(target ? "Extend" : "Retract")} Front Airstair ({State.AirstairForward} => {target})");
                    await Aircraft.SetAirStairForward(target);
                    changed = true;
                    await Task.Delay(100, RequestToken);
                    if (target && State.HasCustomStairs && State.CustomStairsFront)
                    {
                        await Aircraft.SetCustomStairsFront(false);
                        await Task.Delay(100, RequestToken);
                    }
                }

                if (await Aircraft.GetHasAirStairAft() == true && State.AirstairAft != target)
                {
                    Logger.Information($"Automation: {(target ? "Extend" : "Retract")} Rear Airstair ({State.AirstairForward} => {target})");
                    await Aircraft.SetAirStairAft(target);
                    changed = true;
                    await Task.Delay(100, RequestToken);
                    if (target && State.HasCustomStairs && State.CustomStairsRear)
                    {
                        await Aircraft.SetCustomStairsRear(false);
                        await Task.Delay(100, RequestToken);
                    }
                }

                if (changed)
                {
                    await Task.Delay(delayChange, RequestToken);
                    await State.Refresh();
                }
            }

            return changed;
        }

        protected virtual bool GetPowerWithApuAllowed()
        {
            return GsxController.AutomationState == AutomationState.Arrival || Profile.ConnectGpuWithApuRunning || (!Profile.ConnectGpuWithApuRunning && !State.ApuRunning);
        }

        protected virtual bool GetPowerPlaced()
        {
            if (State.HasGpuInternal && State.EquipmentGpu && AppService.Instance.Config.GsxSetGpuOnInternal)
                ServiceGpu.SetGpuConnection(true);
            return (!State.HasGpuInternal && State.UseGpuGsx == GsxGpuUsage.Never) || (State.HasGpuInternal && (State.EquipmentGpu || (!State.EquipmentGpu && !GetPowerWithApuAllowed()))) || (ServiceGpu.IsConnected && State.UseGpuGsx != GsxGpuUsage.Never) || (ServiceJetway.IsConnected && State.UseGpuGsx == GsxGpuUsage.NoJetway);
        }

        protected virtual bool GetChocksPlaced()
        {
            return !State.HasChocks || (State.HasChocks && State.EquipmentChocks);
        }

        protected virtual bool GetConesPlaced()
        {
            return !State.HasCones || (State.HasCones && State.EquipmentCones);
        }

        protected virtual bool GetPcaAllowedOnGate()
        {
            return ConnectPca == 1 || (ConnectPca == 2 && HasGateJetway);
        }

        protected virtual bool GetPcaPlaced()
        {
            if (!State.HasPca)
                return true;

            if (!PcaOverride)
                return State.EquipmentPca;

            return State.EquipmentPca == GetPcaAllowedOnGate();
        }

        public virtual bool EquipmentRemoved()
        {
            return AircraftController.IsAircraftConnected && GetPowerRemoved() && GetChocksRemoved() && GetConesRemoved() && GetPcaRemoved();
        }

        protected virtual bool GetPowerRemoved()
        {
            return (!State.HasGpuInternal && !ServiceGpu.IsConnected) || (State.HasGpuInternal && !State.EquipmentGpu);
        }

        protected virtual bool GetChocksRemoved()
        {
            return !State.HasChocks || (State.HasChocks && !State.EquipmentChocks);
        }

        protected virtual bool GetConesRemoved()
        {
            return !State.HasCones || (State.HasCones && !State.EquipmentCones);
        }

        protected virtual bool GetPcaRemoved()
        {
            return !State.HasPca || (State.HasPca && !State.EquipmentPca);
        }
    }
}
