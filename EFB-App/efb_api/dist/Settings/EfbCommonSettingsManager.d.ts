import { DebounceTimer, DefaultUserSettingManager, UserSettingManagerEntry, UserSettingRecord } from '@microsoft/msfs-sdk';

export declare class EfbCommonSettingsManager<T extends UserSettingRecord> extends DefaultUserSettingManager<T> {
    protected readonly debounceTimer: DebounceTimer;
    protected static readonly debounceTimeout = 5000;
    /** Array of setting keys that should not trigger the game to force the save. */
    protected ignoredForceSaveSettingKeys: Array<keyof T>;
    /** @inheritdoc */
    protected onSettingValueChanged<K extends keyof T>(entry: UserSettingManagerEntry<T[K]>, value: T[K]): void;
}
