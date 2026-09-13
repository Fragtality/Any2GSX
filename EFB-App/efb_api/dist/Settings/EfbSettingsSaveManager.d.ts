import { EventBus, UserSetting, UserSettingSaveManager, UserSettingValue } from '@microsoft/msfs-sdk';

/**
 * A manager for EFB user settings that are saved and persistent across flight sessions.
 * @internal
 */
export declare class EfbSettingsSaveManager extends UserSettingSaveManager {
    private readonly bus;
    protected readonly prefix = "efb-core-settings.2025-04-01.";
    protected readonly oldPrefixes: string[];
    constructor(bus: EventBus);
    protected readonly settings: Array<UserSetting<UserSettingValue>>;
    load(key: string): void;
    save(key: string): void;
    startAutoSave(key: string): void;
    stopAutoSave(key: string): void;
    pruneOldPrefixes(): void;
}
