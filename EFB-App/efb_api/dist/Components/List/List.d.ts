import { Subscribable, SubscribableArray, VNode } from '@microsoft/msfs-sdk';
import { GamepadUiComponent, GamepadUiComponentProps } from '../../Gamepad';
import { StyleProp } from '../../types';

export interface ListProps<T> extends GamepadUiComponentProps {
    data: SubscribableArray<T>;
    renderItem: {
        (data: T, index: number): VNode | null;
    };
    onItemSelected?: (data: T, index: number) => void;
    isListVisible?: Subscribable<boolean>;
    refreshOnUpdate?: boolean;
    isScrollable?: boolean;
    style?: StyleProp;
}
export declare class List<T = unknown> extends GamepadUiComponent<HTMLDivElement, ListProps<T>> {
    private readonly _itemInstanceRefs;
    private gamepadInputManager?;
    private dataSubs;
    onAfterRender(node: VNode): void;
    destroy(): void;
    show(): void;
    hide(): void;
    private onDataChanged;
    private addDomNode;
    private renderListItem;
    private removeDomNode;
    private renderList;
    private refreshListVisibility;
    render(): VNode;
}
//# sourceMappingURL=List.d.ts.map