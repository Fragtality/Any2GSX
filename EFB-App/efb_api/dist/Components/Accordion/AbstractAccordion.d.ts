import { MutableSubscribable, VNode } from '@microsoft/msfs-sdk';
import { GamepadUiComponent, GamepadUiComponentProps } from '../../Gamepad';

export interface AbstractAccordionProps extends GamepadUiComponentProps {
    isFolded?: boolean | MutableSubscribable<boolean>;
}
export declare abstract class AbstractAccordion<T extends AbstractAccordionProps = AbstractAccordionProps> extends GamepadUiComponent<HTMLDivElement, T> {
    protected readonly isFolded: MutableSubscribable<boolean, boolean>;
    protected abstract renderHeader(): VNode;
    protected renderBody(): VNode;
    render(): VNode;
}
