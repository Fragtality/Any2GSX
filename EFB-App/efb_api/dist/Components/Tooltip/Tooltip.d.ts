import { VNode } from '@microsoft/msfs-sdk';
import { GamepadUiComponent, GamepadUiComponentProps } from '../../Gamepad';
import { MaybeSubscribable } from '../../types';
import { Valuable } from '../../utils';

interface TooltipProps extends GamepadUiComponentProps {
    content: Valuable<VNode | MaybeSubscribable<string>>;
    position?: 'top' | 'bottom' | 'left' | 'right';
    condition?: MaybeSubscribable<boolean>;
}
export declare class Tooltip extends GamepadUiComponent<HTMLDivElement, TooltipProps> {
    private readonly forceHide;
    render(): VNode;
}
export {};
