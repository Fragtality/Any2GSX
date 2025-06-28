import { NumberFormatter } from '@microsoft/msfs-sdk';
/**
 * Checks if a setting is fulfilled with a member of the appropriate enum, and reset it if it's not
 * @param setting the setting to check
 * @param type the enum, disered type of the setting
 * @internal
 */
export function checkUserSetting(setting, type) {
    if (!Object.values(type).includes(setting.get())) {
        setting.resetToDefault();
    }
}
export const basicFormatter = NumberFormatter.create({
    maxDigits: 0,
    forceDecimalZeroes: false,
    nanString: '-',
});
