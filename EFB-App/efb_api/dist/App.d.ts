import { EventBus, MutableSubscribable, NodeInstance, Subject } from '@microsoft/msfs-sdk';
import { AppBootMode, AppSuspendMode } from './AppLifecycle';
import { AppView } from './AppView';
import { NotificationManager } from './Managers';
import { OnboardingManager } from './Managers/OnboardingManager/OnboardingManager';
import { EfbSettingsManager, UnitsSettingsManager } from './Settings';
import { TVNode } from './types';

export interface IApp<T extends object = object> {
    BootMode: AppBootMode;
    SuspendMode: AppSuspendMode;
    available: MutableSubscribable<boolean>;
    _install(props: AppInstallProps<T>): Promise<void>;
    install(props: AppInstallProps<T>): Promise<void>;
    get internalName(): string;
    get unitsSettingsManager(): UnitsSettingsManager;
    get compatibleAircraftModels(): string[] | undefined;
    get favoriteIndex(): number;
    set favoriteIndex(index: number);
    get name(): string;
    get icon(): string;
    render(): TVNode<AppView & NodeInstance>;
}
export interface AppConstructor<T extends object = object> {
    new (): App<T>;
}
export interface AppInstallProps<T extends object = object> {
    bus: EventBus;
    unitsSettingManager?: UnitsSettingsManager;
    efbSettingsManager?: EfbSettingsManager;
    notificationManager?: NotificationManager;
    onboardingManager?: OnboardingManager;
    favoriteIndex?: number;
    options: T;
}
/**
 * ```
 * Class that all Apps must extends to be registered.
 * It is used to setup how the app is working.
 * ```
 * @template T App options. ie : you need multiple instance of an App with different styles for development.
 */
export declare abstract class App<T extends object = object> implements IApp<T> {
    private _isInstalled;
    private _isReady;
    private _unitsSettingsManager?;
    private _efbSettingsManager?;
    private _notificationManager?;
    private _onboardingManager?;
    private _favoriteIndex;
    protected bus: EventBus;
    protected options: T;
    readonly available: Subject<boolean>;
    /**
     * Desired AppBootMode
     * @defaultValue > `AppBootMode.COLD`
     */
    BootMode: AppBootMode;
    /**
     * Desired AppSuspendMode
     * @defaultValue > `AppSuspendMode.SLEEP`
     */
    SuspendMode: AppSuspendMode;
    /**
     * @param props
     * @internal
     */
    _install(props: AppInstallProps<T>): Promise<void>;
    /**
     * Install hook
     * @param props
     */
    install(props: AppInstallProps<T>): Promise<void>;
    /** Boolean to check if app is loaded and installed. */
    get isReady(): boolean;
    /**
     * Internal app name
     * @defaultValue > Class's name (`this.constructor.name`)
     */
    get internalName(): string;
    /**
     * EFB units settings manager
     * @returns a unique unitsSettingsManager instance
     */
    get unitsSettingsManager(): UnitsSettingsManager;
    /**
     * EFB settings manager
     * @returns a unique efbSettingsManager instance
     */
    get efbSettingsManager(): EfbSettingsManager;
    /**
     * EFB notification manager
     * @returns a unique efbNotificationManager instance
     */
    get notificationManager(): NotificationManager;
    /** Onboarding manager */
    get onboardingManager(): OnboardingManager;
    /**
     * Aircraft models list compatible with the App. If not defined, the App is compatible with all aircraft models.
     * example: ['Cabri G2', 'H125']
     * @returns a list of aircraft models compatible with the App or undefined
     */
    get compatibleAircraftModels(): string[] | undefined;
    /** @internal */
    get favoriteIndex(): number;
    /** @internal */
    set favoriteIndex(index: number);
    /** App friendly name (shown on the App List) */
    abstract get name(): string;
    /** App icon (must return an uri) */
    abstract get icon(): string;
    /** Must render an extended AppView for the App */
    abstract render(): TVNode<AppView>;
}
//# sourceMappingURL=App.d.ts.map