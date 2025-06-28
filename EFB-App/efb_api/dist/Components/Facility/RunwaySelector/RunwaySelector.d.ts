import { OneWayRunway, Subscribable, VNode } from '@microsoft/msfs-sdk';
import { GamepadUiComponent, GamepadUiComponentProps } from '../../../Gamepad';
import { NullFacility } from '../../../utils';

interface RunwaySelectorProps extends GamepadUiComponentProps {
    facility: Subscribable<NullFacility>;
    currentRunway: Subscribable<OneWayRunway | number>;
    onRunwaySelected(runway: OneWayRunway, index: number): void;
}
export declare class RunwaySelector extends GamepadUiComponent<HTMLDivElement, RunwaySelectorProps> {
    private readonly runways;
    private readonly currentRunwayName;
    render(): VNode;
}
export {};
//# sourceMappingURL=RunwaySelector.d.ts.map