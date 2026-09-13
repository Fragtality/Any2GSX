import { ArraySubject, MutableSubscribable, Subject, Subscribable, Subscription, VNode } from '@microsoft/msfs-sdk';
import { GamepadUiComponent, GamepadUiComponentProps } from '../../Gamepad';
import { MaybeSubscribable } from '../../types';

interface PagingListProps<T> extends GamepadUiComponentProps {
    /** The array subject that will be spread over the pages */
    data: ArraySubject<T>;
    /** The function that will be used to render each item. It returns the rendered VNode given an element and its index within the data array */
    renderItem: (data: T, index: number) => VNode | null;
    /** The maximum number of items per page */
    maxItemsPerPage: MaybeSubscribable<number>;
    /** The index of the selected page. The index of the first page is 0 */
    pageSelected?: MutableSubscribable<number>;
}
/**
 * This component renders a list of items through several pages (if necessary) along with buttons to switch between pages.
 */
export declare class PagingList<T = unknown> extends GamepadUiComponent<HTMLDivElement, PagingListProps<T>> {
    protected readonly currentData: ArraySubject<T>;
    protected readonly previousData: ArraySubject<T>;
    protected readonly nextData: ArraySubject<T>;
    protected readonly firstPage: Subject<boolean>;
    protected readonly lastPage: Subject<boolean>;
    protected readonly maxItemsPerPage: Subscribable<number>;
    protected readonly numberOfPages: Subject<number>;
    protected readonly pageSelected: MutableSubscribable<number, number>;
    protected previousPageSelected: number;
    protected readonly transitionWrapperRef: import('@microsoft/msfs-sdk').NodeReference<HTMLDivElement>;
    protected readonly ongoingTransition: Subject<boolean>;
    protected allowTransition: boolean;
    protected readonly subs: Subscription[];
    onAfterRender(node: VNode): void;
    protected clearData(): void;
    protected updateListsData(pageSelected: number): void;
    protected onPageChanged(newPageSelected: number): Promise<void>;
    protected doTransition(side: -1 | 1, targetPage: number): Promise<void>;
    render(): VNode;
    destroy(): void;
}
export {};
