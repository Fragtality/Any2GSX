import { SubscribableArray } from '@microsoft/msfs-sdk';
import { ListGamepadNavigationOptions } from './List';

/** The manager used to allow the List component to be navigated through via gamepad device. */
export declare class GamepadListNavigationManager<T = unknown> {
    private readonly data;
    private readonly toolbox;
    /** Navigation heuristic variables */
    private static readonly defaultNavigationBetweenItemsDelayMs;
    private readonly navigationBetweenItemsDelayMs;
    private allowedUpcomingNavigationMs;
    private readonly computeUpcomingItemIndex;
    /**
     * @description Whether the list is focused or not, therefore allowing its navigation.
     * @default false
     */
    private readonly isListFocused;
    /**
     * @description The index of the current item in the list navigation. -1 if no item is selected.
     * @default -1
     */
    private readonly currentItemIndex;
    private readonly isListFocusedSubscriptions;
    private readonly currentItemIndexSubscription;
    private readonly internalInputManager;
    private unfocusListActionDestructor?;
    private selectItemActionDestructor?;
    private prevItemActionDestructor?;
    private nextItemActionDestructor?;
    constructor(data: SubscribableArray<T>, toolbox: ListGamepadNavigationOptions<T>);
    private navigateThroughList;
    private enableGamepadNavigation;
    private disableGamepadNavigation;
    pause(): void;
    resume(): void;
    destroy(): void;
}
