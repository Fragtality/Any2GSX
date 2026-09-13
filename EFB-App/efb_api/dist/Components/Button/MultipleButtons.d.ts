import { DisplayComponent, NodeReference, Subject, VNode } from '@microsoft/msfs-sdk';
import { GamepadUiComponent, GamepadUiComponentProps } from '../../Gamepad';
import { MaybeSubscribable } from '../../types';
import { ButtonProps } from './Button';

export interface MultipleButtonsProps extends GamepadUiComponentProps {
    titles: MaybeSubscribable<string>[];
    buttonSelected?: number;
    /** Callback when a button is clicked. If true is return, the selectButton method won't be called when a button is clicked. */
    callback?: (index: number) => boolean;
}
export declare class MultipleButtons<P extends MultipleButtonsProps = MultipleButtonsProps> extends GamepadUiComponent<HTMLDivElement, P> {
    protected buttonSelected: number;
    protected readonly buttonRefs: Array<NodeReference<SelectableButton>>;
    onAfterRender(node: VNode): void;
    protected buttonCallback(buttonIndex: number): void;
    selectButton(buttonIndex: number): void;
    protected unselectAllButtons(): void;
    renderSelectableButtons(): HTMLDivElement[];
    render(): VNode;
}
interface SelectableButtonProps extends ButtonProps {
    title: MaybeSubscribable<string>;
    selected?: boolean;
}
declare class SelectableButton extends DisplayComponent<SelectableButtonProps> {
    protected readonly selected: Subject<boolean>;
    selectButton(): void;
    unSelectButton(): void;
    render(): VNode;
}
export {};
