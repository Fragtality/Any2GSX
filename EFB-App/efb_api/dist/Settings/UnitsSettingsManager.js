import { BasicNavAngleUnit, DefaultUserSettingManager, Subject, UnitType, } from '@microsoft/msfs-sdk';
import { checkUserSetting } from '../utils/SettingsUtils';
/**
 * Setting modes for nav angle units.
 * @internal
 */
export var UnitsNavAngleSettingMode;
(function (UnitsNavAngleSettingMode) {
    UnitsNavAngleSettingMode["Magnetic"] = "magnetic";
    UnitsNavAngleSettingMode["True"] = "true";
})(UnitsNavAngleSettingMode || (UnitsNavAngleSettingMode = {}));
/**
 * Setting modes for speed units.
 * @internal
 */
export var UnitsSpeedSettingMode;
(function (UnitsSpeedSettingMode) {
    UnitsSpeedSettingMode["Nautical"] = "KTS";
    UnitsSpeedSettingMode["Metric"] = "KPH";
})(UnitsSpeedSettingMode || (UnitsSpeedSettingMode = {}));
/**
 * Setting modes for distance units.
 * @internal
 */
export var UnitsDistanceSettingMode;
(function (UnitsDistanceSettingMode) {
    UnitsDistanceSettingMode["Nautical"] = "NM";
    UnitsDistanceSettingMode["Metric"] = "KM";
})(UnitsDistanceSettingMode || (UnitsDistanceSettingMode = {}));
/**
 * Setting modes for altitude units.
 * @internal
 */
export var UnitsAltitudeSettingMode;
(function (UnitsAltitudeSettingMode) {
    UnitsAltitudeSettingMode["Feet"] = "FT";
    UnitsAltitudeSettingMode["Meters"] = "M";
})(UnitsAltitudeSettingMode || (UnitsAltitudeSettingMode = {}));
/**
 * Setting modes for weight units.
 * @internal
 */
export var UnitsWeightSettingMode;
(function (UnitsWeightSettingMode) {
    UnitsWeightSettingMode["Pounds"] = "LBS";
    UnitsWeightSettingMode["Kilograms"] = "KG";
})(UnitsWeightSettingMode || (UnitsWeightSettingMode = {}));
/**
 * Setting modes for weight units.
 * @internal
 */
export var UnitsVolumeSettingMode;
(function (UnitsVolumeSettingMode) {
    UnitsVolumeSettingMode["Gallons"] = "GAL US";
    UnitsVolumeSettingMode["Liters"] = "L";
})(UnitsVolumeSettingMode || (UnitsVolumeSettingMode = {}));
/**
 * Setting modes for temperature units.
 * @internal
 */
export var UnitsTemperatureSettingMode;
(function (UnitsTemperatureSettingMode) {
    UnitsTemperatureSettingMode["Fahrenheit"] = "\u00B0F";
    UnitsTemperatureSettingMode["Celsius"] = "\u00B0C";
})(UnitsTemperatureSettingMode || (UnitsTemperatureSettingMode = {}));
/**
 * Setting modes for time units.
 * @internal
 */
export var UnitsTimeSettingMode;
(function (UnitsTimeSettingMode) {
    UnitsTimeSettingMode["Local12"] = "local-12";
    UnitsTimeSettingMode["Local24"] = "local-24";
})(UnitsTimeSettingMode || (UnitsTimeSettingMode = {}));
export class UnitsSettingsManager extends DefaultUserSettingManager {
    constructor(bus, settingsDefs) {
        super(bus, settingsDefs, true);
        this.navAngleUnitsSub = Subject.create(UnitsSettingsManager.MAGNETIC_BEARING);
        this.navAngleUnits = this.navAngleUnitsSub;
        this.timeUnitsSub = Subject.create(UnitsTimeSettingMode.Local12);
        this._timeUnits = this.timeUnitsSub;
        this.areSubscribablesInit = false;
        this.areSubscribablesInit = true;
        for (const entry of this.settings.values()) {
            this.updateUnitsSubjects(entry.setting.definition.name, entry.setting.value);
        }
    }
    onSettingValueChanged(entry, value) {
        if (this.areSubscribablesInit) {
            this.updateUnitsSubjects(entry.setting.definition.name, value);
        }
        super.onSettingValueChanged(entry, value);
    }
    /**
     * Checks if the values loaded from the datastorage correspond to the settings types.
     */
    checkLoadedValues() {
        checkUserSetting(this.getSetting('unitsNavAngle'), UnitsNavAngleSettingMode);
        checkUserSetting(this.getSetting('unitsSpeed'), UnitsSpeedSettingMode);
        checkUserSetting(this.getSetting('unitsDistance'), UnitsDistanceSettingMode);
        checkUserSetting(this.getSetting('unitsAltitude'), UnitsAltitudeSettingMode);
        checkUserSetting(this.getSetting('unitsWeight'), UnitsWeightSettingMode);
        checkUserSetting(this.getSetting('unitsVolume'), UnitsVolumeSettingMode);
        checkUserSetting(this.getSetting('unitsTemperature'), UnitsTemperatureSettingMode);
        checkUserSetting(this.getSetting('unitsTime'), UnitsTimeSettingMode);
    }
    updateUnitsSubjects(settingName, value) {
        switch (settingName) {
            case 'unitsNavAngle':
                this.navAngleUnitsSub.set(value === UnitsNavAngleSettingMode.True
                    ? UnitsSettingsManager.TRUE_BEARING
                    : UnitsSettingsManager.MAGNETIC_BEARING);
                break;
            case 'unitsTime':
                this.timeUnitsSub.set(value);
                break;
        }
    }
    getSettingUnitType(settingName) {
        return this.getSetting(settingName).map((settingValue) => UnitTypesMap[settingValue]);
    }
}
UnitsSettingsManager.TRUE_BEARING = BasicNavAngleUnit.create(false);
UnitsSettingsManager.MAGNETIC_BEARING = BasicNavAngleUnit.create(true);
const UnitTypesMap = {
    /** Mapped speed unit types */
    KTS: UnitType.KNOT,
    KPH: UnitType.KPH,
    /** Mapped distance unit type */
    NM: UnitType.NMILE,
    KM: UnitType.KILOMETER,
    /** Mapped altitude unit type */
    FT: UnitType.FOOT,
    M: UnitType.METER,
    /** Mapped weight unit type */
    LBS: UnitType.POUND,
    KG: UnitType.KILOGRAM,
    /** Mapped volume unit type */
    'GAL US': UnitType.GALLON,
    L: UnitType.LITER,
    /** Mapped temperature unit type */
    '°C': UnitType.CELSIUS,
    '°F': UnitType.FAHRENHEIT,
};
