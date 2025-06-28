import { UnitsAltitudeSettingMode, UnitsDistanceSettingMode, UnitsNavAngleSettingMode, UnitsSettingsManager, UnitsSpeedSettingMode, UnitsTemperatureSettingMode, UnitsTimeSettingMode, UnitsVolumeSettingMode, UnitsWeightSettingMode, } from './UnitsSettingsManager';
/**
 * Utility class for retrieving display units setting managers.
 * @internal
 */
export class UnitsSettings {
    /**
     * Retrieves a manager for display units settings.
     * @param bus The event bus.
     * @returns a manager for display units settings.
     */
    static getManager(bus) {
        var _a;
        return ((_a = UnitsSettings.INSTANCE) !== null && _a !== void 0 ? _a : (UnitsSettings.INSTANCE = new UnitsSettingsManager(bus, [
            {
                name: 'unitsNavAngle',
                defaultValue: UnitsNavAngleSettingMode.Magnetic,
            },
            {
                name: 'unitsSpeed',
                defaultValue: UnitsSpeedSettingMode.Nautical,
            },
            {
                name: 'unitsDistance',
                defaultValue: UnitsDistanceSettingMode.Nautical,
            },
            {
                name: 'unitsAltitude',
                defaultValue: UnitsAltitudeSettingMode.Feet,
            },
            {
                name: 'unitsWeight',
                defaultValue: UnitsWeightSettingMode.Pounds,
            },
            {
                name: 'unitsVolume',
                defaultValue: UnitsVolumeSettingMode.Gallons,
            },
            {
                name: 'unitsTemperature',
                defaultValue: UnitsTemperatureSettingMode.Celsius,
            },
            {
                name: 'unitsTime',
                defaultValue: UnitsTimeSettingMode.Local12,
            },
        ])));
    }
}
