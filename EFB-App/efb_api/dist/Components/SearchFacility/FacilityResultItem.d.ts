import { Facility, VNode } from '@microsoft/msfs-sdk';
import { GamepadUiComponent, GamepadUiComponentProps } from '../../Gamepad';

export interface FacilityResultItemProps extends GamepadUiComponentProps {
    /** The resulting facility. */
    facility: Facility;
    /** the ICAO / Name separator */
    separator: ' - ' | ', ' | ' ' | ' / ';
    /** the callback to click on call */
    callback?: (facility: Facility) => void;
}
export declare class FacilityResultItem extends GamepadUiComponent<HTMLDivElement, FacilityResultItemProps> {
    onAfterRender(node: VNode): void;
    render(): VNode | null;
}
