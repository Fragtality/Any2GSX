import { EventBus, SubscribableArray } from '@microsoft/msfs-sdk';
import { App, AppConstructor, AppOptions, IApp } from './App';
import { NotificationManager } from './Managers';
import { OnboardingManager } from './Managers/OnboardingManager/OnboardingManager';
import { EfbSettingsManager, UnitsSettingsManager } from './Settings';

declare global {
    interface Window {
        EFB_API: Container | undefined;
    }
}
/** EFB Instance */
export declare class Container {
    /** @private */
    _uid: number;
    private static _instance?;
    /** App install promises */
    private _registeredAppsPromises;
    /** Installed apps */
    private _installedApps;
    private bus?;
    /** Units settings manager */
    private unitsSettingManager?;
    /** Main settings manager */
    private efbSettingsManager?;
    /** Notification manager */
    private notificationManager?;
    /** Onboarding manager */
    private onboardingManager?;
    private constructor();
    /**
     * Static singleton instance of Efb container
     * @internal
     */
    static get instance(): Container;
    /** @internal */
    apps(): SubscribableArray<IApp>;
    /** @internal */
    allAppsLoaded(): boolean;
    /**
     * Method used by the OS to share the bus to apps
     * @internal
     */
    setBus(bus: EventBus): this;
    /**
     * Method used by the OS to share the units settings manager to the apps
     * @internal
     */
    setUnitsSettingManager(unitsSettingManager: UnitsSettingsManager): this;
    /**
     * Method used by the OS to share the settings manager to the apps
     * @internal
     */
    setEfbSettingManager(efbSettingsManager: EfbSettingsManager): this;
    setOnboardingManager(onboardingManager: OnboardingManager): this;
    /**
     * Method used by the OS to share the notification manager to the apps
     * @internal
     */
    setNotificationManager(notificationManager: NotificationManager): this;
    /**
     * Load stylesheet
     * @param uri
     * @returns Promise which is resolved when stylesheet is loaded or rejected if an error occur.
     */
    loadCss(uri: string): Promise<void>;
    /**
     * Load script file
     * @param uri
     * @returns Promise which is resolved when script is loaded or rejected if an error occur.
     */
    loadJs(uri: string): Promise<void>;
    /**
     * Register an app in EFB
     * @template T - App registration options
     * @param app The app you wan't to register
     * @param options Options you'r app might need when installing
     * @returns EFB instance
     * @throws Throw an error if App install went wrong.
     */
    use<T extends AppOptions = AppOptions>(app: AppConstructor<T> | App<T>, ...options: Array<Partial<T>>): this;
}
/** EFB Instance */
export declare const Efb: Container;
/** @internal */
export declare const EfbApiVersion = "1.0.3";
