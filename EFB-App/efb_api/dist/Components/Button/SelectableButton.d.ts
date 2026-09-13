import { VNode } from '@microsoft/msfs-sdk';
import { MaybeSubscribable } from '../../types';
import { TypedButton, TypedButtonProps } from './TypedButton';

/** @deprecated */
export interface SelectableButtonProps extends TypedButtonProps {
    title: MaybeSubscribable<string>;
    selected?: boolean;
}
/** @deprecated Deprecated component */
export declare class SelectableButton<P extends SelectableButtonProps = SelectableButtonProps> extends TypedButton<P> {
    protected selected: boolean;
    selectButton(): void;
    unSelectButton(): void;
    switchSelection(selected: boolean): void;
    protected buttonRender(): VNode;
}
