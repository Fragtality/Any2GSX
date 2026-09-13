import { MutableSubscribable, Subject, VNode } from '@microsoft/msfs-sdk';
import { GamepadUiComponent, GamepadUiComponentProps } from '../../Gamepad';

/**
 * TODO Add units to the props
 * @internal
 */
export interface UnitsBoxProps extends GamepadUiComponentProps {
    valueSub: MutableSubscribable<number>;
    defaultValue?: number;
}
/**
 * @deprecated UnitsBox is the old version of UnitBox. Use UnitBox instead.
 */
export declare class UnitsBox extends GamepadUiComponent<HTMLDivElement, UnitsBoxProps> {
    protected readonly inputSub: Subject<string>;
    render(): VNode;
}
