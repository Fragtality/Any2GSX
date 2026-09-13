import { ComponentProps, DisplayComponent, EventBus, NodeReference, Subject, Subscribable, Subscription, VNode } from '@microsoft/msfs-sdk';
import { GamepadEvents } from '../Gamepad';
import { GamepadUiView, UiView, UiViewProps } from '../UiView';
import { ViewBootMode, ViewSuspendMode } from '../UiView/UiViewLifecycle';
import { Service, TVNode } from '../types';
import { Valuable } from '../utils';
import { AppViewContainer } from './AppContainer';

/** {@link UiView} type. Can be set to page or popup */
export type ViewType = 'page' | 'popup';
type UiViewRef = null | UiView | GamepadUiView<HTMLElement>;
/** The {@link UiView} entry interface */
export interface UiViewEntry<Ref = UiViewRef> {
    /** key of the {@link UiView}  */
    readonly key: string;
    /** Closure to render the view */
    readonly render: () => TVNode<UiView<UiViewProps>, UiViewProps>;
    /** The {@link UiView} vNode callback*/
    vNode: Valuable<TVNode<UiView<UiViewProps>, UiViewProps>>;
    /** Reference to the view */
    ref: Ref;
    /** Reference to the container */
    containerRef: NodeReference<AppViewWrapper>;
    /** State of the view's visibility */
    readonly isVisible: Subject<boolean>;
    /** The view-stack layer the view is in, if any */
    readonly layer: Subject<number>;
    /** The view type this entry's view is currently opened as: page, popup, or base. */
    readonly type: Subject<ViewType>;
    /** Is the view initiated. */
    isInit: boolean;
    /** the UiView viewOptions. Defaults to COLD BootMode and SLEEP SuspendMode */
    readonly viewOptions: ViewOptions;
}
/** Publicly consumable ViewEntry interface to disallow the visibility to be mutated from outside of AppViewService */
export type PublicUiViewEntry<Ref = UiViewRef> = Pick<UiViewEntry<Ref>, 'key' | 'ref'>;
/**
 * UiView lifecycle options.
 * @param BootMode {@link ViewBootMode}: specifies action when the UiView is created
 * @param SuspendMode {@link ViewSuspendMode}: specifies action when the UiView is paused
 */
export interface ViewOptions {
    BootMode: ViewBootMode;
    SuspendMode: ViewSuspendMode;
}
/**
 * Handle and track page/popup usage for an App
 */
export declare class AppViewService implements Service {
    /** Keeps track of registered views with the {@link registerView} method  */
    private readonly registeredUiViewEntries;
    private appViewRef?;
    /** keeps track of {@link UiViewEntry} history */
    private readonly appViewStack;
    private registeredUiViewPromises;
    private hasInitialized;
    readonly bus: EventBus;
    protected readonly goHomeSub: Subscription;
    constructor(bus: EventBus);
    onOpen(): void;
    onClose(): void;
    onResume(): void;
    onPause(): void;
    /**
     * Registers and renders a view (page or popup) to be opened by the service.
     * @param key The UiView string key.
     * @param type The view type
     * @param vNodeFactory A function that returns a {@link UiView} VNode for the key
     * @param options The {@link UiView} {@link ViewOptions}
     * @returns UiViewEntry
     */
    protected registerView(key: string, type: ViewType, vNodeFactory: () => TVNode<UiView<UiViewProps>, UiViewProps>, options?: Partial<ViewOptions>): UiViewEntry<UiViewRef>;
    registerPage(key: string, vNodeFactory: () => TVNode<UiView<UiViewProps>, UiViewProps>, options?: Partial<ViewOptions>): UiViewEntry<UiViewRef>;
    registerPopup(key: string, vNodeFactory: () => TVNode<UiView<UiViewProps>, UiViewProps>, options?: Partial<ViewOptions>): UiViewEntry<UiViewRef>;
    private killView;
    /**
     * @param entry a {@link UiViewEntry}
     * @param shouldOpen opens the view on initialization, defaults to false
     */
    private initViewEntry;
    /**
     * Destroys every view in registered view entries and resets the view stack.
     */
    unload(): void;
    /**
     *
     * @param homePageUiViewKey the string key of the {@link UiView}
     * @returns a Promise resolving when all pages are initialized
     */
    initialize(homePageUiViewKey: string): Promise<void>;
    /**
     * @returns the current active view entry.
     */
    private getActiveUiViewEntry;
    private readonly _currentUiView;
    /** The page that is currently open in the active view stack. */
    readonly currentUiView: Subscribable<UiViewEntry | null>;
    /** @deprecated */
    on(_event: 'pause' | 'resume', _viewKey: string, _callback: (view?: PublicUiViewEntry) => void): this;
    /**
     * Handles logic associated with changing the open page.
     * @param page Page to close
     */
    private handleCloseView;
    /**
     * Handles logic associated with changing the open page.
     * @param page Page to open
     */
    private handleOpenView;
    /**
     * Populate the view stack with its respective home page.
     * @param mainPageUiViewKey the key of the home page
     */
    private initializeAppViewStack;
    /**
     * @param key the {@link UiView} string key
     * @returns the {@link UiViewEntry} corresponding to the key
     * @throws if the {@link UiViewEntry} doesn't exists
     */
    private getUiViewEntry;
    /**
     * Called by AppContainer to pass in the refs to the view.
     * Should only be called once.
     * @param appViewRef The app view ref.
     */
    onAppContainerRendered(appViewRef: AppViewContainer): void;
    /**
     * Searches for an entry in the view stack
     * @param key the key of the entry to search for
     * @returns true if the entry has been found in the view stack, false otherwise
     */
    isInViewStack(key: string): boolean;
    isInViewStack(viewEntry: UiViewEntry): boolean;
    /**
     * Returns a boolean to inform whether an entry is active or not
     * @param key the key of the entry to search for
     * @returns true if the key in parameter corresponds to the key of the active entry, false otherwise
     */
    isActive(key: string): boolean;
    isActive(viewEntry: UiViewEntry): boolean;
    open<Ref = UiViewRef>(key: string): PublicUiViewEntry<Ref>;
    /**
     * Returns the most recent previous history state of the view stack
     * @param steps The count of steps you wants to back in history.
     * @returns the {@link PublicUiViewEntry} entry of the previous {@link UiView}
     */
    goBack(): PublicUiViewEntry | undefined;
    goBack(steps: number): PublicUiViewEntry | undefined;
    /**
     * Handles view stack logic
     * @param key the {@link UiView} string key
     * @returns the current {@link UiViewEntry}
     */
    private advanceViewStack;
    /**
     * Handle logic associated with opening a view
     * @param view the view to open.
     */
    private openView;
    /**
     * Updates all the pages/popups that are initialized and visible
     * @param time timestamp
     */
    update(time: number): void;
    /**
     * Routes the event to the current {@link UiView}
     * @param gamepadEvent the {@link GamepadEvents}
     */
    routeGamepadInteractionEvent(gamepadEvent: GamepadEvents): void;
}
/**
 * The AppViewWrapper props interface
 */
interface AppViewWrapperProps extends ComponentProps {
    /** wrapper visibility */
    isVisible: Subscribable<boolean>;
    /** The name of the view */
    viewName: string;
    /** the view type of the view */
    type: Subscribable<ViewType>;
    /** the layer index of the view */
    layer: Subscribable<number>;
}
declare class AppViewWrapper extends DisplayComponent<AppViewWrapperProps> {
    private readonly rootRef;
    render(): VNode;
    destroy(): void;
}
export {};
