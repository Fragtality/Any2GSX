import { UserSetting, UserSettingValue } from '@microsoft/msfs-sdk';

/**
 * Checks if a setting is fulfilled with a member of the appropriate enum, and reset it if it's not
 * @param setting the setting to check
 * @param type the enum, disered type of the setting
 * @internal
 */
export declare function checkUserSetting(setting: UserSetting<UserSettingValue>, type: object): void;
export declare const basicFormatter: (number: number) => string;
