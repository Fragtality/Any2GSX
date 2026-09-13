import { MutableSubscribable, Subscribable, SubscribableArray, VNode } from '@microsoft/msfs-sdk';
import { GamepadUiComponent, GamepadUiComponentProps } from '../../Gamepad';
import { MaybeSubscribable, Nullable, StyleProp } from '../../types';
import { GamepadListNavigationManager } from './GamepadListNavigationManager';

/** The options used for the list gamepad navigation. */
export interface ListGamepadNavigationOptions<T> {
    /** Whether the list is currently being browsed or not. */
    isListFocused?: MutableSubscribable<boolean> | MaybeSubscribable<boolean>;
    /** The callback that will be called when an item is selected. */
    onItemSelected?: (item: T, index: number) => void;
    /**
     * The callback that will be called when an item is hovered.
     * When no item is hovered anymore, the callback will be called with a nullish item and an index of -1.
     */
    onItemHovered?: (item: Nullable<T>, index: number) => void;
    /**
     * @description The delay in milliseconds between the navigation of two items.
     * @default 100
     */
    navigationBetweenItemsDelayMs?: number;
    /**
     * @description Whether the list should wrap around when reaching its start/end.
     * @default false
     */
    wrap?: boolean;
}
/** The properties of the List component. */
export interface ListProps<T> extends GamepadUiComponentProps {
    /** The data collection of items that will be rendered. */
    data: SubscribableArray<T>;
    /** The callback that will be used to render each item of the data collection. */
    renderItem: {
        (item: T, index: number): VNode | null;
    };
    /** @deprecated Use the onItemSelected field of the gamepadNavigationOptions property instead. */
    onItemSelected?: (item: T, index: number) => void;
    /** If false, the list won't be displayed in the DOM. Default to true. */
    isListVisible?: Subscribable<boolean>;
    /** If true, the list will be entirely re-rendered every time changes are detected in the data collection. Default to false. */
    refreshOnUpdate?: boolean;
    /** If true, the list will be scrollable. Default to false */
    isScrollable?: boolean;
    /** The style that will be used by the list stylesheet */
    style?: StyleProp;
    /**
     * The options for the list navigation using a gamepad.
     * If not provided, no gamepad navigation will be available.
     */
    gamepadNavigationOptions?: ListGamepadNavigationOptions<T>;
}
/** A component that allows the rendering of a collection of data. */
export declare class List<T = unknown> extends GamepadUiComponent<HTMLDivElement, ListProps<T>> {
    private readonly renderedItems;
    private readonly gamepadComponentChildren;
    private readonly nbRenderedItems;
    private readonly renderedItemsSubscription;
    private readonly isListVisible;
    private readonly isListVisibleSubscription;
    protected previousItemIndex?: number;
    protected gamepadNavigationManager?: GamepadListNavigationManager<T>;
    private scrollActionDestructor?;
    private readonly onDataChangedSubscription;
    constructor(props: ListProps<T>);
    /**
     * Scroll the list to a specific item given its index.
     * @param index The index of the item to scroll to.
     * @throws Error if the index is out of bounds or if the item is not an Element.
     */
    scrollToItem(index: number): void;
    private cleanRenderedContent;
    private onDataChanged;
    private addDomNode;
    private removeDomNode;
    protected enableGamepadInputsOnHover(element: HTMLElement): void;
    enableGamepadInputs(): void;
    disableGamepadInputs(): void;
    private renderList;
    render(): VNode;
    onAfterRender(node: VNode): void;
    destroy(): void;
}
