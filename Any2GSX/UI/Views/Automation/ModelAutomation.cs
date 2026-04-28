using Any2GSX.AppConfig;
using Any2GSX.GSX.Automation;
using Any2GSX.Notifications;
using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.ComponentModel;

namespace Any2GSX.UI.Views.Automation
{
    public partial class ModelAutomation : ModelBase<SettingProfile>
    {
        public override SettingProfile Source => AppService?.SettingProfile;

        public ModelAutomation(AppService appService) : base(appService?.SettingProfile, appService)
        {
            DepartureServices = new ModelDepartureServices(this) { AddAllowed = false };
            DepartureServices.CollectionChanged += (_, _) => SaveConfig();

            OperatorPreferences = new ModelOperatorPreferences(this);
            OperatorPreferences.CollectionChanged += (_, _) => SaveConfig();

            CompanyHubs = new ModelCompanyHubs(this);
            CompanyHubs.CollectionChanged += (_, _) => SaveConfig();

            this.PropertyChanged += OnSelfPropertyChanged;
            AppService.PluginCapabilitiesChanged += PluginCapabilitiesChanged;
        }

        protected virtual void OnProfileChanged(SettingProfile profile)
        {
            InhibitConfigSave = true;
            DepartureServices.NotifyCollectionChanged();
            OperatorPreferences.NotifyCollectionChanged();
            CompanyHubs.NotifyCollectionChanged();
            OnUnitChanged();
            NotifyPropertyChanged(string.Empty);
            InhibitConfigSave = false;
        }

        protected virtual void OnUnitChanged()
        {
            InhibitConfigSave = true;
            NotifyPropertyChanged(nameof(RefuelRateKgSec));
            NotifyPropertyChanged(nameof(FuelResetBaseKg));
            NotifyPropertyChanged(nameof(DisplayUnitCurrentString));
            InhibitConfigSave = false;
        }

        protected override void InitializeModel()
        {
            AppService.ProfileChanged += OnProfileChanged;
            Config.PropertyChanged += OnConfigPropertyChanged;
            InitializeMessageService();
        }

        protected virtual void PluginCapabilitiesChanged(PluginCapabilities pluginCapabilities)
        {
            PluginCapabilities = pluginCapabilities;
            NotifyPropertyChanged(nameof(HasNotificationPrep));
            NotifyPropertyChanged(nameof(HasNotificationFinal));
            NotifyPropertyChanged(nameof(HasNotificationChocks));
            NotifyPropertyChanged(nameof(HasNotificationTurn));
        }

        protected virtual void OnConfigPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e?.PropertyName == nameof(Config.DisplayUnitCurrent) || e?.PropertyName == nameof(Config.DisplayUnitCurrentString))
                OnUnitChanged();
        }

        protected virtual void OnSelfPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e?.PropertyName == nameof(SkipCrewBoardQuestion))
            {
                NotifyPropertyChanged(nameof(NotSkipCrewBoardQuestion));
                NotifyPropertyChanged(nameof(CanSetPilotTarget));
            }
            if (e?.PropertyName == nameof(SkipCrewDeboardQuestion))
            {
                NotifyPropertyChanged(nameof(NotSkipCrewDeboardQuestion));
                NotifyPropertyChanged(nameof(CanSetPilotTarget));
            }
            if (e?.PropertyName == nameof(AnswerCrewBoardQuestion))
                NotifyPropertyChanged(nameof(CanSetPilotTarget));
            if (e?.PropertyName == nameof(AnswerCrewDeboardQuestion))
                NotifyPropertyChanged(nameof(CanSetPilotTarget));
            if (e?.PropertyName == nameof(OperatorAutoSelect))
                NotifyPropertyChanged(nameof(OperatorPreferencEnabled));
        }

        [RelayCommand]
        public virtual void ShowVariableChecker()
        {
            var window = new VariableCheckDialog();
            window.ShowDialog();
        }

        public virtual string DisplayUnitCurrentString => Config.DisplayUnitCurrentString;
        [ObservableProperty]
        public partial PluginCapabilities PluginCapabilities { get; set; } = new();
        public virtual bool HasNotificationPrep => PluginCapabilities?.CockpitNotifications.HasFlag(CockpitNotification.PrepFinished) == true;
        public virtual bool HasNotificationFinal => PluginCapabilities?.CockpitNotifications.HasFlag(CockpitNotification.FinalReceived) == true;
        public virtual bool HasNotificationChocks => PluginCapabilities?.CockpitNotifications.HasFlag(CockpitNotification.ChocksPlaced) == true;
        public virtual bool HasNotificationTurn => PluginCapabilities?.CockpitNotifications.HasFlag(CockpitNotification.TurnReady) == true;

        //Gate & Doors
        public virtual bool CloseDoorsOnFinal { get => Source.CloseDoorsOnFinal; set => SetModelValue<bool>(value); }
        public virtual bool DoorPaxHandling { get => Source.DoorPaxHandling; set => SetModelValue<bool>(value); }
        public virtual bool DoorStairHandling { get => Source.DoorStairHandling; set => SetModelValue<bool>(value); }
        public virtual bool DoorServiceHandling { get => Source.DoorServiceHandling; set => SetModelValue<bool>(value); }
        public virtual bool DoorCargoHandling { get => Source.DoorCargoHandling; set => SetModelValue<bool>(value); }
        public virtual bool DoorCargoOpenOnActive { get => Source.DoorCargoOpenOnActive; set => SetModelValue<bool>(value); }
        public virtual int DoorCargoOpenCloseDelay { get => Source.DoorCargoOpenCloseDelay; set => SetModelValue<int>(value); }
        public virtual bool DoorsCargoKeepOpenOnDetachBoard { get => Source.DoorsCargoKeepOpenOnDetachBoard; set => SetModelValue<bool>(value); }
        public virtual bool DoorsCargoKeepOpenOnDetachDeboard { get => Source.DoorsCargoKeepOpenOnDetachDeboard; set => SetModelValue<bool>(value); }
        public virtual bool DoorPanelHandling { get => Source.DoorPanelHandling; set => SetModelValue<bool>(value); }


        public virtual bool CallJetwayStairsOnPrep { get => Source.CallJetwayStairsOnPrep; set => SetModelValue<bool>(value); }
        public virtual bool AttemptConnectStairRefuel { get => Source.AttemptConnectStairRefuel; set => SetModelValue<bool>(value); }
        public virtual int DelayCallRefuelAfterStair { get => Source.DelayCallRefuelAfterStair; set => SetModelValue<int>(value); }
        public virtual bool CallJetwayStairsDuringDeparture { get => Source.CallJetwayStairsDuringDeparture; set => SetModelValue<bool>(value); }
        public virtual bool CallJetwayStairsOnArrival { get => Source.CallJetwayStairsOnArrival; set => SetModelValue<bool>(value); }
        public virtual int RemoveStairsAfterDepature { get => Source.RemoveStairsAfterDepature; set => SetModelValue<int>(value); }
        public virtual bool RemoveJetwayStairsOnFinal { get => Source.RemoveJetwayStairsOnFinal; set => SetModelValue<bool>(value); }

        //Ground Equipment
        public virtual bool ClearGroundEquipOnBeacon { get => Source.ClearGroundEquipOnBeacon; set => SetModelValue<bool>(value); }
        public virtual bool ClearChocksOnTugAttach { get => Source.ClearChocksOnTugAttach; set => SetModelValue<bool>(value); }
        public virtual bool GradualGroundEquipRemoval { get => Source.GradualGroundEquipRemoval; set => SetModelValue<bool>(value); }
        public virtual bool ConnectGpuWithApuRunning { get => Source.ConnectGpuWithApuRunning; set => SetModelValue<bool>(value); }
        public virtual int ConnectPca { get => Source.ConnectPca; set => SetModelValue<int>(value); }
        public virtual bool PcaOverride { get => Source.PcaOverride; set => SetModelValue<bool>(value); }
        public virtual bool CallJetwayStairsInWalkaround { get => Source.CallJetwayStairsInWalkaround; set => SetModelValue<bool>(value); }
        public virtual int ChockDelayMin
        {
            get => Source.ChockDelayMin;
            set
            {
                if (value < ChockDelayMax)
                    SetModelValue<int>(value);
                else
                    OnPropertyChanged(nameof(ChockDelayMin));
            }
        }
        public virtual int ChockDelayMax
        {
            get => Source.ChockDelayMax;
            set
            {
                if (value > ChockDelayMin)
                    SetModelValue<int>(value);
                else
                    OnPropertyChanged(nameof(ChockDelayMax));
            }
        }

        //OFP Import
        public virtual bool FuelRoundUp100 { get => Source.FuelRoundUp100; set => SetModelValue<bool>(value); }
        public virtual bool RandomizePax { get => Source.RandomizePax; set => SetModelValue<bool>(value); }
        public virtual int RandomizePaxMaxDiff { get => Source.RandomizePaxMaxDiff; set => SetModelValue<int>(value); }
        public virtual bool ApplyPaxToCargo { get => Source.ApplyPaxToCargo; set => SetModelValue<bool>(value); }
        public virtual int DelayTurnAroundSeconds { get => Source.DelayTurnAroundSeconds; set => SetModelValue<int>(value); }
        public virtual int DelayTurnRecheckSeconds { get => Source.DelayTurnRecheckSeconds; set => SetModelValue<int>(value); }
        public virtual bool RefreshGsxOnDeparture { get => Source.RefreshGsxOnDeparture; set => SetModelValue<bool>(value); }
        public virtual bool RefreshGsxOnTurn { get => Source.RefreshGsxOnTurn; set => SetModelValue<bool>(value); }
        public virtual bool UseSimTime { get => Source.UseSimTime; set => SetModelValue<bool>(value); }


        //Fuel & Payload
        public virtual bool RefuelFinishOnHose { get => Source.RefuelFinishOnHose; set => SetModelValue<bool>(value); }
        public virtual double FuelResetBaseKg { get => Config.ConvertKgToDisplayUnit(Source.FuelResetBaseKg); set => SetModelValue<double>(Config.ConvertFromDisplayUnitKg(value)); }
        public virtual double RefuelRateKgSec { get => Config.ConvertKgToDisplayUnit(Source.RefuelRateKgSec); set => SetModelValue<double>(Config.ConvertFromDisplayUnitKg(value)); }
        public virtual bool UseFixedRefuelRate => !UseRefuelTimeTarget;
        public virtual bool UseRefuelTimeTarget { get => Source.UseRefuelTimeTarget; set { SetModelValue<bool>(value); NotifyPropertyChanged(nameof(UseFixedRefuelRate)); } }
        public virtual int RefuelTimeTargetSeconds { get => Source.RefuelTimeTargetSeconds; set => SetModelValue<int>(value); }
        public virtual bool FuelSaveLoadFob { get => Source.FuelSaveLoadFob; set => SetModelValue<bool>(value); }
        public virtual int DefaultPilotTarget { get => Source.DefaultPilotTarget; set => SetModelValue<int>(value); }
        public virtual int DefaultCrewTarget { get => Source.DefaultCrewTarget; set => SetModelValue<int>(value); }
        public virtual bool CanSetPilotTarget => (AnswerCrewBoardQuestion != 1 && !SkipCrewBoardQuestion) || (AnswerCrewDeboardQuestion != 1 && !SkipCrewDeboardQuestion);
        public virtual bool ResetPayloadOnPrep { get => Source.ResetPayloadOnPrep; set => SetModelValue<bool>(value); }
        public virtual bool ResetPayloadOnTurn { get => Source.ResetPayloadOnTurn; set => SetModelValue<bool>(value); }

        //GSX Services
        public virtual bool CallReposition { get => Source.CallReposition; set => SetModelValue<bool>(value); }
        public virtual bool SkipFuelOnTankering { get => Source.SkipFuelOnTankering; set => SetModelValue<bool>(value); }
        public virtual bool CallDeboardOnArrival { get => Source.CallDeboardOnArrival; set => SetModelValue<bool>(value); }
        public virtual bool RunDepartureOnArrival { get => Source.RunDepartureOnArrival; set => SetModelValue<bool>(value); }
        public virtual GsxCancelService SmartButtonAbortService { get => Source.SmartButtonAbortService; set => SetModelValue<GsxCancelService>(value); }
        public virtual Dictionary<GsxCancelService, string> AbortOptions { get; } = new()
        {
            { GsxCancelService.Never, "Never" },
            { GsxCancelService.Complete, "Gracefully" },
            { GsxCancelService.Abort, "Forcefully" },
        };
        public virtual ModelDepartureServices DepartureServices { get; }
        public virtual Dictionary<GsxServiceActivation, string> TextServiceActivations => ServiceConfig.TextServiceActivations;
        public virtual Dictionary<GsxServiceConstraint, string> TextServiceConstraints => ServiceConfig.TextServiceConstraints;

        public virtual int CallPushbackWhenTugAttached { get => Source.CallPushbackWhenTugAttached; set => SetModelValue<int>(value); }
        public virtual bool CallPushbackOnBeacon { get => Source.CallPushbackOnBeacon; set => SetModelValue<bool>(value); }
        public virtual bool CancelServicesOnPushPhase { get => Source.CancelServicesOnPushPhase; set => SetModelValue<bool>(value); }

        //Operator Selection
        public virtual bool OperatorAutoSelect { get => Source.OperatorAutoSelect; set => SetModelValue<bool>(value); }
        public virtual bool OperatorPreferencEnabled => !Source.OperatorAutoSelect;
        public virtual bool OperatorPreferenceSelect { get => Source.OperatorPreferenceSelect; set => SetModelValue<bool>(value); }
        public virtual ModelOperatorPreferences OperatorPreferences { get; }

        //Company Hubs
        public virtual ModelCompanyHubs CompanyHubs { get; }

        //LS & Notifications
        public virtual int FinalDelayMin
        {
            get => Source.FinalDelayMin;
            set
            {
                if (value < FinalDelayMax)
                    SetModelValue<int>(value);
                else
                    OnPropertyChanged(nameof(FinalDelayMin));
            }
        }
        public virtual int FinalDelayMax
        {
            get => Source.FinalDelayMax;
            set
            {
                if (value > FinalDelayMin)
                    SetModelValue<int>(value);
                else
                    OnPropertyChanged(nameof(FinalDelayMax));
            }
        }
        public virtual bool NotifyPrepFinished { get => Source.NotifyPrepFinished; set => SetModelValue<bool>(value); }
        public virtual bool NotifyFinalReceived { get => Source.NotifyFinalReceived; set => SetModelValue<bool>(value); }
        public virtual bool NotifyChocksPlaced { get => Source.NotifyChocksPlaced; set => SetModelValue<bool>(value); }
        public virtual bool NotifyTurnReady { get => Source.NotifyTurnReady; set => SetModelValue<bool>(value); }

        //Skip Questions
        public virtual bool SkipWalkAround { get => Source.SkipWalkAround; set => SetModelValue<bool>(value); }
        public virtual bool NotSkipCrewBoardQuestion => !SkipCrewBoardQuestion;
        public virtual bool SkipCrewBoardQuestion
        {
            get => Source.SkipCrewBoardQuestion;
            set
            {
                SetModelValue<bool>(value);
                if (value)
                    AnswerCrewBoardQuestion = 1;
            }
        }
        public virtual bool NotSkipCrewDeboardQuestion => !SkipCrewDeboardQuestion;
        public virtual bool SkipCrewDeboardQuestion
        {
            get => Source.SkipCrewDeboardQuestion;
            set
            {
                SetModelValue<bool>(value);
                if (value)
                    AnswerCrewDeboardQuestion = 1;
            }
        }
        public virtual Dictionary<int, string> CrewOptions { get; } = new()
        {
            { 0, "Not Answer" },
            { 1, "No(body)" },
            { 2, "Crew" },
            { 3, "Pilots" },
            { 4, "Both" },
        };
        public virtual int AnswerCrewBoardQuestion { get => Source.AnswerCrewBoardQuestion; set => SetModelValue<int>(value); }
        public virtual int AnswerCrewDeboardQuestion { get => Source.AnswerCrewDeboardQuestion; set => SetModelValue<int>(value); }
        public virtual Dictionary<int, string> TugOptions { get; } = new()
        {
            { 0, "Not Answer" },
            { 1, "No" },
            { 2, "Yes" },
        };
        public virtual int AttachTugDuringBoarding { get => Source.AttachTugDuringBoarding; set => SetModelValue<int>(value); }
        public virtual bool SkipFollowMe { get => Source.SkipFollowMe; set => SetModelValue<bool>(value); }
        public virtual bool KeepDirectionMenuOpen { get => Source.KeepDirectionMenuOpen; set => SetModelValue<bool>(value); }
        public virtual bool AnswerDeiceOnReopen { get => Source.AnswerDeiceOnReopen; set => SetModelValue<bool>(value); }
        public virtual bool EnableMenuForSelection { get => Source.EnableMenuForSelection; set => SetModelValue<bool>(value); }
        public virtual Dictionary<GsxChangePark, string> ClearGateOptions { get; } = NotificationManager.ClearGateOptions;
        public virtual GsxChangePark ClearGateMenuOption { get => Source.ClearGateMenuOption; set => SetModelValue<GsxChangePark>(value); }
        public virtual Dictionary<GsxStopPush, string> StopPushOptions { get; } = NotificationManager.StopPushOptions;
        public virtual GsxStopPush StopPushMenuOption { get => Source.StopPushMenuOption; set => SetModelValue<GsxStopPush>(value); }
    }
}
