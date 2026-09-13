import { EventBus, UserSettingDefinition, UserSettingManagerEntry } from '@microsoft/msfs-sdk';
import { IApp } from '../App';
import { EfbCommonSettingsManager } from './EfbCommonSettingsManager';

export declare enum EfbMode {
    '2D' = 0,
    '3D' = 1
}
export declare enum EfbSizeSettingMode {
    Small = 0,
    Medium = 1,
    Large = 2
}
export declare enum OrientationSettingMode {
    Vertical = 0,
    Horizontal = 1
}
export type EfbSettingsType = {
    /** The EFB mode (2D or 3D) */
    mode: EfbMode;
    /** The orientation mode setting */
    efbSize: EfbSizeSettingMode;
    /** The orientation mode setting */
    orientationMode: OrientationSettingMode;
    /** Wether we are using auto brightness or not */
    isBrightnessAuto: boolean;
    /** Auto brightness percentage used to set manual when toggling auto/manual (will only be updated if we are on SettingsApp) */
    autoBrightnessPercentage: number;
    /** Current brightness percentage used on slider (will only be updated if we are on SettingsApp) */
    manualBrightnessPercentage: number;
    /** The favorite apps setting. */
    favoriteApps: string;
    /** The app that appears when the efb is loaded for the first time */
    defaultApp: string;
};
export declare class EfbSettingsManager extends EfbCommonSettingsManager<EfbSettingsType> {
    protected favoriteApps: string[];
    constructor(bus: EventBus, settingsDefs: Array<UserSettingDefinition<EfbSettingsType[keyof EfbSettingsType]>>);
    get favoriteAppsArray(): readonly string[];
    /**
     * Checks if the values loaded from the datastorage correspond to the settings types.
     */
    checkLoadedValues(): void;
    /**
     * Add an app to the favorites
     * @param app The app to add
     * @returns the EFB settings manager
     */
    addAppToFavorites(app: IApp): this;
    /**
     * Remove an app from the favorites
     * @param app The app to remove
     * @returns the EFB settings manager
     */
    removeAppFromFavorites(app: IApp): this;
    /**
     * Update the favoriteSetting and the apps array in the EFB instance in order to rerender when the favorite apps change
     */
    protected onFavoriteAppsUpdated(): void;
    protected updateFavoriteAppsArray(value: EfbSettingsType['favoriteApps']): void;
    /** @inheritdoc */
    protected ignoredForceSaveSettingKeys: Array<keyof EfbSettingsType>;
    protected onSettingValueChanged<K extends keyof EfbSettingsType>(entry: UserSettingManagerEntry<EfbSettingsType[K]>, value: EfbSettingsType[K]): void;
}
/**
 * Utility class for retrieving EFB setting managers.
 * @internal
 */
export declare class EfbSettings {
    private static INSTANCE;
    private constructor();
    static getManager(bus: EventBus): EfbSettingsManager;
}
