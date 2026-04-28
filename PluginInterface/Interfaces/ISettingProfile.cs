using System.Collections.Generic;

namespace Any2GSX.PluginInterface.Interfaces
{
    public interface ISettingProfile
    {
        public string Name { get; }
        public string PluginId { get; }

        //PilotsDeck
        public bool PilotsDeckIntegration { get; }

        //OFP
        public bool FuelRoundUp100 { get; }
        public bool RandomizePax { get; }
        public int RandomizePaxMaxDiff { get; }
        public bool ApplyPaxToCargo { get; }
        public int DelayTurnAroundSeconds { get; }
        public int DelayTurnRecheckSeconds { get; }
        public bool RefreshGsxOnDeparture { get; }
        public bool RefreshGsxOnTurn { get; }
        public bool UseSimTime { get; }
        public bool NotifyPrepFinished { get; }
        public bool NotifyFinalReceived { get; }
        public bool NotifyChocksPlaced { get; }
        public bool NotifyTurnReady { get; }

        //Plugin
        public Dictionary<string, object> PluginSettings { get; }

        //GSX Automation
        public bool SkipWalkAround { get; }
        public bool RunAutomationService { get; }
        public int ConnectPca { get; }
        public bool CallReposition { get; }
        public bool PcaOverride { get; }
        public bool CallJetwayStairsInWalkaround { get; }
        public bool CallJetwayStairsOnPrep { get; }
        public bool CallJetwayStairsDuringDeparture { get; }
        public bool CallJetwayStairsOnArrival { get; }
        public int RemoveStairsAfterDepature { get; }
        public bool AttemptConnectStairRefuel { get; }
        public bool SkipFuelOnTankering { get; }
        public bool CallPushbackOnBeacon { get; }
        public int CallPushbackWhenTugAttached { get; }
        public bool ClearGroundEquipOnBeacon { get; }
        public bool ClearChocksOnTugAttach { get; }
        public bool CallDeboardOnArrival { get; }
        public bool RunDepartureOnArrival { get; }
        public GsxCancelService SmartButtonAbortService { get; }
        public int ChockDelayMin { get; }
        public int ChockDelayMax { get; }
        public int FinalDelayMin { get; }
        public int FinalDelayMax { get; }
        public bool CloseDoorsOnFinal { get; }
        public bool DoorPaxHandling { get; }
        public bool DoorStairHandling { get; }
        public bool DoorServiceHandling { get; }
        public bool DoorPanelHandling { get; }
        public bool DoorCargoHandling { get; }
        public bool DoorCargoOpenOnActive { get; }
        public int DoorCargoOpenCloseDelay { get; }
        public bool DoorsCargoKeepOpenOnDetachBoard { get; }
        public bool DoorsCargoKeepOpenOnDetachDeboard { get; }
        public bool RemoveJetwayStairsOnFinal { get; }

        public bool FuelSaveLoadFob { get; }
        public double FuelResetBaseKg { get; }
        public double RefuelRateKgSec { get; }
        public bool UseRefuelTimeTarget { get; }
        public int RefuelTimeTargetSeconds { get; }
        public bool ResetPayloadOnPrep { get; }
        public bool ResetPayloadOnTurn { get; }

        public int AttachTugDuringBoarding { get; }
        public bool OperatorAutoSelect { get; }
        public bool OperatorPreferenceSelect { get; }
        public List<string> OperatorPreferences { get; }
        public List<string> CompanyHubs { get; }
        public bool KeepDirectionMenuOpen { get; }
        public bool AnswerDeiceOnReopen { get; }
        public bool SkipFollowMe { get; }
        public bool SkipCrewBoardQuestion { get; }
        public bool SkipCrewDeboardQuestion { get; }
        public int DefaultPilotTarget { get; }
        public int AnswerCrewBoardQuestion { get; }
        public int AnswerCrewDeboardQuestion { get; }
        public GsxChangePark ClearGateMenuOption { get; }
        public GsxStopPush StopPushMenuOption { get; }
        public bool EnableMenuForSelection { get; }

        public bool RunAudioService { get; }
        public string ChannelFileId { get; }
        public Dictionary<string, double> AudioStartupVolumes { get; }
        public Dictionary<string, bool> AudioStartupUnmute { get; }

        public bool IsCompanyHub(string icao);
        public bool HasSetting(string key);
        public bool HasSetting<T>(string key, out T value);
        public bool HasPluginSetting<T>(string key, out T value);
        public bool HasGenericSetting<T>(string key, out T value);
        public T GetSetting<T>(string key);
        public dynamic GetSetting(PluginSetting pluginSetting);
        public T GetPluginSetting<T>(string key);
        public T GetGenericSetting<T>(string key);
        public bool SetSetting(string key, object value);
        public bool SetPluginSetting(string key, object value);
        public bool SetGenericSetting(string key, object value);
    }
}
