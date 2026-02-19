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
                Description = "Simulation Variable indicating a SmartButton Request"
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarSmartButtonUnit,
                Type = PluginSettingType.String,
                DefaultValue = SimUnitType.Number,
                Description = "Simulation Variable Unit for SmartButton"
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarSmartButtonComp,
                Type = PluginSettingType.Enum,
                DefaultValue = (int)Comparison.NOT_EQUAL,
                Description = "Comparison for the SmartButton Variable",
                EnumValues = []
            };
            foreach (var value in Enum.GetValues<Comparison>())
                setting.EnumValues.Add((int)value, value.ToString());
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarSmartButtonValue,
                Type = PluginSettingType.Number,
                DefaultValue = 0.0,
                Description = "Value used in SmartButton Comparison"
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarSmartButtonReset,
                Type = PluginSettingType.String,
                DefaultValue = "",
                Description = "RPN/Calculator Code to execute for Reset (blank if not required)"
            };
            list.Add(setting);

            //Depart Trigger
            setting = new PluginSetting()
            {
                Key = VarDepartTriggerName,
                Type = PluginSettingType.String,
                DefaultValue = "",
                Description = "Variable used as an additional Trigger for Departure Services"
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarDepartTriggerUnit,
                Type = PluginSettingType.String,
                DefaultValue = SimUnitType.Number,
                Description = "Simulation Variable Unit for Departure Trigger Variable"
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarDepartTriggerComp,
                Type = PluginSettingType.Enum,
                DefaultValue = (int)Comparison.NOT_EQUAL,
                Description = "Comparison for the Departure Trigger Variable",
                EnumValues = []
            };
            foreach (var value in Enum.GetValues<Comparison>())
                setting.EnumValues.Add((int)value, value.ToString());
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarDepartTriggerValue,
                Type = PluginSettingType.Number,
                DefaultValue = 0.0,
                Description = "Value used in Departure Trigger Comparison"
            };
            list.Add(setting);

            //IsCargo
            setting = new PluginSetting()
            {
                Key = OptionAircraftIsCargo,
                Type = PluginSettingType.Bool,
                DefaultValue = false,
                Description = "Handle the Aircraft as Cargo Plane"
            };
            list.Add(setting);

            //RefuelStair
            setting = new PluginSetting()
            {
                Key = OptionAircraftRefuelStair,
                Type = PluginSettingType.Bool,
                DefaultValue = false,
                Description = "The Aircraft is refueled on the Left/Stair Side"
            };
            list.Add(setting);

            //InitDelay
            setting = new PluginSetting()
            {
                Key = OptionAircraftInitDelay,
                Type = PluginSettingType.Integer,
                DefaultValue = 1000,
                Description = "Delay in ms to wait for the Aircraft Systems to initialize"
            };
            list.Add(setting);

            //GsxGpu
            setting = new PluginSetting()
            {
                Key = OptionAircraftGsxGpu,
                Type = PluginSettingType.Enum,
                DefaultValue = (int)GsxGpuUsage.Never,
                Description = "Use GSX GPU for Aircraft",
                EnumValues = []
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
                Description = "Simulation Variable indicating Combustion for Engine 1"
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarEngine1Unit,
                Type = PluginSettingType.String,
                DefaultValue = SimUnitType.Bool,
                Description = "Simulation Variable Unit for Engine 1 Combustion"
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarEngine2Name,
                Type = PluginSettingType.String,
                DefaultValue = "ENG COMBUSTION:2",
                Description = "Simulation Variable indicating Combustion for Engine 2"
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarEngine2Unit,
                Type = PluginSettingType.String,
                DefaultValue = SimUnitType.Bool,
                Description = "Simulation Variable Unit for Engine 2 Combustion"
            };
            list.Add(setting);

            //Power Variables
            setting = new PluginSetting()
            {
                Key = VarPowerAvionicName,
                Type = PluginSettingType.String,
                DefaultValue = "ELECTRICAL MAIN BUS VOLTAGE",
                Description = "Simulation Variable indicating Avionics powered"
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarPowerAvionicUnit,
                Type = PluginSettingType.String,
                DefaultValue = SimUnitType.Volts,
                Description = "Simulation Variable Unit for Avionics powered"
            };
            list.Add(setting);

            setting = new PluginSetting()
            {
                Key = VarPowerExtAvailName,
                Type = PluginSettingType.String,
                DefaultValue = "EXTERNAL POWER AVAILABLE",
                Description = "Simulation Variable indicating External Power available"
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarPowerExtAvailUnit,
                Type = PluginSettingType.String,
                DefaultValue = SimUnitType.Bool,
                Description = "Simulation Variable Unit for External Power available"
            };
            list.Add(setting);

            setting = new PluginSetting()
            {
                Key = VarPowerExtConnName,
                Type = PluginSettingType.String,
                DefaultValue = "EXTERNAL POWER ON",
                Description = "Simulation Variable indicating External Power connected"
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarPowerExtConnUnit,
                Type = PluginSettingType.String,
                DefaultValue = SimUnitType.Bool,
                Description = "Simulation Variable Unit for External Power connected"
            };
            list.Add(setting);

            //APU Variables
            setting = new PluginSetting()
            {
                Key = VarApuRunningName,
                Type = PluginSettingType.String,
                DefaultValue = "APU GENERATOR ACTIVE:1",
                Description = "Simulation Variable indicating APU generating Power"
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarApuRunningUnit,
                Type = PluginSettingType.String,
                DefaultValue = SimUnitType.Bool,
                Description = "Simulation Variable Unit for APU generating Power"
            };
            list.Add(setting);

            setting = new PluginSetting()
            {
                Key = VarApuBleedOnName,
                Type = PluginSettingType.String,
                DefaultValue = "BLEED AIR APU",
                Description = "Simulation Variable indicating APU providing AC"
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarApuBleedOnUnit,
                Type = PluginSettingType.String,
                DefaultValue = SimUnitType.Bool,
                Description = "Simulation Variable Unit for APU providing AC"
            };
            list.Add(setting);

            //Light Variables
            setting = new PluginSetting()
            {
                Key = VarLightNavName,
                Type = PluginSettingType.String,
                DefaultValue = "LIGHT NAV ON",
                Description = "Simulation Variable indicating NAV Lights on"
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarLightNavUnit,
                Type = PluginSettingType.String,
                DefaultValue = SimUnitType.Bool,
                Description = "Simulation Variable Unit for NAV Lights on"
            };
            list.Add(setting);

            setting = new PluginSetting()
            {
                Key = VarLightBeaconName,
                Type = PluginSettingType.String,
                DefaultValue = "LIGHT BEACON ON",
                Description = "Simulation Variable indicating Beacon Light on"
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarLightBeaconUnit,
                Type = PluginSettingType.String,
                DefaultValue = SimUnitType.Bool,
                Description = "Simulation Variable Unit for Beacon Light on"
            };
            list.Add(setting);

            //Brake
            setting = new PluginSetting()
            {
                Key = VarParkBrakeName,
                Type = PluginSettingType.String,
                DefaultValue = "BRAKE PARKING POSITION",
                Description = "Simulation Variable indicating Parking Brake set"
            };
            list.Add(setting);
            setting = new PluginSetting()
            {
                Key = VarParkBrakeUnit,
                Type = PluginSettingType.String,
                DefaultValue = SimUnitType.Bool,
                Description = "Simulation Variable Unit for Parking Brake set"
            };
            list.Add(setting);

            return list;
        }
    }
}
