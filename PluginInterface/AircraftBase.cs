using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppFramework.ResourceStores;
using CFIT.AppLogger;
using CFIT.SimConnectLib.SimResources;
using CFIT.SimConnectLib.SimVars;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Any2GSX.PluginInterface
{
    public abstract class AircraftBase(IAppResources appResources)
    {
        public virtual IAppResources AppResources => appResources;
        public virtual IConfig Config => AppResources.AppConfig;
        public virtual ISettingProfile ISettingProfile => AppResources.ISettingProfile;
        public virtual IProductDefinition ProductDefinition => AppResources.ProductDefinition;
        public virtual ReceiverStore ReceiverStore => AppResources.ReceiverStore;
        public virtual SimStore SimStore => AppResources.SimStore;
        public virtual string AircraftString => AppResources.AircraftString;
        public virtual IGsxController GsxController => AppResources.IGsxController;
        public virtual CancellationToken Token => AppResources.Token;
        public virtual ICommBus CommBus => AppResources.ICommBus;
        public virtual IFlightplan Flightplan => AppResources.IFlightplan;
        public virtual bool IsInitialized { get; protected set; } = false;
        public virtual bool IsExecutionAllowed { get; protected set; } = true;
        public abstract bool IsConnected { get; }
        public virtual int RunIntervalMs { get; protected set; } = 1000;
        public virtual int RefuelIntervalMs { get; protected set; } = 1000;
        protected virtual List<string> SettingVariables { get; } = [];

        public virtual bool IsCargo => GetIsCargo().Result;
        protected virtual ISimResourceSubscription SubSpeed { get; set; }
        public virtual int GroundSpeed => GetSpeed().Result;
        protected virtual ISimResourceSubscription SubEngine1 { get; set; }
        protected virtual bool Engine1 => GetEngine1().Result;
        protected virtual ISimResourceSubscription SubEngine2 { get; set; }
        protected virtual bool Engine2 => GetEngine2().Result;
        public virtual bool IsEngineRunning => GetEngineRunning().Result;
        public virtual bool ReadyForDepartureServices => GetReadyDepartureServices().Result;
        public virtual bool SmartButtonRequest => GetSmartButtonRequest().Result;
        protected virtual ISimResourceSubscription SubSmartButton { get; set; }
        protected virtual bool SmartButtonReceived { get; set; } = false;
        protected virtual ISimResourceSubscription SubDepartureTrigger { get; set; }
        public virtual DisplayUnit UnitAircraft => GetAircraftUnits().Result;
        protected virtual ISimResourceSubscription SubFuelOnBoardKg { get; set; }
        public virtual double FuelOnBoardKg => GetFuelOnBoardKg().Result;
        protected virtual ISimResourceSubscription SubFuelCapacityGallon { get; set; }
        public virtual double FuelCapacityGallon => SubFuelCapacityGallon?.GetNumber() ?? 0;
        protected virtual ISimResourceSubscription SubWeightTotalKg { get; set; }
        public virtual double WeightTotalKg => GetWeightTotalKg().Result;
        public virtual double WeightZeroFuelKg => GetWeightZeroFuelKg().Result;
        public virtual int PaxOnBoard { get; protected set; } = 0;
        public virtual bool IsBoardingCompleted => GetIsBoardingCompleted().Result;
        protected virtual ISimResourceSubscription SubMsfsAvionicPowered { get; set; }
        public virtual bool IsAvionicPowered => GetAvionicPowered().Result;
        protected virtual ISimResourceSubscription SubMsfsApuRunning { get; set; }
        public virtual bool IsApuRunning => GetApuRunning().Result;
        protected virtual ISimResourceSubscription SubMsfsApuBleedOn { get; set; }
        public virtual bool IsApuBleedOn => GetApuBleedOn().Result;
        protected virtual ISimResourceSubscription SubMsfsPowerConnected { get; set; }
        public virtual bool IsExternalPowerConnected => GetExternalPowerConnected().Result;

        public virtual bool HasFuelSynch => GetHasFuelSynch().Result;
        public virtual bool CanSetPayload => GetCanSetPayload().Result;
        public virtual bool HasFobSaveRestore => GetHasFobSaveRestore().Result;
        public virtual bool IsFuelOnStairSide => GetIsFuelOnStairSide().Result;
        public virtual bool HasGpuInternal => GetHasGpuInternal().Result;
        public virtual bool UseGpuGsx => GetUseGpuGsx().Result;
        public virtual bool HasChocks => GetHasChocks().Result;
        public virtual bool HasCones => GetHasCones().Result;
        public virtual bool HasPca => GetHasPca().Result;

        protected virtual ISimResourceSubscription SubMsfsPowerAvail { get; set; }
        public virtual bool EquipmentPower => GetExternalPowerAvailable().Result;
        public virtual bool EquipmentChocks => GetEquipmentChocks().Result;
        public virtual bool EquipmentCones => GetEquipmentCones().Result;
        public virtual bool EquipmentPca => GetEquipmentPca().Result;

        public virtual bool HasOpenDoors => GetHasOpenDoors().Result;
        public virtual bool HasAirStairForward => GetHasAirStairForward().Result;
        public virtual bool HasAirStairAft => GetHasAirStairAft().Result;
        protected virtual ISimResourceSubscription SubMsfsParkingBrake { get; set; }
        public virtual bool IsBrakeSet => GetBrakeSet().Result;
        protected virtual ISimResourceSubscription SubMsfsParkingBrakeSet { get; set; }

        protected virtual ISimResourceSubscription SubMsfsLightNav { get; set; }
        public virtual bool LightNav => GetLightNav().Result;
        protected virtual ISimResourceSubscription SubMsfsLightBeacon { get; set; }
        public virtual bool LightBeacon => GetLightBeacon().Result;

        public virtual async Task Init()
        {
            if (!IsInitialized)
            {
                SubFuelCapacityGallon = SimStore.AddVariable("FUEL TOTAL CAPACITY", SimUnitType.Gallon);
                SubFuelOnBoardKg = SimStore.AddVariable("FUEL TOTAL QUANTITY WEIGHT", SimUnitType.Kilogram);
                SubWeightTotalKg = SimStore.AddVariable("TOTAL WEIGHT", SimUnitType.Kilogram);
                SubSpeed = SimStore.AddVariable("GPS GROUND SPEED", SimUnitType.Knots);
                RegisterVariables();

                await DoInit();
                if (ISettingProfile.HasSetting<int>(GenericSettings.OptionAircraftInitDelay, out int delay))
                {
                    Logger.Debug($"Waiting {delay}ms for Aircraft to intialize");
                    await Task.Delay(delay, AppResources.RequestToken);
                }

                SubMsfsParkingBrakeSet ??= SimStore.AddEvent("PARKING_BRAKE_SET");
            }

            IsExecutionAllowed = true;
        }

        protected virtual ISimResourceSubscription AddVariableFromSettings(string keyName, string keyUnit = "")
        {
            ISimResourceSubscription sub = null;
            try
            {
                if (ISettingProfile.HasSetting<string>(keyName, out _))
                {
                    string name = ISettingProfile.GetSetting<string>(keyName);
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        if (!string.IsNullOrWhiteSpace(keyUnit) && ISettingProfile.HasSetting<string>(keyUnit, out _))
                            sub = SimStore.AddVariable(name, ISettingProfile.GetSetting<string>(keyUnit));
                        else
                            sub = SimStore.AddVariable(name, SimUnitType.Number);

                        if (sub != null)
                        {
                            Logger.Debug($"Added Variable '{sub.Name}' ('{name}') from Settings");
                            SettingVariables.Add(sub.Name);
                        }
                    }
                    else if (keyName != GenericSettings.VarDepartTriggerName)
                        Logger.Warning($"Could not get Name for Setting Variable of Key '{keyName}'");
                    else
                        Logger.Debug($"Could not get Name for Setting Variable of Key '{keyName}'");
                }
                else
                    Logger.Warning($"Could not get Variable for Setting Key '{keyName}'");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            return sub;
        }

        protected virtual void RegisterVariables()
        {
            SubEngine1 = AddVariableFromSettings(GenericSettings.VarEngine1Name, GenericSettings.VarEngine1Unit);
            SubEngine2 = AddVariableFromSettings(GenericSettings.VarEngine2Name, GenericSettings.VarEngine2Unit);

            SubMsfsAvionicPowered = AddVariableFromSettings(GenericSettings.VarPowerAvionicName, GenericSettings.VarPowerAvionicUnit);
            SubMsfsPowerAvail = AddVariableFromSettings(GenericSettings.VarPowerExtAvailName, GenericSettings.VarPowerExtAvailUnit);
            SubMsfsPowerConnected = AddVariableFromSettings(GenericSettings.VarPowerExtConnName, GenericSettings.VarPowerExtConnUnit);

            SubMsfsApuRunning = AddVariableFromSettings(GenericSettings.VarApuRunningName, GenericSettings.VarApuRunningUnit);
            SubMsfsApuBleedOn = AddVariableFromSettings(GenericSettings.VarApuBleedOnName, GenericSettings.VarApuBleedOnUnit);

            SubMsfsLightNav = AddVariableFromSettings(GenericSettings.VarLightNavName, GenericSettings.VarLightNavUnit);
            SubMsfsLightBeacon = AddVariableFromSettings(GenericSettings.VarLightBeaconName, GenericSettings.VarLightBeaconUnit);

            SubMsfsParkingBrake = AddVariableFromSettings(GenericSettings.VarParkBrakeName, GenericSettings.VarParkBrakeUnit);

            RegisterSmartButton();

            SubDepartureTrigger = AddVariableFromSettings(GenericSettings.VarDepartTriggerName, GenericSettings.VarDepartTriggerUnit);
        }

        protected virtual void RegisterSmartButton()
        {
            SubSmartButton = AddVariableFromSettings(GenericSettings.VarSmartButtonName, GenericSettings.VarSmartButtonUnit);
            if (SubSmartButton != null)
                SubSmartButton.OnReceived += OnSmartButtonValue;
        }

        protected virtual void UnregisterVariables()
        {
            try
            {
                foreach (var variable in SettingVariables)
                {
                    if (!string.IsNullOrWhiteSpace(variable))
                        SimStore.Remove(variable);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            SettingVariables.Clear();
        }

        protected abstract Task DoInit();

        public virtual Task Stop()
        {
            IsExecutionAllowed = false;
            DoStop();
            UnregisterVariables();
            FreeSubscriptions();
            Logger.Debug($"Aircraft Interface {this.GetType().Name} stopped");
            return Task.CompletedTask;
        }

        protected abstract Task DoStop();

        protected virtual void FreeSubscriptions()
        {
            SimStore?.Remove("GPS GROUND SPEED");
            SimStore?.Remove("FUEL TOTAL CAPACITY");
            SimStore?.Remove("FUEL TOTAL QUANTITY WEIGHT");
            SimStore?.Remove("TOTAL WEIGHT");
            SimStore?.Remove("PARKING_BRAKE_SET");
        }

        public virtual Task OnCouatlStarted()
        {
            return Task.CompletedTask;
        }

        public virtual Task OnAutomationStateChange(AutomationState state)
        {
            return Task.CompletedTask;
        }

        public virtual Task CheckConnection()
        {
            return Task.CompletedTask;
        }

        public abstract Task RunInterval();

        protected virtual Task<bool> GetIsCargo()
        {
            return Task.FromResult(ISettingProfile.GetSetting<bool>(GenericSettings.OptionAircraftIsCargo));
        }

        protected virtual Task<int> GetSpeed()
        {
            return Task.FromResult((int)(SubSpeed?.GetNumber() ?? 0));
        }

        protected virtual Task<bool> GetEngine1()
        {
            return Task.FromResult(SubEngine1?.IsActive != null && SubEngine1.GetNumber() > 0);
        }

        protected virtual Task<bool> GetEngine2()
        {
            return Task.FromResult(SubEngine2?.IsActive != null && SubEngine2.GetNumber() > 0);
        }

        protected virtual Task<bool> GetEngineRunning()
        {
            return Task.FromResult(Engine1 || Engine2);
        }

        protected virtual Task<bool> GetReadyDepartureServices()
        {
            bool trigger = true;
            if (SubDepartureTrigger != null)
            {
                double value = SubDepartureTrigger.GetNumber();
                Comparison comp = (Comparison)ISettingProfile.GetSetting<int>(GenericSettings.VarDepartTriggerComp);
                double target = ISettingProfile.GetSetting<double>(GenericSettings.VarDepartTriggerValue);
                trigger = CompareValues(comp, value, target);
            }

            return Task.FromResult(IsAvionicPowered && IsExternalPowerConnected && LightNav && trigger);
        }

        public virtual Task<bool> GetSmartButtonRequest()
        {
            return Task.FromResult(SmartButtonReceived);
        }

        protected virtual void OnSmartButtonValue(ISimResourceSubscription sub, object data)
        {
            try
            {
                if (SmartButtonReceived)
                    return;

                double value = sub.GetNumber();
                Comparison comp = (Comparison)ISettingProfile.GetSetting<int>(GenericSettings.VarSmartButtonComp);
                double target = ISettingProfile.GetSetting<double>(GenericSettings.VarSmartButtonValue);
                SmartButtonReceived = CompareValues(comp, value, target);
                Logger.Debug($"Compared SmartButton Value '{value}' {comp} '{target}': {SmartButtonReceived}");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public static bool CompareValues(Comparison comp, double value, double target)
        {
            return comp switch
            {
                Comparison.LESS => value < target,
                Comparison.LESS_EQUAL => value <= target,
                Comparison.GREATER => value > target,
                Comparison.GREATER_EQUAL => value >= target,
                Comparison.EQUAL => value == target,
                Comparison.NOT_EQUAL => value != target,
                _ => false,
            };
        }

        public virtual async Task ResetSmartButton()
        {
            try
            {
                SmartButtonReceived = false;
                string code = ISettingProfile.GetSetting<string>(GenericSettings.VarSmartButtonReset);
                if (!string.IsNullOrWhiteSpace(code))
                {
                    Logger.Debug($"Executing Reset Code '{code}'");
                    await CommBus.ExecuteCalculatorCode(code);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public virtual Task<DisplayUnit> GetAircraftUnits()
        {
            return Task.FromResult(AppResources.IFlightplan.Unit);
        }

        protected virtual Task<double> GetFuelOnBoardKg()
        {
            return Task.FromResult(SubFuelOnBoardKg?.GetNumber() ?? 0);
        }

        protected virtual Task<double> GetWeightTotalKg()
        {
            return Task.FromResult(SubWeightTotalKg?.GetNumber() ?? 0);
        }

        protected virtual Task<double> GetWeightZeroFuelKg()
        {
            return Task.FromResult(WeightTotalKg - FuelOnBoardKg);
        }

        protected virtual Task<bool> GetAvionicPowered()
        {
            return Task.FromResult(SubMsfsAvionicPowered?.GetNumber() > 0);
        }

        protected virtual Task<bool> GetApuRunning()
        {
            return Task.FromResult(SubMsfsApuRunning?.GetNumber() > 0);
        }

        protected virtual Task<bool> GetApuBleedOn()
        {
            return Task.FromResult(SubMsfsApuBleedOn?.GetNumber() > 0);
        }

        protected virtual Task<bool> GetExternalPowerConnected()
        {
            return Task.FromResult(SubMsfsPowerConnected?.GetNumber() > 0);
        }

        protected virtual Task<bool> GetExternalPowerAvailable()
        {
            return Task.FromResult(SubMsfsPowerAvail?.GetNumber() > 0);
        }

        public virtual Task<bool> GetHasFobSaveRestore()
        {
            return Task.FromResult(false);
        }

        public virtual Task<bool> GetHasFuelSynch()
        {
            return Task.FromResult(false);
        }

        public virtual Task<bool> GetCanSetPayload()
        {
            return Task.FromResult(false);
        }

        public virtual Task<bool> GetIsFuelOnStairSide()
        {
            if (ISettingProfile.HasSetting<bool>(GenericSettings.OptionAircraftRefuelStair, out bool value))
                return Task.FromResult(value);
            else
                return Task.FromResult(false);
        }

        public virtual Task<bool> GetHasGpuInternal()
        {
            return Task.FromResult(false);
        }

        public virtual Task<bool> GetUseGpuGsx()
        {
            return Task.FromResult(false);
        }

        public virtual Task<bool> GetHasChocks()
        {
            return Task.FromResult(false);
        }

        public virtual Task<bool> GetHasCones()
        {
            return Task.FromResult(false);
        }

        public virtual Task<bool> GetHasPca()
        {
            return Task.FromResult(false);
        }

        protected virtual Task<bool> GetEquipmentChocks()
        {
            return Task.FromResult(false);
        }

        protected virtual Task<bool> GetEquipmentCones()
        {
            return Task.FromResult(false);
        }

        protected virtual Task<bool> GetEquipmentPca()
        {
            return Task.FromResult(false);
        }

        protected virtual Task<bool> GetBrakeSet()
        {
            return Task.FromResult(SubMsfsParkingBrake?.GetNumber() > 0);
        }

        protected virtual Task<bool> GetLightNav()
        {
            return Task.FromResult(IsAvionicPowered && SubMsfsLightNav?.GetNumber() > 0);
        }

        protected virtual Task<bool> GetLightBeacon()
        {
            return Task.FromResult(IsAvionicPowered && SubMsfsLightBeacon?.GetNumber() > 0);
        }

        public virtual async Task SetParkingBrake(bool state)
        {
            await SubMsfsParkingBrakeSet.WriteValue(state);
        }

        public virtual Task SetEquipmentPower(bool state, bool force = false)
        {
            return Task.CompletedTask;
        }

        public virtual Task SetEquipmentChocks(bool state, bool force = false)
        {
            return Task.CompletedTask;
        }

        public virtual Task SetEquipmentCones(bool state, bool force = false)
        {
            return Task.CompletedTask;
        }

        public virtual Task SetEquipmentPca(bool state, bool force = false)
        {
            return Task.CompletedTask;
        }

        protected virtual Task<bool> GetHasOpenDoors()
        {
            return Task.FromResult(false);
        }

        public virtual Task DoorsAllClose()
        {
            return Task.CompletedTask;
        }

        public virtual Task<bool> GetHasAirStairForward()
        {
            return Task.FromResult(false);
        }

        public virtual Task<bool> GetHasAirStairAft()
        {
            return Task.FromResult(false);
        }

        public virtual Task OnDoorTrigger(GsxDoor door, bool trigger)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnLoaderAttached(GsxDoor door, bool trigger)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnJetwayChange(GsxServiceState state)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnStairChange(GsxServiceState state)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnStairOperationChange(GsxServiceState state)
        {
            return Task.CompletedTask;
        }

        public virtual Task RefuelActive()
        {
            return Task.CompletedTask;
        }

        public virtual Task SetFuelOnBoardKg(double fuelKg)
        {
            return Task.CompletedTask;
        }

        public virtual Task RefuelStart(double fuelTargetKg)
        {
            return Task.CompletedTask;
        }


        public virtual async Task RefuelTick(double stepKg, double fuelOnBoardKg)
        {
            await SetFuelOnBoardKg(fuelOnBoardKg);
        }

        public virtual async Task RefuelStop(double fuelTargetKg, bool setTarget)
        {
            Logger.Debug($"RefuelStop: setTarget {setTarget}");
            if (setTarget)
                await SetFuelOnBoardKg(fuelTargetKg);
        }

        public virtual Task RefuelCompleted()
        {
            return Task.CompletedTask;
        }

        public virtual Task SetPayloadEmpty()
        {
            return Task.CompletedTask;
        }

        public virtual Task PushStateChange(GsxServiceState state)
        {
            return Task.CompletedTask;
        }

        public virtual Task PushOperationChange(int status)
        {
            return Task.CompletedTask;
        }

        public virtual Task BoardActive(int paxTarget, double cargoTargetKg)
        {
            return Task.CompletedTask;
        }

        public virtual Task BoardChangePax(int paxOnBoard, double weightPerPaxKg)
        {
            PaxOnBoard = paxOnBoard;
            return Task.CompletedTask;
        }

        public virtual Task BoardChangeCargo(int progressLoad, double cargoOnBoardKg)
        {
            return Task.CompletedTask;
        }

        public virtual Task BoardLoadingChange(GsxDoor door, bool state)
        {
            return Task.CompletedTask;
        }

        protected virtual Task<bool> GetIsBoardingCompleted()
        {
            return Task.FromResult(Flightplan?.IsLoaded == true && ReadyForDepartureServices && GetWeightTotalKg().Result >= Flightplan.WeightTotalRampKg - Config.FuelCompareVariance);
        }

        public virtual Task SetCargoCrew(int paxOnBoard, double weightPerPaxKg)
        {
            PaxOnBoard = paxOnBoard;
            Logger.Debug($"PaxOnBoard changed to {PaxOnBoard}");
            return Task.CompletedTask;
        }

        public virtual Task BoardCompleted(int paxTarget, double weightPerPaxKg, double cargoTargetKg)
        {
            PaxOnBoard = paxTarget;
            return Task.CompletedTask;
        }

        public virtual Task DeboardActive()
        {
            return Task.CompletedTask;
        }

        public virtual Task DeboardChangePax(int paxOnBoard, int gsxTotal, double weightPerPaxKg)
        {
            PaxOnBoard = paxOnBoard;
            return Task.CompletedTask;
        }

        public virtual Task DeboardChangeCargo(int progressUnload, double cargoOnBoardKg)
        {
            return Task.CompletedTask;
        }

        public virtual Task DeboardUnloadingChange(GsxDoor door, bool state)
        {
            return Task.CompletedTask;
        }

        public virtual Task DeboardCompleted()
        {
            PaxOnBoard = 0;
            return Task.CompletedTask;
        }
    }
}
