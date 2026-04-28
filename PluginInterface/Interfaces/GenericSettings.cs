using CFIT.SimConnectLib.SimVars;
using System;
using System.Collections.Generic;

namespace Any2GSX.PluginInterface.Interfaces
{
    public static class GenericSettings
    {
        public const string VarEngine1Name = "Generic.Var.Engine1.Name";
        public const string VarEngine1Unit = "Generic.Var.Engine1.Unit";
        public const string VarEngine2Name = "Generic.Var.Engine2.Name";
        public const string VarEngine2Unit = "Generic.Var.Engine2.Unit";

        public const string VarPowerAvionicName = "Generic.Var.PowerAvionic.Name";
        public const string VarPowerAvionicUnit = "Generic.Var.PowerAvionic.Unit";
        public const string VarPowerExtAvailName = "Generic.Var.ExtAvail.Name";
        public const string VarPowerExtAvailUnit = "Generic.Var.ExtAvail.Unit";
        public const string VarPowerExtConnName = "Generic.Var.ExtConn.Name";
        public const string VarPowerExtConnUnit = "Generic.Var.ExtConn.Unit";

        public const string VarApuRunningName = "Generic.Var.ApuRunning.Name";
        public const string VarApuRunningUnit = "Generic.Var.ApuRunning.Unit";
        public const string VarApuBleedOnName = "Generic.Var.ApuBleedOn.Name";
        public const string VarApuBleedOnUnit = "Generic.Var.ApuBleedOn.Unit";

        public const string VarLightNavName = "Generic.Var.LightNav.Name";
        public const string VarLightNavUnit = "Generic.Var.LightNav.Unit";
        public const string VarLightBeaconName = "Generic.Var.LightBeacon.Name";
        public const string VarLightBeaconUnit = "Generic.Var.LightBeacon.Unit";

        public const string VarParkBrakeName = "Generic.Var.ParkBrake.Name";
        public const string VarParkBrakeUnit = "Generic.Var.ParkBrake.Unit";

        public const string VarSmartButtonName = "Generic.Var.SmartButton.Name";
        public const string VarSmartButtonDefault = "L:ANY2GSX_SMARTBUTTON_REQ";
        public const string VarSmartButtonUnit = "Generic.Var.SmartButton.Unit";
        public const string VarSmartButtonComp = "Generic.Var.SmartButton.Comp";
        public const string VarSmartButtonValue = "Generic.Var.SmartButton.Value";
        public const string VarSmartButtonReset = "Generic.Var.SmartButton.Reset";

        public const string VarDepartTriggerName = "Generic.Var.DepartTrigger.Name";
        public const string VarDepartTriggerUnit = "Generic.Var.DepartTrigger.Unit";
        public const string VarDepartTriggerComp = "Generic.Var.DepartTrigger.Comp";
        public const string VarDepartTriggerValue = "Generic.Var.DepartTrigger.Value";

        public const string OptionAircraftIsCargo = "Generic.Option.Aircraft.IsCargo";
        public const string OptionAircraftRefuelStair = "Generic.Option.Aircraft.RefuelStair";
        public const string OptionAircraftFuelDialog = "Generic.Option.Aircraft.FuelDialog";
        public const string OptionAircraftInitDelay = "Generic.Option.Aircraft.InitDelay";
        public const string OptionAircraftGsxGpu = "Generic.Option.Aircraft.GsxGpu";

        public static List<PluginSetting> GetGenericSettings()
        {
            List<PluginSetting> list = [];

            //Smart Button
            var setting = new PluginSetting()
            {
                Key = VarSmartButtonName,
                Type = PluginSettingType.String,
                DefaultValue = VarSmartButtonDefault,
                Description = "SmartButton: Simulation Variable",
                Tooltip = "The Variable to be monitored and compared to trigger SmartButton Requests.\nThe default L-Var will still work if a different Variable is used here (both will trigger a SmartButton Request)."
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarSmartButtonUnit,
                Type = PluginSettingType.String,
                DefaultValue = SimUnitType.Number,
                Description = "SmartButton: Variable Unit",
                Tooltip = "Simulation Variable Unit as known to SimConnect. When in doubt: use 'number'."
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarSmartButtonComp,
                Type = PluginSettingType.Enum,
                DefaultValue = (int)Comparison.NOT_EQUAL,
                Description = "SmartButton: Comparison",
                EnumValues = [],
                Tooltip = "Comparison used to compare the current Value against the configured Value.\nMust evaluate to true for a SmartButton Request to be triggered."
            };
            foreach (var value in Enum.GetValues<Comparison>())
                setting.EnumValues.Add((int)value, value.ToString());
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarSmartButtonValue,
                Type = PluginSettingType.Number,
                DefaultValue = 0.0,
                Description = "SmartButton: Value",
                Tooltip = "The Value to be compared against the Variable's current Value."
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarSmartButtonReset,
                Type = PluginSettingType.String,
                DefaultValue = "",
                Description = "SmartButton: Reset Code",
                Tooltip = "The RPN Code used to reset the Switch/Variable after the Request was handled.\nLeave blank if not required (i.e. the Switch/Variable resets itself after clicked)."
            };
            list.Add(setting);

            //Depart Trigger
            setting = new PluginSetting()
            {
                Key = VarDepartTriggerName,
                Type = PluginSettingType.String,
                DefaultValue = "",
                Description = "Departure Trigger: Simulation Variable",
                Tooltip = "The Variable to be monitored and compared to trigger the Departure Services.\nThe default Conditions (Avionics powered, External connected, Nav Lights on) and this Trigger (if configured) need all to be true.\nCan be used to Trigger the Departure only after Flightplan Import in the Aircraft - IF that can be checked through a Sim- or L-Variable."
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarDepartTriggerUnit,
                Type = PluginSettingType.String,
                DefaultValue = SimUnitType.Number,
                Description = "Departure Trigger: Variable Unit",
                Tooltip = "Simulation Variable Unit as known to SimConnect. When in doubt: use 'number'."
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarDepartTriggerComp,
                Type = PluginSettingType.Enum,
                DefaultValue = (int)Comparison.NOT_EQUAL,
                Description = "Departure Trigger: Comparison",
                EnumValues = [],
                Tooltip = "Comparison used to compare the current Value against the configured Value.\nMust evaluate to true for the Trigger to be recognized."
            };
            foreach (var value in Enum.GetValues<Comparison>())
                setting.EnumValues.Add((int)value, value.ToString());
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarDepartTriggerValue,
                Type = PluginSettingType.Number,
                Description = "Departure Trigger: Value",
                Tooltip = "The Value to be compared against the Variable's current Value."
            };
            list.Add(setting);

            //IsCargo
            setting = new PluginSetting()
            {
                Key = OptionAircraftIsCargo,
                Type = PluginSettingType.Bool,
                DefaultValue = false,
                Description = "Aircraft: Is Cargo",
                Tooltip = "Report the Aircraft as Cargo Aircraft to the App (i.e. to decide which Services are called)."
            };
            list.Add(setting);

            //RefuelStair
            setting = new PluginSetting()
            {
                Key = OptionAircraftRefuelStair,
                Type = PluginSettingType.Bool,
                DefaultValue = false,
                Description = "Aircraft: Refuel on Left/Port Side",
                Tooltip = "Report the Refuel Side for the Aircraft on the Left Side to the App.\nSome Services (i.e. Stairs) need different Handling when Refuel is on the same Side."
            };
            list.Add(setting);

            //FuelDialog
            setting = new PluginSetting()
            {
                Key = OptionAircraftFuelDialog,
                Type = PluginSettingType.Bool,
                DefaultValue = false,
                Description = "Aircraft: Uses Refuel Dialog",
                Tooltip = "When enabled, the App will automatically wait for and the Refuel Level Dialog and automatically select the Simbrief Fuel.\nThis Option is needed for (GSX) Aircraft Profiles with the Option 'Default Fuel System' checked (i.e. some iniBuilds Aircrafts)"
            };
            list.Add(setting);

            //InitDelay
            setting = new PluginSetting()
            {
                Key = OptionAircraftInitDelay,
                Type = PluginSettingType.Integer,
                DefaultValue = 1000,
                Description = "Aircraft: Initialization Delay",
                DescUnit = "ms",
                Tooltip = "Delay to wait after the Aircraft Variables are initialized before they are used to check the Aircraft State."
            };
            list.Add(setting);

            //GsxGpu
            setting = new PluginSetting()
            {
                Key = OptionAircraftGsxGpu,
                Type = PluginSettingType.Enum,
                DefaultValue = (int)GsxGpuUsage.Never,
                Description = "GSX GPU: Call for Aircraft",
                EnumValues = [],
                Tooltip = "Call/Remove the GSX GPU as Part of the App's Ground Equipment Flow.\n(Call on Session Start, remove in Pushback Phase or call on Arrival)"
            };
            foreach (var value in Enum.GetValues<GsxGpuUsage>())
                setting.EnumValues.Add((int)value, value.ToString());
            list.Add(setting);

            //Engine Combustion Variables
            setting = new PluginSetting()
            {
                Key = VarEngine1Name,
                Type = PluginSettingType.String,
                DefaultValue = "ENG COMBUSTION:1",
                Description = "Engine 1: Simulation Variable",
                Tooltip = "The Variable used to check if the Engines are running.\nUsed to check if the Aircraft is ready for Arrival Services or Runway Starts.\nA non-Zero Value means the Engine is running."
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarEngine1Unit,
                Type = PluginSettingType.String,
                DefaultValue = SimUnitType.Bool,
                Description = "Engine 1: Variable Unit",
                Tooltip = "Simulation Variable Unit as known to SimConnect. When in doubt: use 'number'."
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarEngine2Name,
                Type = PluginSettingType.String,
                DefaultValue = "ENG COMBUSTION:2",
                Description = "Engine 2: Simulation Variable",
                Tooltip = "The Variable used to check if the Engines are running.\nUsed to check if the Aircraft is ready for Arrival Services or Runway Starts.\nA non-Zero Value means the Engine is running."
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarEngine2Unit,
                Type = PluginSettingType.String,
                DefaultValue = SimUnitType.Bool,
                Description = "Engine 2: Variable Unit",
                Tooltip = "Simulation Variable Unit as known to SimConnect. When in doubt: use 'number'."
            };
            list.Add(setting);

            //Power Variables
            setting = new PluginSetting()
            {
                Key = VarPowerAvionicName,
                Type = PluginSettingType.String,
                DefaultValue = "ELECTRICAL MAIN BUS VOLTAGE",
                Description = "Avionics powered: Simulation Variable",
                Tooltip = "The Variable used to check if the Aircraft is powered (not cold and dark).\nUsed to check if the Aircraft is ready for Departure Services and for Audio Control to become active.\nA non-Zero Value means the Avionics are powered."
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarPowerAvionicUnit,
                Type = PluginSettingType.String,
                DefaultValue = SimUnitType.Volts,
                Description = "Avionics powered: Variable Unit",
                Tooltip = "Simulation Variable Unit as known to SimConnect. When in doubt: use 'number'."
            };
            list.Add(setting);

            setting = new PluginSetting()
            {
                Key = VarPowerExtAvailName,
                Type = PluginSettingType.String,
                DefaultValue = "EXTERNAL POWER AVAILABLE",
                Description = "External Power available: Simulation Variable",
                Tooltip = "The Variable used to check if the Aircraft's external Power Source (i.e. GPU) is available.\nA non-Zero Value means that external Power is available."
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarPowerExtAvailUnit,
                Type = PluginSettingType.String,
                DefaultValue = SimUnitType.Bool,
                Description = "External Power available: Variable Unit",
                Tooltip = "Simulation Variable Unit as known to SimConnect. When in doubt: use 'number'."
            };
            list.Add(setting);

            setting = new PluginSetting()
            {
                Key = VarPowerExtConnName,
                Type = PluginSettingType.String,
                DefaultValue = "EXTERNAL POWER ON",
                Description = "External Power connected: Simulation Variable",
                Tooltip = "The Variable used to check if the Aircraft's external Power Source is connected.\nUsed to check if the Aircraft is ready for Departure Services or Pushback.\nA non-Zero Value means that external Power is connected."
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarPowerExtConnUnit,
                Type = PluginSettingType.String,
                DefaultValue = SimUnitType.Bool,
                Description = "External Power connected: Variable Unit",
                Tooltip = "Simulation Variable Unit as known to SimConnect. When in doubt: use 'number'."
            };
            list.Add(setting);

            //APU Variables
            setting = new PluginSetting()
            {
                Key = VarApuRunningName,
                Type = PluginSettingType.String,
                DefaultValue = "APU GENERATOR ACTIVE:1",
                Description = "APU Running: Simulation Variable",
                Tooltip = "The Variable used to check if the Aircraft's APU is generating Power.\nAlternative to Avionics powered and additional Condition to APU Bleed.\nA non-Zero Value means that the APU is running and providing Power."
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarApuRunningUnit,
                Type = PluginSettingType.String,
                DefaultValue = SimUnitType.Bool,
                Description = "APU Running: Variable Unit",
                Tooltip = "Simulation Variable Unit as known to SimConnect. When in doubt: use 'number'."
            };
            list.Add(setting);

            setting = new PluginSetting()
            {
                Key = VarApuBleedOnName,
                Type = PluginSettingType.String,
                DefaultValue = "BLEED AIR APU",
                Description = "APU Bleed: Simulation Variable",
                Tooltip = "The Variable used to check if the Aircraft's APU is providing AC.\nUsed to check if PCA can be disconnected.\nA non-Zero Value means that the APU is providing Bleed Air/AC."
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarApuBleedOnUnit,
                Type = PluginSettingType.String,
                DefaultValue = SimUnitType.Bool,
                Description = "APU Bleed: Variable Unit",
                Tooltip = "Simulation Variable Unit as known to SimConnect. When in doubt: use 'number'."
            };
            list.Add(setting);

            //Light Variables
            setting = new PluginSetting()
            {
                Key = VarLightNavName,
                Type = PluginSettingType.String,
                DefaultValue = "LIGHT NAV ON",
                Description = "Nav Lights: Simulation Variable",
                Tooltip = "The Variable used to check if the Aircraft's Nav Lights are on.\nUsed to check if the Aircraft is ready for Departure Services.\nA non-Zero Value means that the Lights are on."
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarLightNavUnit,
                Type = PluginSettingType.String,
                DefaultValue = SimUnitType.Bool,
                Description = "Nav Lights: Variable Unit",
                Tooltip = "Simulation Variable Unit as known to SimConnect. When in doubt: use 'number'."
            };
            list.Add(setting);

            setting = new PluginSetting()
            {
                Key = VarLightBeaconName,
                Type = PluginSettingType.String,
                DefaultValue = "LIGHT BEACON ON",
                Description = "Beacon Lights: Simulation Variable",
                Tooltip = "The Variable used to check if the Aircraft's Beacon Lights are on.\nUsed to check if the Aircraft is ready for Arrival Services or as Pushback Trigger.\nA non-Zero Value means that the Lights are on."
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarLightBeaconUnit,
                Type = PluginSettingType.String,
                DefaultValue = SimUnitType.Bool,
                Description = "Beacon Lights: Variable Unit",
                Tooltip = "Simulation Variable Unit as known to SimConnect. When in doubt: use 'number'."
            };
            list.Add(setting);

            //Brake
            setting = new PluginSetting()
            {
                Key = VarParkBrakeName,
                Type = PluginSettingType.String,
                DefaultValue = "BRAKE PARKING POSITION",
                Description = "Parking Brake: Simulation Variable",
                Tooltip = "The Variable used to check if the Aircraft's Parking Brake is set.\nUsed to check if the Aircraft is ready for Arrival Services or as Pushback Trigger.\nA non-Zero Value means that Brakes are set."
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarParkBrakeUnit,
                Type = PluginSettingType.String,
                DefaultValue = SimUnitType.Bool,
                Description = "Parking Brake: Variable Unit",
                Tooltip = "Simulation Variable Unit as known to SimConnect. When in doubt: use 'number'."
            };
            list.Add(setting);

            return list;
        }

        public static List<string> GetEssentialIds()
        {
            return [VarEngine1Name, VarEngine2Name, VarPowerAvionicName, VarPowerExtConnName, VarLightNavName, VarLightBeaconName, VarParkBrakeName];
        }
    }
}
