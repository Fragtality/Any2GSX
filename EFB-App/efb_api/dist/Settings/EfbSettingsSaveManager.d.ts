import { EventBus, UserSetting, UserSettingSaveManager, UserSettingValue } from '@microsoft/msfs-sdk';

/**
 * A manager for EFB user settings that are saved and persistent across flight sessions.
 * @internal
 */
export declare class EfbSettingsSaveManager extends UserSettingSaveManager {
    private readonly bus;
    protected readonly prefix = "efb-2024-08-28.";
    constructor(bus: EventBus);
    protected readonly settings: UserSetting<UserSettingValue>[];
    load(key: string): void;
    save(key: string): void;
    startAutoSave(key: string): void;
    stopAutoSave(key: string): void;
    pruneOldPrefixes(): void;
}
//# sourceMappingURL=EfbSettingsSaveManager.d.ts.map