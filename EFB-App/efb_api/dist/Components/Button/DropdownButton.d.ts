import { MutableSubscribable, Subject, SubscribableArray, Subscription, VNode } from '@microsoft/msfs-sdk';
import { GamepadUiComponent } from '../../Gamepad';
import { TTButtonProps } from './TTButton';

interface DropdownButtonProps<T> extends Omit<TTButtonProps, 'key'> {
    title: TTButtonProps['key'];
    listDataset: T[] | SubscribableArray<T>;
    getItemLabel: (item: T) => string;
    onItemClick: (item: T, index: number) => void;
    isListVisible?: MutableSubscribable<boolean>;
    showArrowIcon?: boolean;
}
export declare class DropdownButton<T = unknown> extends GamepadUiComponent<HTMLDivElement, DropdownButtonProps<T>> {
    private listDataset;
    protected readonly isListVisible: Subject<boolean>;
    protected isListVisibleSub?: Subscription;
    protected onClickOutOfComponent(): void;
    onAfterRender(node: VNode): void;
    destroy(): void;
    render(): VNode;
}
export {};
//# sourceMappingURL=DropdownButton.d.ts.map