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
        public IAppResources AppResources => appResources;
        public IConfig Config => AppResources.AppConfig;
        public ISettingProfile ISettingProfile => AppResources.ISettingProfile;
        public IProductDefinition ProductDefinition => AppResources.ProductDefinition;
        public SimStore SimStore => AppResources.SimStore;
        public string AircraftString => AppResources.AircraftString;
        public IGsxController GsxController => AppResources.IGsxController;
        public CancellationToken Token => AppResources.Token;
        public ICommBus CommBus => AppResources.ICommBus;
        public IFlightplan Flightplan => AppResources.IFlightplan;
        public bool IsInitialized { get; protected set; } = false;
        public bool IsExecutionAllowed { get; protected set; } = true;
        public abstract bool IsConnected { get; }
        public int InitDelay => ISettingProfile.HasSetting<int>(GenericSettings.OptionAircraftInitDelay, out int delay) ? delay : 0;
        public virtual int RunIntervalMs { get; protected set; } = 1000;
        public virtual int RefuelIntervalMs { get; protected set; } = 1000;
        protected List<string> SettingVariables { get; } = [];

        protected ISimResourceSubscription SubSpeed { get; set; }
        public double Speed => SubSpeed?.GetNumber() ?? 0.0;
        protected ISimResourceSubscription SubEngine1 { get; set; }
        public bool Engine1 => SubEngine1?.GetNumber() > 0;
        protected ISimResourceSubscription SubEngine2 { get; set; }
        public bool Engine2 => SubEngine2?.GetNumber() > 0;
        protected ISimResourceSubscription SubSmartButton { get; set; }
        protected bool SmartButtonReceived { get; set; } = false;
        protected ISimResourceSubscription SubDepartureTrigger { get; set; }
        public double DepartureTrigger => SubDepartureTrigger?.GetNumber() ?? 0.0;
        protected ISimResourceSubscription SubFuelOnBoardKg { get; set; }
        public double FuelOnBoardKg => SubFuelOnBoardKg?.GetNumber() ?? 0.0;
        protected ISimResourceSubscription SubFuelCapacityGallon { get; set; }
        public double FuelCapacityGallon => SubFuelCapacityGallon?.GetNumber() ?? 0;
        protected ISimResourceSubscription SubWeightTotalKg { get; set; }
        public double WeightTotalKg => SubWeightTotalKg?.GetNumber() ?? 0.0;
        public double WeightZeroFuelKg => WeightTotalKg - FuelOnBoardKg;
        protected ISimResourceSubscription SubMsfsAvionicPowered { get; set; }
        public bool AvionicPowered => SubMsfsAvionicPowered?.GetNumber() > 0;
        protected ISimResourceSubscription SubMsfsApuRunning { get; set; }
        public bool ApuRunning => SubMsfsApuRunning?.GetNumber() > 0;
        protected ISimResourceSubscription SubMsfsApuBleedOn { get; set; }
        public bool ApuBleedOn => SubMsfsApuBleedOn?.GetNumber() > 0;
        protected ISimResourceSubscription SubMsfsPowerConnected { get; set; }
        public bool PowerConnected => SubMsfsPowerConnected?.GetNumber() > 0;
        protected ISimResourceSubscription SubMsfsPowerAvail { get; set; }
        public bool PowerAvail => SubMsfsPowerAvail?.GetNumber() > 0;
        protected ISimResourceSubscription SubMsfsParkingBrake { get; set; }
        public bool ParkingBrake => SubMsfsParkingBrake?.GetNumber() > 0;
        protected ISimResourceSubscription SubMsfsParkingBrakeSetCommand { get; set; }
        protected ISimResourceSubscription SubMsfsLightNav { get; set; }
        public bool LightNav => SubMsfsLightNav?.GetNumber() > 0;
        protected ISimResourceSubscription SubMsfsLightBeacon { get; set; }
        public bool LightBeacon => SubMsfsLightBeacon?.GetNumber() > 0;

        public async Task Init()
        {
            if (!IsInitialized)
            {
                SubFuelCapacityGallon = SimStore.AddVariable("FUEL TOTAL CAPACITY", SimUnitType.Gallon);
                SubFuelOnBoardKg = SimStore.AddVariable("FUEL TOTAL QUANTITY WEIGHT", SimUnitType.Kilogram);
                SubWeightTotalKg = SimStore.AddVariable("TOTAL WEIGHT", SimUnitType.Kilogram);
                SubSpeed = SimStore.AddVariable("GPS GROUND SPEED", SimUnitType.Knots);
                RegisterVariables();

                await DoInit();
                if (InitDelay > 0)
                {
                    Logger.Debug($"Waiting {InitDelay}ms for Aircraft to intialize");
                    await Task.Delay(InitDelay, AppResources.RequestToken);
                }

                SubMsfsParkingBrakeSetCommand ??= SimStore.AddEvent("PARKING_BRAKE_SET");
            }

            IsExecutionAllowed = true;
        }

        protected ISimResourceSubscription AddVariableFromSettings(string keyName, string keyUnit = "")
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
                    else
                        Logger.Debug($"Could not get Name for Setting Variable of Key '{keyName}'");
                }
                else
                    Logger.Debug($"Could not get Variable for Setting Key '{keyName}'");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            return sub;
        }

        protected void RegisterVariables()
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

        protected void RegisterSmartButton()
        {
            string name = ISettingProfile.GetSetting<string>(GenericSettings.VarSmartButtonName) ?? "";
            if (name != GenericSettings.VarSmartButtonDefault)
            {
                SubSmartButton = AddVariableFromSettings(GenericSettings.VarSmartButtonName, GenericSettings.VarSmartButtonUnit);
                SubSmartButton?.OnReceived += OnSmartButtonValue;
            }
        }

        protected void UnregisterVariables()
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

        public async Task Stop()
        {
            IsExecutionAllowed = false;
            SubSmartButton?.OnReceived -= OnSmartButtonValue;
            await DoStop();
            UnregisterVariables();
            FreeSubscriptions();
            Logger.Debug($"Aircraft Interface {this.GetType().Name} stopped");
            IsInitialized = false;

        }

        protected abstract Task DoStop();

        protected void FreeSubscriptions()
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

        public abstract Task CheckConnection();

        public abstract Task RunInterval();

        public virtual Task<bool> GetIsCargo()
        {
            return Task.FromResult(ISettingProfile?.GetSetting<bool>(GenericSettings.OptionAircraftIsCargo) ?? false);
        }

        public virtual Task<int> GetSpeed()
        {
            return Task.FromResult((int)Speed);
        }

        public virtual Task<bool> GetEngine1()
        {
            return Task.FromResult(SubEngine1?.IsActive != null && SubEngine1.GetNumber() > 0);
        }

        public virtual Task<bool> GetEngine2()
        {
            return Task.FromResult(SubEngine2?.IsActive != null && SubEngine2.GetNumber() > 0);
        }

        public virtual async Task<bool> GetEngineRunning()
        {
            return await GetEngine1() || await GetEngine2();
        }

        public virtual async Task<bool> GetReadyDepartureServices()
        {
            bool trigger = true;
            if (SubDepartureTrigger != null)
            {
                double value = SubDepartureTrigger.GetNumber();
                Comparison comp = (Comparison)ISettingProfile.GetSetting<int>(GenericSettings.VarDepartTriggerComp);
                double target = ISettingProfile.GetSetting<double>(GenericSettings.VarDepartTriggerValue);
                trigger = CompareValues(comp, value, target);
            }

            return await GetAvionicPowered() && await GetExternalPowerConnected() && await GetLightNav() && trigger;
        }

        public virtual Task<bool> GetSmartButtonRequest()
        {
            return Task.FromResult(SmartButtonReceived);
        }

        public virtual Task OnSmartButtonValue(ISimResourceSubscription sub, object data)
        {
            try
            {
                if (SmartButtonReceived)
                    return Task.CompletedTask;

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

            return Task.CompletedTask;
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

        public virtual Task ResetSmartButton()
        {
            try
            {

                if (SmartButtonReceived)
                {
                    string code = ISettingProfile.GetSetting<string>(GenericSettings.VarSmartButtonReset);
                    if (!string.IsNullOrWhiteSpace(code))
                    {
                        Logger.Debug($"Executing Reset Code '{code}'");
                        return CommBus.ExecuteCalculatorCode(code);
                    }
                }
                SmartButtonReceived = false;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return Task.CompletedTask;
        }

        public virtual Task<DisplayUnit> GetAircraftUnits()
        {
            return Task.FromResult(AppResources.IFlightplan.Unit);
        }

        public virtual Task NotifyCockpit(CockpitNotification notification)
        {
            return Task.CompletedTask;
        }

        public virtual Task<double> GetFuelOnBoardKg()
        {
            return Task.FromResult(FuelOnBoardKg);
        }

        public virtual Task<double> GetWeightTotalKg()
        {
            return Task.FromResult(WeightTotalKg);
        }

        public virtual async Task<double> GetWeightZeroFuelKg()
        {
            return await GetWeightTotalKg() - await GetFuelOnBoardKg();
        }

        public virtual Task<bool> GetAvionicPowered()
        {
            return Task.FromResult(AvionicPowered || ApuRunning);
        }

        public virtual Task<bool> GetApuRunning()
        {
            return Task.FromResult(ApuRunning);
        }

        public virtual Task<bool> GetApuBleedOn()
        {
            return Task.FromResult(ApuRunning && ApuBleedOn);
        }

        public virtual Task<bool> GetExternalPowerConnected()
        {
            return Task.FromResult(PowerConnected);
        }

        public virtual Task<bool> GetExternalPowerAvailable()
        {
            return Task.FromResult(PowerAvail);
        }

        public virtual Task<bool> GetHasFobSaveRestore()
        {
            return Task.FromResult(false);
        }

        public virtual Task<bool> GetHasFuelSync()
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

        public virtual Task<bool> GetGpuRequireChocks()
        {
            return Task.FromResult(false);
        }

        public virtual Task<GsxGpuUsage> GetUseGpuGsx()
        {
            if (ISettingProfile.HasSetting<GsxGpuUsage>(GenericSettings.OptionAircraftGsxGpu, out GsxGpuUsage value))
                return Task.FromResult(value);
            else
                return Task.FromResult(GsxGpuUsage.Never);
        }

        public virtual Task<bool> GetSettingAutoMode()
        {
            return Task.FromResult(false);
        }

        public virtual Task<bool> GetSettingProgRefuel()
        {
            return Task.FromResult(false);
        }

        public virtual Task<bool> GetSettingDetectCustFuel()
        {
            return Task.FromResult(false);
        }

        public virtual Task<bool> GetSettingAdvAutomation()
        {
            return Task.FromResult(true);
        }

        public virtual Task<bool> GetSettingFuelDialog()
        {
            if (ISettingProfile.HasSetting<bool>(GenericSettings.OptionAircraftFuelDialog, out bool value))
                return Task.FromResult(value);
            else
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

        public virtual Task<bool> GetPcaRequirePower()
        {
            return Task.FromResult(false);
        }

        public virtual Task<bool> GetEquipmentChocks()
        {
            return Task.FromResult(false);
        }

        public virtual Task<bool> GetEquipmentCones()
        {
            return Task.FromResult(false);
        }

        public virtual Task<bool> GetEquipmentPca()
        {
            return Task.FromResult(false);
        }

        public virtual Task BeforeWalkaroundSkip()
        {
            return Task.CompletedTask;
        }

        public virtual Task AfterWalkaroundSkip()
        {
            return Task.CompletedTask;
        }

        public virtual Task HandleWalkaroundEquipment()
        {
            return Task.CompletedTask;
        }

        public virtual Task<bool> GetBrakeSet()
        {
            return Task.FromResult(ParkingBrake);
        }

        public virtual async Task<bool> GetLightNav()
        {
            return LightNav && await GetAvionicPowered();
        }

        public virtual async Task<bool> GetLightBeacon()
        {
            return LightBeacon && await GetAvionicPowered();
        }

        public virtual Task SetParkingBrake(bool state)
        {
            return SubMsfsParkingBrakeSetCommand?.WriteValue(state);
        }

        public virtual Task SetExternalPowerAvailable(bool state)
        {
            return Task.CompletedTask;
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

        public virtual Task<bool> GetHasOpenDoors()
        {
            return Task.FromResult(false);
        }

        public virtual Task SetCargoDoors(bool state, bool force = false)
        {
            return Task.CompletedTask;
        }

        public virtual async Task DoorsAllClose()
        {
            await SetCargoDoors(false, true);
            await SetPanelRefuel(false);
            await SetPanelLavatory(false);
            await SetPanelWater(false);
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

        public virtual Task SetPanelLavatory(bool target)
        {
            return Task.CompletedTask;
        }

        public virtual Task SetPanelWater(bool target)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnLoaderAttached(GsxDoor door, bool attached)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnJetwayStateChange(GsxServiceState state, bool paxDoorAllowed)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnJetwayOperationChange(GsxServiceState state, bool paxDoorAllowed)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnStairStateChange(GsxServiceState state, bool paxDoorAllowed)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnStairOperationChange(GsxServiceState state, bool paxDoorAllowed)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnStairVerhicleChange(GsxVehicleStair stair, GsxVehicleStairState state, bool paxDoorAllowed)
        {
            return Task.CompletedTask;
        }

        public virtual Task SetPanelRefuel(bool target)
        {
            return Task.CompletedTask;
        }

        public virtual Task RefuelActive()
        {
            return Task.CompletedTask;
        }

        public virtual Task SetFuelOnBoardKg(double fuelOnBoardKg, double targetKg)
        {
            return Task.CompletedTask;
        }

        public virtual Task RefuelStart(double fuelTargetKg)
        {
            return Task.CompletedTask;
        }

        public virtual Task RefuelTick(bool isFuelInc, double stepKg, double fuelOnBoardKg, double fuelTargetKg)
        {
            return SetFuelOnBoardKg(fuelOnBoardKg, fuelTargetKg);
        }

        public virtual Task RefuelStop(double fuelTargetKg, bool setTarget)
        {
            if (setTarget)
                return SetFuelOnBoardKg(fuelTargetKg, fuelTargetKg);
            else
                return Task.CompletedTask;
        }

        public virtual Task RefuelCompleted()
        {
            return Task.CompletedTask;
        }

        public virtual async Task SetPayloadEmpty()
        {
            await SetPaxOnBoard(0, Flightplan.WeightPerPaxKg, 0);
            await SetCargoOnBoard(0, 0);
        }

        public virtual Task PushStateChange(GsxServiceState state)
        {
            return Task.CompletedTask;
        }

        public virtual Task PushOperationChange(int status)
        {
            return Task.CompletedTask;
        }

        public virtual Task<int> GetPaxOnBoard()
        {
            return Task.FromResult(Flightplan.CountPax);
        }

        public virtual Task<int> GetBagsOnBoard()
        {
            return Task.FromResult(Flightplan.CountBags);
        }

        public virtual Task SetPaxOnBoard(int paxOnBoard, double weightPerPaxKg, int paxTarget)
        {
            return Task.CompletedTask;
        }

        public virtual Task<double> GetCargoOnBoard()
        {
            return Task.FromResult(Flightplan.WeightCargoKg);
        }

        public virtual Task SetCargoOnBoard(double cargoOnBoardKg, double cargoTargetKg)
        {
            return Task.CompletedTask;
        }

        public virtual Task BoardRequested(int paxTarget, double cargoTargetKg)
        {
            return Task.CompletedTask;
        }

        public virtual Task BoardActive(int paxTarget, double cargoTargetKg)
        {
            return Task.CompletedTask;
        }

        public virtual Task BoardChangePax(int paxOnBoard, double weightPerPaxKg, int paxTarget)
        {
            return SetPaxOnBoard(paxOnBoard, weightPerPaxKg, paxTarget);
        }

        public virtual Task BoardChangeCargo(int progressLoad, double cargoOnBoardKg, double cargoPlannedKg)
        {
            return SetCargoOnBoard(cargoOnBoardKg, cargoPlannedKg);
        }

        public virtual async Task<bool> GetIsBoardingCompleted()
        {
            return Flightplan?.IsLoaded == true && await GetReadyDepartureServices() && (await GetWeightTotalKg() - await GetFuelOnBoardKg()) >= Flightplan.ZeroFuelRampKg - Config.FuelCompareVariance;
        }

        public virtual async Task BoardCompleted(int paxTarget, double weightPerPaxKg, double cargoTargetKg)
        {
            await SetPaxOnBoard(paxTarget, weightPerPaxKg, paxTarget);
            await SetCargoOnBoard(cargoTargetKg, cargoTargetKg);
        }

        public virtual Task DeboardRequested()
        {
            return Task.CompletedTask;
        }

        public virtual Task DeboardActive()
        {
            return Task.CompletedTask;
        }

        public virtual Task DeboardChangePax(int paxOnBoard, int gsxTotal, double weightPerPaxKg)
        {
            return SetPaxOnBoard(paxOnBoard, weightPerPaxKg, 0);
        }

        public virtual Task DeboardChangeCargo(int progressUnload, double cargoOnBoardKg)
        {
            return SetCargoOnBoard(cargoOnBoardKg, 0);
        }

        public virtual async Task DeboardCompleted()
        {
            await SetPaxOnBoard(0, Flightplan.WeightPerPaxKg, 0);
            await SetCargoOnBoard(0, 0);
        }

        public virtual Task OnFlightplanImport(IFlightplan ofp)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnFlightplanUnload()
        {
            return Task.CompletedTask;
        }

        public virtual Task GenerateLoadsheetPrelim()
        {
            return Task.CompletedTask;
        }

        public virtual Task GenerateLoadsheetFinal()
        {
            return Task.CompletedTask;
        }

        public virtual async Task<PayloadReport> GetPayload()
        {
            return new(Flightplan.Id)
            {
                CountPax = await GetPaxOnBoard(),
                CountBags = await GetBagsOnBoard(),
                WeightCargoKg = await GetCargoOnBoard(),
            };
        }
    }
}
