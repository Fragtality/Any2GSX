import { MutableSubscribable, VNode } from '@microsoft/msfs-sdk';
import { GamepadUiComponent, GamepadUiComponentProps } from '../../Gamepad';

type SwitchDirection = 'left' | 'right';
interface SwitchProps extends GamepadUiComponentProps {
    turnOnDirection?: SwitchDirection;
    checked?: boolean | MutableSubscribable<boolean>;
    callback?: (checked: boolean) => void;
}
export declare class Switch extends GamepadUiComponent<HTMLInputElement, SwitchProps> {
    protected readonly turnOnDirection: SwitchDirection;
    protected readonly checked: MutableSubscribable<boolean, boolean>;
    protected readonly turnOnDirectionSub: import('@microsoft/msfs-sdk').MappedSubscribable<boolean> | MutableSubscribable<boolean, boolean>;
    private readonly sliderRef;
    onAfterRender(node: VNode): void;
    render(): VNode;
}
export {};
//# sourceMappingURL=Switch.d.ts.map