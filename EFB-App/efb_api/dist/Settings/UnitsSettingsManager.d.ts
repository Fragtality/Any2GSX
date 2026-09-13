import { EventBus, NavAngleUnit, Subscribable, Unit, UserSettingDefinition, UserSettingManagerEntry, UserSettingValue } from '@microsoft/msfs-sdk';
import { EfbCommonSettingsManager } from './EfbCommonSettingsManager';

/**
 * Setting modes for nav angle units.
 * @internal
 */
export declare enum UnitsNavAngleSettingMode {
    Magnetic = "magnetic",
    True = "true"
}
/**
 * Setting modes for speed units.
 * @internal
 */
export declare enum UnitsSpeedSettingMode {
    Nautical = "KTS",
    Metric = "KPH"
}
/**
 * Setting modes for distance units.
 * @internal
 */
export declare enum UnitsDistanceSettingMode {
    Nautical = "NM",
    Metric = "KM"
}
/**
 * Setting modes for small distance units.
 * @internal
 */
export declare enum UnitsSmallDistanceSettingMode {
    Feet = "FT",
    Meters = "M"
}
/**
 * Setting modes for altitude units.
 * @internal
 */
export declare enum UnitsAltitudeSettingMode {
    Feet = "FT",
    Meters = "M"
}
/**
 * Setting modes for weight units.
 * @internal
 */
export declare enum UnitsWeightSettingMode {
    Pounds = "LBS",
    Kilograms = "KG"
}
/**
 * Setting modes for weight units.
 * @internal
 */
export declare enum UnitsVolumeSettingMode {
    Gallons = "GAL US",
    Liters = "L"
}
/**
 * Setting modes for temperature units.
 * @internal
 */
export declare enum UnitsTemperatureSettingMode {
    Fahrenheit = "\u00B0F",
    Celsius = "\u00B0C"
}
/**
 * Setting modes for time units.
 * @internal
 */
export declare enum UnitsTimeSettingMode {
    Local12 = "local-12",
    Local24 = "local-24"
}
export type UnitsSettingsTypes = {
    /** The nav angle units setting. */
    unitsNavAngle: UnitsNavAngleSettingMode;
    /** The speed units setting. */
    unitsSpeed: UnitsSpeedSettingMode;
    /** The distance units setting. */
    unitsDistance: UnitsDistanceSettingMode;
    /** The small distance units settings. Linked to unitsDistance */
    unitsSmallDistance: UnitsSmallDistanceSettingMode;
    /** The altitude units setting. */
    unitsAltitude: UnitsAltitudeSettingMode;
    /** The weight units setting. */
    unitsWeight: UnitsWeightSettingMode;
    /** The temperature units setting. */
    unitsVolume: UnitsVolumeSettingMode;
    /** The temperature units setting. */
    unitsTemperature: UnitsTemperatureSettingMode;
    /** The time units setting. */
    unitsTime: UnitsTimeSettingMode;
};
export declare class UnitsSettingsManager extends EfbCommonSettingsManager<UnitsSettingsTypes> {
    private static readonly TRUE_BEARING;
    private static readonly MAGNETIC_BEARING;
    private readonly navAngleUnitsSub;
    readonly navAngleUnits: Subscribable<NavAngleUnit>;
    private readonly timeUnitsSub;
    readonly _timeUnits: Subscribable<UnitsTimeSettingMode>;
    private areSubscribablesInit;
    constructor(bus: EventBus, settingsDefs: Array<UserSettingDefinition<UnitsSettingsTypes[keyof UnitsSettingsTypes]>>);
    protected onSettingValueChanged<K extends keyof UnitsSettingsTypes>(entry: UserSettingManagerEntry<UnitsSettingsTypes[K]>, value: UnitsSettingsTypes[K]): void;
    /**
     * Checks if the values loaded from the datastorage correspond to the settings types.
     */
    checkLoadedValues(): void;
    protected updateUnitsSubjects(settingName: string, value: UserSettingValue): void;
    /**
     * Gets a unit type subscribable from a unit setting name
     * @param settingName the name of the unit setting
     * @returns a Subscribable containing the unit type. If the value in the dataStorage is unvalid, it returns the default unitType
     */
    getSettingUnitType<F extends string = string>(settingName: keyof UnitsSettingsTypes): Subscribable<Unit<F>>;
}
