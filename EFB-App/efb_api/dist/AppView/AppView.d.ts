import { ComponentProps, DisplayComponent, EventBus, NodeInstance, VNode } from '@microsoft/msfs-sdk';
import { AppViewService } from '../AppView/AppViewService';
import { GamepadEventHandler, GamepadEvents } from '../Gamepad';
import { NotificationManager } from '../Managers';
import { OnboardingManager } from '../Managers/OnboardingManager/OnboardingManager';
import { EfbSettingsManager, UnitsSettingsManager } from '../Settings';
import { Nullable, Promiseable, TVNode } from '../types';

/** AppViewProps */
export interface AppViewProps extends ComponentProps {
    /** The AppViewService instance */
    appViewService?: AppViewService;
    /** The event bus */
    bus?: EventBus;
    /** the units setting manager */
    unitsSettingManager?: UnitsSettingsManager;
    /** the general setting manager */
    efbSettingsManager?: EfbSettingsManager;
    /** the notification manager */
    notificationManager?: NotificationManager;
    /** the onboarding manager */
    onboardingManager?: OnboardingManager;
}
/**
 * ```
 * Mandatory class to extend for the App's render method
 * It's the entrypoint view of every EFB apps.
 * All events from the EFB are sended to this class when the app is visible.
 * ```
 */
export declare abstract class AppView<T extends AppViewProps = AppViewProps> extends DisplayComponent<T> implements GamepadEventHandler {
    private readonly _appViewService?;
    private readonly _bus?;
    private readonly _unitsSettingManager?;
    private readonly _efbSettingsManager?;
    private readonly _notificationManager?;
    private readonly _onboardingManager?;
    protected readonly rootRef: import('@microsoft/msfs-sdk').NodeReference<HTMLElement>;
    /**
     * Get AppViewService instance
     * @throws {Error}
     */
    protected get appViewService(): AppViewService;
    /**
     * Get EventBus instance
     * @throws {Error}
     */
    protected get bus(): EventBus;
    /**
     * Get units setting manager instance
     * @throws {Error}
     */
    protected get unitsSettingManager(): UnitsSettingsManager;
    /**
     * Get general setting manager instance
     * @throws {Error}
     */
    protected get efbSettingsManager(): EfbSettingsManager;
    /**
     * EFB notification manager
     * @returns a unique efbNotificationManager instance
     */
    get notificationManager(): NotificationManager;
    /** Get onboarding manager instance
     * @throws {Error}
     */
    protected get onboardingManager(): OnboardingManager;
    constructor(props: T);
    /**
     * Called once when the view is opened for the first time.
     */
    onOpen(): void;
    /**
     * Called once when the view is destroyed.
     */
    onClose(): void;
    /**
     * Called each time the view is resumed.
     */
    onResume(): void;
    /**
     * Called each time the view is closed.
     */
    onPause(): void;
    /**
     * On Update loop - It update the `AppViewService` if it is used.
     * @param time in milliseconds
     */
    onUpdate(time: number): void;
    /**
     * Define the view the app should show immediatly after being initialized.
     */
    protected readonly defaultView?: string;
    /**
     * Callback to register all views the app might use.
     */
    protected registerViews(): void;
    /**
     * @internal
     * TODO : Need to be documented after Gamepad integration
     */
    routeGamepadInteractionEvent(gamepadEvent: GamepadEvents): void;
    protected pageKeyActions: Map<Lowercase<string>, (args: Nullable<string[]>) => Promiseable<void | string>>;
    /**
     * @internal
     * @param key custom page key defined in `pageKeyActions`
     * @param args array of arguments given to the defined callback
     */
    handlePageKeyAction(key: Nullable<Lowercase<string>>, args: Nullable<string[]>): void;
    /**
     * If using EFB's AppViewService, this method returns an AppContainer binded to AppViewService.
     * Otherwise it can be customized with plain JSX/TSX or custom view service, etc...
     *
     * @example
     * Surrounding AppContainer with a custom class:
     * ```ts
     * public render(): TVNode<HTMLDivElement> {
     * 	return <div class="my-custom-class">{super.render()}</div>;
     * }
     * ```
     * @example
     * Here's an plain JSX/TSX example:
     * ```ts
     * public render(): TVNode<HTMLSpanElement> {
     * 	return <span>Hello World!</span>;
     * }
     * ```
     */
    render(): TVNode<NodeInstance, T>;
    /** @inheritdoc */
    onAfterRender(node: VNode): void;
    /** @internal */
    destroy(): void;
}
