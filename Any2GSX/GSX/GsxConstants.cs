using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Any2GSX.GSX
{
    public static class GsxConstants
    {
        //Paths
        public static string RegPath { get { return @"HKEY_CURRENT_USER\SOFTWARE\FSDreamTeam"; } }
        public static string RegValue { get { return "root"; } }
        public static string PathDefault { get { return @"C:\Program Files (x86)\Addon Manager"; } }
        public static string RelativePathMenu { get { return @"\MSFS\fsdreamteam-gsx-pro\html_ui\InGamePanels\FSDT_GSX_Panel\menu"; } }
        public static string PathAircraftProfile { get { return Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Virtuali\Airplanes"); } }

        //Events
        public static string EventMenu { get; } = "EXTERNAL_SYSTEM_TOGGLE";

        //Variables
        public static string VarCouatlStarted { get; } = "L:FSDT_GSX_COUATL_STARTED";
        public static string VarCouatlSimbrief { get; } = "L:FSDT_GSX_SIMBRIEF_SUCCESS";
        public static string VarCouatlStartProg5 { get; } = "L:FSDT_GSX_COUATL_STARTED_5_PROGRESS";
        public static string VarCouatlStartProg6 { get; } = "L:FSDT_GSX_COUATL_STARTED_6_PROGRESS";
        public static string VarCouatlStartProg7 { get; } = "L:FSDT_GSX_COUATL_STARTED_7_PROGRESS";
        public static string VarMenuOpen { get; } = "L:FSDT_GSX_MENU_OPEN";
        public static string VarMenuChoice { get; } = "L:FSDT_GSX_MENU_CHOICE";
        public static string VarReadProgFuel { get; } = "L:FSDT_GSX_SETTINGS_PROGRESS_REFUEL";
        public static string VarSetProgFuel { get; } = "L:FSDT_GSX_SET_PROGRESS_REFUEL";
        public static string VarReadCustFuel { get; } = "L:FSDT_GSX_SETTINGS_DETECT_CUST_REFUEL";
        public static string VarSetCustFuel { get; } = "L:FSDT_GSX_SET_DETECT_CUST_REFUEL";
        public static string VarReadAutoMode { get; } = "L:FSDT_GSX_SETTINGS_AUTOMODE";
        public static string VarSetAutoMode { get; } = "L:FSDT_GSX_SET_AUTOMODE";
        public static string VarServiceJetway { get; } = "L:FSDT_GSX_JETWAY";
        public static string VarServiceJetwayOperation { get; } = "L:FSDT_GSX_OPERATEJETWAYS_STATE";
        public static string VarServiceStairs { get; } = "L:FSDT_GSX_STAIRS";
        public static string VarServiceStairsOperation { get; } = "L:FSDT_GSX_OPERATESTAIRS_STATE";
        public static string VarServiceRefuel { get; } = "L:FSDT_GSX_REFUELING_STATE";
        public static string VarServiceRefuelHose { get; } = "L:FSDT_GSX_FUELHOSE_CONNECTED";
        public static string VarServiceRefuelUnderground { get; } = "L:FSDT_GSX_UNDERGROUND_REFUELING";
        public static string VarServiceCatering { get; } = "L:FSDT_GSX_CATERING_STATE";
        public static string VarServiceBoarding { get; } = "L:FSDT_GSX_BOARDING_STATE";
        public static string VarServiceDeboarding { get; } = "L:FSDT_GSX_DEBOARDING_STATE";
        public static string VarServiceDeparture { get; } = "L:FSDT_GSX_DEPARTURE_STATE";
        public static string VarServiceGpu { get; } = "L:FSDT_GSX_GPU_STATE";
        public static string VarPusbackStatus { get; } = "L:FSDT_GSX_PUSHBACK_STATUS";
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
        public static string VarCargoLoading1 { get; } = "L:FSDT_GSX_BOARDING_CARGO_EXIT_0";
        public static string VarCargoLoading2 { get; } = "L:FSDT_GSX_BOARDING_CARGO_EXIT_1";
        public static string VarCargoLoading3 { get; } = "L:FSDT_GSX_BOARDING_CARGO_EXIT_2";
        public static string VarCargoUnloading1 { get; } = "L:FSDT_GSX_DEBOARDING_CARGO_EXIT_0";
        public static string VarCargoUnloading2 { get; } = "L:FSDT_GSX_DEBOARDING_CARGO_EXIT_1";
        public static string VarCargoUnloading3 { get; } = "L:FSDT_GSX_DEBOARDING_CARGO_EXIT_2";

        //Menu
        public static string GsxChoice { get; } = "[GSX choice]";
        public static string MenuGate { get; } = "Activate Services at";
        public static string MenuParkingSelect { get; } = "Select Position at";
        public static string MenuParkingChange { get; } = "Change parking or service";
        public static Regex MenuRegexFacility { get; } = new(@"^Change Facility \[(.+)\]$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex MenuRegexGate { get; } = new(@"\s{0,1}([A-Z]{1,2}[0-9]{1,3}[A-Z]{0,2})\s{0,1}|\s{0,1}([A-Z]{0,2}[0-9]{1,3}[A-Z]{1,2})\s{0,1}", RegexOptions.Compiled | RegexOptions.RightToLeft);
        public static string MenuRequestDeice { get; } = "Request DeIce";
        public static string MenuAdditionalServices { get; } = "Activate Ground Services";
        public static string MenuOperatorHandling { get; } = "Select handling operator";
        public static string MenuOperatorCater { get; } = "Select catering operator";
        public static string MenuTugAttach { get; } = "Attach Pushback Tug"; 
        public static string MenuPushbackInterrupt { get; } = "Interrupt pushback";
        public static string MenuPushbackDirection { get; } = "Select pushback direction";
        public static string MenuPushbackChange { get; } = "Change Direction";
        public static string MenuDeiceOnPush { get; } = "Ice warning: do you request the de-icing treatment";
        public static string MenuPushbackConfirm { get; } = "Interrupt pushback";
        public static string MenuFollowMe { get; } = "Request FollowMe";
        public static string MenuCrewBoard { get; } = "Do you want to board crew";
        public static string MenuCrewDeboard { get; } = "Do you want to deboard crew";
        public static string MenuCancelService { get; } = "Service in progress";
    }
}
