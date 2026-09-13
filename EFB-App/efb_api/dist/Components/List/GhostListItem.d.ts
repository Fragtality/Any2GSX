import { VNode } from '@microsoft/msfs-sdk';
import { GamepadUiComponent, GamepadUiComponentProps } from '../../Gamepad';

/**
 * Temporary solution to have hidden empty list items.
 * To be used by things like flight plan lists.
 */
export declare class GhostListItem extends GamepadUiComponent<HTMLDivElement> {
    constructor(props: GamepadUiComponentProps);
    render(): VNode | null;
}
