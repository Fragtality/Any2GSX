using CFIT.AppLogger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Any2GSX.PluginInterface.Interfaces.GSX
{
    public static class GsxConstants
    {
        //Paths
        public static string RegPath { get { return @"HKEY_CURRENT_USER\SOFTWARE\FSDreamTeam"; } }
        public static string RegValue { get { return "root"; } }
        public static string PathDefault { get { return @"C:\Program Files (x86)\Addon Manager"; } }
        public static string PathAircraftProfile { get { return Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Virtuali\Airplanes"); } }
        public static string PathGsxConfigFile { get { return Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Virtuali\CouatlAddons.ini"); } }
        public static string GsxConfigRemotePort { get; } = "remote_server_port";

        //Events
        public static string EventMenu { get; } = "EXTERNAL_SYSTEM_TOGGLE";
        public static string EventCommBus { get; } = "Any2GsxToolbarNotification";

        //Variables
        public static string VarMenuChoice { get; } = "L:FSDT_GSX_MENU_CHOICE";
        public static string SettingMultiTrips { get; } = "multiple_trips";
        public static string SettingMultiTripsTrains { get; } = "multiple_trips_trains";
        public static string SettingIgnoreCrew { get; } = "ignore_crew";
        public static string SettingAutoSingleOperator { get; } = "auto_select_single_operator";
        public static string SettingAutoReposition { get; } = "auto_reposition";
        public static string SettingPushRequest { get; } = "request_pushback";
        public static string SettingPushOnBoarding { get; } = "pushback_on_boarding";
        public static string SettingPushIgnoreIcing { get; } = "ignore_icing";
        public static string SettingPushConfirmEngine { get; } = "good_engine_start";
        public static string SettingMenuTimeout { get; } = "menu_timeout";
        public static string SettingOperatorTimeout { get; } = "waiting_msg_delay";
        public static string SettingProgFuel { get; } = "refuel_always_progressive";
        public static string SettingCustFuel { get; } = "detect_custom_refuel";
        public static string SettingAutoMode { get; } = "assistance_services_auto_mode";
        public static string SettingAutoStairs { get; } = "auto_stairs";
        public static string SettingMessageVerbosity { get; } = "ui_verbosity";
        public static string SettingDisableInvoices { get; } = "disable_invoices";
        public static string SettingAuralCues { get; } = "gsx_aural_cues";
        public static string SettingCashRegisterBeep { get; } = "cash_register_beep";
        public static string SettingPinnedMenuMode { get; } = "pinned_menu_mode";
        public static string SettingRefuelTimeAccel { get; } = "refueling_time_acceleration";
        public static string SettingMenuNumberKey { get; } = "menu_numeric_keys";
        public static string SettingPaxDensityBoard { get; } = "pax_density";
        public static string SettingPaxDensityDeboard { get; } = "pax_density_deboarding";
        public static string SettingLoadScaleBag { get; } = "baggage_load_scale";
        public static string SettingLoadScaleCargo { get; } = "cargo_load_scale";
        public static double ConstantFuelRate { get; } = 45;
        public static double ConstantStairAngle { get; } = 35;
        public static double ConstantStairWorkArea { get; } = 2.5;
        public static string VarSetRemoteServer { get; } = "L:FSDT_GSX_SET_REMOTE_SERVER_ENABLED";
        public static string VarSetAutoDoors { get; } = "L:FSDT_GSX_AUTOMATION_DOORS";
        public static string VarSetAutoFuel { get; } = "L:FSDT_GSX_AUTOMATION_FUEL";
        public static string VarSetAutoPayload { get; } = "L:FSDT_GSX_AUTOMATION_PAYLOAD";
        public static string VarSetAutoEquip { get; } = "L:FSDT_GSX_AUTOMATION_GROUND_EQUIP";
        public static string VarSetDoorMsg { get; } = "L:FSDT_GSX_DISABLE_DOORS_MSG";
        public static string VarServiceJetway { get; } = "L:FSDT_GSX_JETWAY";
        public static string VarServiceJetwayOperation { get; } = "L:FSDT_GSX_OPERATEJETWAYS_STATE";
        public static string VarServiceStairs { get; } = "L:FSDT_GSX_STAIRS";
        public static string VarServiceStairsOperation { get; } = "L:FSDT_GSX_OPERATESTAIRS_STATE";
        public static string VarVehicleStairsFront { get; } = "L:FSDT_GSX_VEHICLE_PASSENGERSTAIRSFRONT_STATE";
        public static string VarVehicleStairsMiddle { get; } = "L:FSDT_GSX_VEHICLE_PASSENGERSTAIRSMIDDLE_STATE";
        public static string VarVehicleStairsRear { get; } = "L:FSDT_GSX_VEHICLE_PASSENGERSTAIRSREAR_STATE";
        public static string VarVehicleCleaning { get; } = "L:FSDT_GSX_VEHICLE_PASSENGERBUSCLEANING_STATE";
        public static string VarServiceReposition { get; } = "L:FSDT_GSX_REPOSITIONING";
        public static string VarServiceRefuel { get; } = "L:FSDT_GSX_REFUELING_STATE";
        public static string VarServiceRefuelHose { get; } = "L:FSDT_GSX_FUELHOSE_CONNECTED";
        public static string VarServiceRefuelUnderground { get; } = "L:FSDT_GSX_UNDERGROUND_REFUELING";
        public static string VarServiceCatering { get; } = "L:FSDT_GSX_CATERING_STATE";
        public static string VarVehicleCateringFront { get; } = "L:FSDT_GSX_VEHICLE_CATERINGVEHICLEFRONT_STATE";
        public static string VarVehicleCateringRear { get; } = "L:FSDT_GSX_VEHICLE_CATERINGVEHICLEREAR_STATE";
        public static string VarServiceBoarding { get; } = "L:FSDT_GSX_BOARDING_STATE";
        public static string VarServiceDeboarding { get; } = "L:FSDT_GSX_DEBOARDING_STATE";
        public static string VarServiceDeparture { get; } = "L:FSDT_GSX_DEPARTURE_STATE";
        public static string VarServiceGpu { get; } = "L:FSDT_GSX_GPU_STATE";
        public static string VarServiceGpuConnect { get; } = "L:FSDT_GSX_GPU_CONNECTED";
        public static string VarPushbackStatus { get; } = "L:FSDT_GSX_PUSHBACK_STATUS";
        public static string VarBypassPin { get; } = "L:FSDT_GSX_BYPASS_PIN";
        public static string VarServiceDeice { get; } = "L:FSDT_GSX_DEICING_STATE";
        public static string VarDeiceAvail { get; } = "L:FSDT_Deice_Available";
        public static string VarServiceLavatory { get; } = "L:FSDT_GSX_LAVATORY_STATE";
        public static string VarServiceWater { get; } = "L:FSDT_GSX_WATER_STATE";
        public static string VarServiceCleaning { get; } = "L:FSDT_GSX_CLEANING_STATE";
        public static string VarPaxTarget { get; } = "L:FSDT_GSX_NUMPASSENGERS";
        public static string VarPaxTotalBoard { get; } = "L:FSDT_GSX_NUMPASSENGERS_BOARDING_TOTAL";
        public static string VarPaxTotalDeboard { get; } = "L:FSDT_GSX_NUMPASSENGERS_DEBOARDING_TOTAL";
        public static string VarCargoPercentBoard { get; } = "L:FSDT_GSX_BOARDING_CARGO_PERCENT";
        public static string VarCargoPercentDeboard { get; } = "L:FSDT_GSX_DEBOARDING_CARGO_PERCENT";
        public static string VarPilotTarget { get; } = "L:FSDT_GSX_NUMPILOTS";
        public static string VarCrewTarget { get; } = "L:FSDT_GSX_NUMCREW";
        public static string VarNoCrewBoard { get; } = "L:FSDT_GSX_CREW_NOT_BOARDING";
        public static string VarNoPilotsBoard { get; } = "L:FSDT_GSX_PILOTS_NOT_BOARDING";
        public static string VarNoCrewDeboard { get; } = "L:FSDT_GSX_CREW_NOT_DEBOARDING";
        public static string VarNoPilotsDeboard { get; } = "L:FSDT_GSX_PILOTS_NOT_DEBOARDING";
        public static string VarDoorToggleExit1 { get; } = "L:FSDT_GSX_AIRCRAFT_EXIT_1_TOGGLE";
        public static string VarDoorToggleExit2 { get; } = "L:FSDT_GSX_AIRCRAFT_EXIT_2_TOGGLE";
        public static string VarDoorToggleExit3 { get; } = "L:FSDT_GSX_AIRCRAFT_EXIT_3_TOGGLE";
        public static string VarDoorToggleExit4 { get; } = "L:FSDT_GSX_AIRCRAFT_EXIT_4_TOGGLE";
        public static string VarDoorToggleService1 { get; } = "L:FSDT_GSX_AIRCRAFT_SERVICE_1_TOGGLE";
        public static string VarDoorToggleService2 { get; } = "L:FSDT_GSX_AIRCRAFT_SERVICE_2_TOGGLE";
        public static string VarDoorToggleCargo1 { get; } = "L:FSDT_GSX_AIRCRAFT_CARGO_1_TOGGLE";
        public static string VarDoorToggleCargo2 { get; } = "L:FSDT_GSX_AIRCRAFT_CARGO_2_TOGGLE";
        public static string VarDoorToggleCargo3 { get; } = "L:FSDT_GSX_AIRCRAFT_CARGO_3_TOGGLE";
        public static string VarCargoLoader1 { get; } = "L:FSDT_GSX_LOADER_EXIT_0";
        public static string VarCargoLoader2 { get; } = "L:FSDT_GSX_LOADER_EXIT_1";
        public static string VarCargoLoader3 { get; } = "L:FSDT_GSX_LOADER_EXIT_2";
        public static string VarCargoTrainEmptyFront { get; } = "L:FSDT_GSX_VEHICLE_BAGGAGETRAINEMPTYFRONT_STATE";
        public static string VarCargoTrainEmptyRear { get; } = "L:FSDT_GSX_VEHICLE_BAGGAGETRAINEMPTYREAR_STATE";
        public static string VarCargoTrainEmptyMain { get; } = "L:FSDT_GSX_VEHICLE_BAGGAGETRAINEMPTYMAIN_STATE";
        public static string VarCargoTrainFullFront { get; } = "L:FSDT_GSX_VEHICLE_BAGGAGETRAINFULLFRONT_STATE";
        public static string VarCargoTrainFullRear { get; } = "L:FSDT_GSX_VEHICLE_BAGGAGETRAINFULLREAR_STATE";
        public static string VarCargoTrainFullMain { get; } = "L:FSDT_GSX_VEHICLE_BAGGAGETRAINFULLMAIN_STATE";


        //Menu
        public static string GsxChoice { get; } = "[GSX choice]";
        public static string MenuGate { get; } = "Activate Services at";
        public static string MenuParkingSelect { get; } = "Select Position at";
        public static string MenuParkingChange { get; } = "Change parking or service";
        public static Regex MenuRegexFacility { get; } = new(@"^Change Facility \[(.+)\]$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex MenuRegexGate { get; } = new(@"\s{0,1}([A-Z]{1,2}[0-9]{1,3}[A-Z]{0,2})\s{0,1}|\s{0,1}([A-Z]{0,2}[0-9]{1,3}[A-Z]{1,2})\s{0,1}|\w+\s{0,1}([0-9]{1,3})\s{0,1}", RegexOptions.Compiled | RegexOptions.RightToLeft);
        public static string MenuRequestDeice { get; } = "Request DeIce";
        public static string MenuLineDeice { get; } = "De-icing service";
        public static string MenuAdditionalServices { get; } = "Additional Services";
        public static string MenuOperatorFuel { get; } = "Select fueltruck operator";
        public static string MenuOperatorHandling { get; } = "Select handling operator";
        public static string MenuOperatorCater { get; } = "Select catering operator";
        public static string MenuTugAttach { get; } = "Attach Pushback Tug";
        public static string MenuPushbackInterrupt { get; } = "Interrupt pushback";
        public static string MenuPushbackDirection { get; } = "Select pushback direction";
        public static string MenuDeiceOnPush { get; } = "Ice warning: do you request the de-icing treatment";
        public static string MenuPushbackConfirm { get; } = "Interrupt pushback";
        public static string MenuLineConfirm { get; } = "Confirm good engine";
        public static string MenuPushbackRequest { get; } = "Do you want to request";
        public static string MenuPushQuickEdit { get; } = "QuickEdit Pushback";
        public static string MenuPushQuickEditMap { get; } = "QuickEdit Pushback on Map";
        public static string MenuPushStraightPush { get; } = "Straight pushback";
        public static string MenuPushStraightPull { get; } = "Straight Pull pushback";
        public static List<string> MenuPushStaticOptions { get; } = [MenuPushQuickEdit, MenuPushQuickEditMap, MenuPushStraightPush, MenuPushStraightPull];
        public static string MenuFollowMe { get; } = "Request FollowMe";
        public static string MenuBoardCrew { get; } = "Do you want to board crew";
        public static string MenuDeboardCrew { get; } = "Do you want to deboard crew";
        public static string MenuCancelService { get; } = "Service ";
        public static string MenuLineCompleteService { get; } = "Complete now";
        public static string MenuLineAbortService { get; } = "Abort service";
        public static string MenuRefuelLevel { get; } = "Select refueling";
        public static string MenuRefuelLevelCustom { get; } = "Custom refueling";
        public static string MenuNoJetway { get; } = "No Jetways here";
        public static string MenuRemoveStairs { get; } = "Remove the stairs?";
        public static readonly Dictionary<GsxStopPush, string[]> MenuLinesPush = new()
        {
            { GsxStopPush.Pause, ["Pause", "Resume"] },
            { GsxStopPush.Stop, ["Stop"] },
            { GsxStopPush.Abort, ["Abort"] },
        };

        public static int GetRemotePort()
        {
            int port = 8744;

            try
            {
                Match match = Regex.Match(File.ReadAllText(PathGsxConfigFile), @$"{GsxConfigRemotePort}\s*=\s*(\d+)");

                if (match.Success)
                {
                    port = int.Parse(match.Groups[1].Value);
                    Logger.Debug($"GSX Remote Port: {port}");
                }
                else
                {
                    Logger.Debug($"GSX Remote Port could not be parsed - using default");
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return port;
        }

        public static GsxDoor MapStairDoor(GsxVehicleLocation stair)
        {
            GsxDoor door = GsxDoor.PaxDoor1;
            if (stair == GsxVehicleLocation.Middle)
                door = GsxDoor.PaxDoor2;
            else if (stair == GsxVehicleLocation.Rear)
                door = GsxDoor.PaxDoor4;

            return door;
        }

        public static GsxDoor MapCateringDoor(GsxVehicleLocation truck)
        {
            GsxDoor door = GsxDoor.ServiceDoor1;
            if (truck > GsxVehicleLocation.Front)
                door = GsxDoor.ServiceDoor2;

            return door;
        }
    }
}
