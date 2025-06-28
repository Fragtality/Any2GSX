import { ArraySubject, MutableSubscribable, Subject, VNode } from '@microsoft/msfs-sdk';
import { GamepadUiComponent, GamepadUiComponentProps } from '../../Gamepad';

interface PagingListProps<T> extends GamepadUiComponentProps {
    data: ArraySubject<T>;
    renderItem: (data: T, index: number) => VNode | null;
    maxItemsPerPage: number;
    pageSelected?: MutableSubscribable<number>;
}
export declare class PagingList<T = unknown> extends GamepadUiComponent<HTMLDivElement, PagingListProps<T>> {
    protected readonly currentData: ArraySubject<T>;
    protected readonly previousData: ArraySubject<T>;
    protected readonly nextData: ArraySubject<T>;
    protected readonly firstPage: Subject<boolean>;
    protected readonly lastPage: Subject<boolean>;
    protected numberOfPages: Subject<number>;
    protected readonly pageSelected: MutableSubscribable<number, number>;
    protected previousPageSelected: number;
    protected readonly transitionWrapperRef: import('@microsoft/msfs-sdk').NodeReference<HTMLDivElement>;
    protected readonly ongoingTransition: Subject<boolean>;
    onAfterRender(node: VNode): void;
    protected updateListsData(pageSelected: number): void;
    protected onPageChanged(newPageSelected: number): Promise<void>;
    protected doTransition(side: -1 | 1, targetPage: number): Promise<void>;
    render(): VNode;
}
export {};
//# sourceMappingURL=PagingList.d.ts.map