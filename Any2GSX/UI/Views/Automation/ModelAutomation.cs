using Any2GSX.AppConfig;
using Any2GSX.GSX;
using Any2GSX.PluginInterface.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Threading;

namespace Any2GSX.UI.Views.Automation
{
    public partial class ModelAutomation : ModelBase<SettingProfile>
    {
        protected virtual DispatcherTimer ProfileUpdateTimer { get; }
        protected virtual DispatcherTimer UnitUpdateTimer { get; }
        public override SettingProfile Source => AppService?.SettingProfile;

        public ModelAutomation(AppService appService) : base(appService?.SettingProfile, appService)
        {
            ProfileUpdateTimer = new()
            {
                Interval = TimeSpan.FromMilliseconds(100),
            };
            ProfileUpdateTimer.Tick += ProfileUpdateTimer_Tick;

            UnitUpdateTimer = new()
            {
                Interval = TimeSpan.FromMilliseconds(100),
            };
            UnitUpdateTimer.Tick += UnitUpdateTimer_Tick;

            DepartureServices = new ModelDepartureServices(this) { AddAllowed = false };
            DepartureServices.CollectionChanged += (_, _) => SaveConfig();

            OperatorPreferences = new ModelOperatorPreferences(this);
            OperatorPreferences.CollectionChanged += (_, _) => SaveConfig();

            CompanyHubs = new ModelCompanyHubs(this);
            CompanyHubs.CollectionChanged += (_, _) => SaveConfig();

            this.PropertyChanged += OnSelfPropertyChanged;
        }

        protected virtual void ProfileUpdateTimer_Tick(object? sender, EventArgs e)
        {
            InhibitConfigSave = true;
            DepartureServices.NotifyCollectionChanged();
            OperatorPreferences.NotifyCollectionChanged();
            CompanyHubs.NotifyCollectionChanged();
            NotifyModelUpdated();
            InhibitConfigSave = false;
            ProfileUpdateTimer.Stop();
        }

        protected virtual void UnitUpdateTimer_Tick(object? sender, EventArgs e)
        {
            InhibitConfigSave = true;
            NotifyPropertyChanged(nameof(RefuelRateKgSec));
            NotifyPropertyChanged(nameof(RefuelResetDeltaKg));
            NotifyPropertyChanged(nameof(FuelResetBaseKg));
            NotifyPropertyChanged(nameof(DisplayUnitCurrentString));
            InhibitConfigSave = false;
            UnitUpdateTimer.Stop();
        }

        protected override void InitializeModel()
        {
            AppService.ProfileChanged += OnProfileChanged;
            Config.PropertyChanged += OnConfigPropertyChanged;
        }

        protected virtual void OnProfileChanged(SettingProfile profile)
        {
            NotifyPropertyChanged(string.Empty);
            ProfileUpdateTimer.Start();
        }

        protected virtual void OnConfigPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if ((e?.PropertyName == nameof(Config.DisplayUnitCurrent) || e?.PropertyName == nameof(Config.DisplayUnitCurrentString))
                && !UnitUpdateTimer.IsEnabled)
                UnitUpdateTimer.Start();
            if (e?.PropertyName == nameof(Config.CurrentProfile))
                NotifyPropertyChanged(nameof(ProfileName));
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
        }

        public virtual string ProfileName => Source?.Name;
        public virtual string DisplayUnitCurrentString => Config.DisplayUnitCurrentString;

        //Gate & Doors
        public virtual bool DoorStairHandling { get => Source.DoorStairHandling; set => SetModelValue<bool>(value); }
        public virtual bool DoorServiceHandling { get => Source.DoorServiceHandling; set => SetModelValue<bool>(value); }
        public virtual bool DoorCargoHandling { get => Source.DoorCargoHandling; set => SetModelValue<bool>(value); }
        public virtual bool CloseDoorsOnFinal { get => Source.CloseDoorsOnFinal; set => SetModelValue<bool>(value); }

        public virtual bool CallJetwayStairsOnPrep { get => Source.CallJetwayStairsOnPrep; set => SetModelValue<bool>(value); }
        public virtual bool AttemptConnectStairRefuel { get => Source.AttemptConnectStairRefuel; set => SetModelValue<bool>(value); }
        public virtual int DelayCallRefuelAfterStair { get => Source.DelayCallRefuelAfterStair; set => SetModelValue<int>(value); }
        public virtual bool CallJetwayStairsDuringDeparture { get => Source.CallJetwayStairsDuringDeparture; set => SetModelValue<bool>(value); }
        public virtual bool CallJetwayStairsOnArrival { get => Source.CallJetwayStairsOnArrival; set => SetModelValue<bool>(value); }
        public virtual int RemoveStairsAfterDepature { get => Source.RemoveStairsAfterDepature; set => SetModelValue<int>(value); }
        public virtual bool RemoveJetwayStairsOnFinal { get => Source.RemoveJetwayStairsOnFinal; set => SetModelValue<bool>(value); }

        //Ground Equipment
        public virtual bool ClearGroundEquipOnBeacon { get => Source.ClearGroundEquipOnBeacon; set => SetModelValue<bool>(value); }
        public virtual bool GradualGroundEquipRemoval { get => Source.GradualGroundEquipRemoval; set => SetModelValue<bool>(value); }
        public virtual int ConnectPca { get => Source.ConnectPca; set => SetModelValue<int>(value); }
        public virtual int ChockDelayMin { get => Source.ChockDelayMin; set => SetModelValue<int>(value); }
        public virtual int ChockDelayMax { get => Source.ChockDelayMax; set => SetModelValue<int>(value); }
        public virtual int FinalDelayMin { get => Source.FinalDelayMin; set => SetModelValue<int>(value); }
        public virtual int FinalDelayMax { get => Source.FinalDelayMax; set => SetModelValue<int>(value); }

        //OFP Import
        public virtual bool FuelRoundUp100 { get => Source.FuelRoundUp100; set => SetModelValue<bool>(value); }
        public virtual bool RandomizePax { get => Source.RandomizePax; set => SetModelValue<bool>(value); }
        public virtual int RandomizePaxMaxDiff { get => Source.RandomizePaxMaxDiff; set => SetModelValue<int>(value); }
        public virtual int DelayTurnAroundSeconds { get => Source.DelayTurnAroundSeconds; set => SetModelValue<int>(value); }
        public virtual int DelayTurnRecheckSeconds { get => Source.DelayTurnRecheckSeconds; set => SetModelValue<int>(value); }


        //Fuel & Payload
        public virtual bool RefuelFinishOnHose { get => Source.RefuelFinishOnHose; set => SetModelValue<bool>(value); }
        public virtual double RefuelResetDeltaKg { get => Config.ConvertKgToDisplayUnit(Source.RefuelResetDeltaKg); set => SetModelValue<double>(Config.ConvertFromDisplayUnitKg(value)); }
        public virtual double FuelResetBaseKg { get => Config.ConvertKgToDisplayUnit(Source.FuelResetBaseKg); set => SetModelValue<double>(Config.ConvertFromDisplayUnitKg(value)); }
        public virtual double RefuelRateKgSec { get => Config.ConvertKgToDisplayUnit(Source.RefuelRateKgSec); set => SetModelValue<double>(Config.ConvertFromDisplayUnitKg(value)); }
        public virtual bool UseFixedRefuelRate => !UseRefuelTimeTarget;
        public virtual bool UseRefuelTimeTarget { get => Source.UseRefuelTimeTarget; set { SetModelValue<bool>(value); NotifyPropertyChanged(nameof(UseFixedRefuelRate)); } }
        public virtual int RefuelTimeTargetSeconds { get => Source.RefuelTimeTargetSeconds; set => SetModelValue<int>(value); }
        public virtual bool FuelSaveLoadFob { get => Source.FuelSaveLoadFob; set => SetModelValue<bool>(value); }        
        public virtual int DefaultPilotTarget { get => Source.DefaultPilotTarget; set => SetModelValue<int>(value); }
        public virtual bool CanSetPilotTarget => (AnswerCrewBoardQuestion != 1 && !SkipCrewBoardQuestion) || (AnswerCrewDeboardQuestion != 1 && !SkipCrewDeboardQuestion);
        public virtual bool ResetPayloadOnPrep { get => Source.ResetPayloadOnPrep; set => SetModelValue<bool>(value); }
        public virtual bool ResetPayloadOnTurn { get => Source.ResetPayloadOnTurn; set => SetModelValue<bool>(value); }

        //GSX Services
        public virtual bool CallReposition { get => Source.CallReposition; set => SetModelValue<bool>(value); }
        public virtual bool SkipFuelOnTankering { get => Source.SkipFuelOnTankering; set => SetModelValue<bool>(value); }
        public virtual bool CallDeboardOnArrival { get => Source.CallDeboardOnArrival; set => SetModelValue<bool>(value); }
        public virtual ModelDepartureServices DepartureServices { get; }
        public virtual Dictionary<GsxServiceActivation, string> TextServiceActivations => ServiceConfig.TextServiceActivations;
        public virtual Dictionary<GsxServiceConstraint, string> TextServiceConstraints => ServiceConfig.TextServiceConstraints;

        public virtual int CallPushbackWhenTugAttached { get => Source.CallPushbackWhenTugAttached; set => SetModelValue<int>(value); }
        public virtual bool CallPushbackOnBeacon { get => Source.CallPushbackOnBeacon; set => SetModelValue<bool>(value); }

        //Operator Selection
        public virtual bool OperatorAutoSelect { get => Source.OperatorAutoSelect; set => SetModelValue<bool>(value); }
        public virtual ModelOperatorPreferences OperatorPreferences { get; }

        //Company Hubs
        public virtual ModelCompanyHubs CompanyHubs { get; }

        //Skip Questions
        public virtual bool SkipWalkAround { get => Source.SkipWalkAround; set => SetModelValue<bool>(value); }
        public virtual bool NotSkipCrewBoardQuestion => !SkipCrewBoardQuestion;
        public virtual bool SkipCrewBoardQuestion { get => Source.SkipCrewBoardQuestion;
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
        public virtual Dictionary<int, string> ClearGateOptions { get; } = new()
        {
            { 1, "Change Facility" },
            { 2, "Request FollowMe" },
            { 3, "Revoke Services" },
            { 4, "Remove AI" },
            { 5, "Warp to Gate" },
            { 6, "Show Gate" },
        };
        public virtual int ClearGateMenuOption { get => Source.ClearGateMenuOption; set => SetModelValue<int>(value); }
    }
}
