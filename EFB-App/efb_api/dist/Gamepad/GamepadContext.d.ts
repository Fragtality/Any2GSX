import { NodeReference, Subscribable } from '@microsoft/msfs-sdk';
import { GamepadEvents } from './GamepadEvents';
import { GamepadUiComponent } from './GamepadUiComponent';

/** @internal */
export interface GamepadEventHandler {
    routeGamepadInteractionEvent(gamepadEvent: GamepadEvents): void;
}
/** @internal */
export interface ChainResponsabilityGamepadUiViewEventHandler {
    nextHandler: Subscribable<GamepadUiComponent<HTMLElement> | undefined>;
    setNextGamepadEventHandler(ref: GamepadUiComponent<HTMLElement>): void;
    deletePreviousGamepadEventHandler(): void;
    handleGamepadEvent(gamepadEvent: GamepadEvents): void;
}
/** @internal */
export interface ChainResponsabilityGamepadUiComponentEventHandler<T extends HTMLElement = HTMLElement> {
    nextHandler: Subscribable<NodeReference<T> | undefined>;
    setNextGamepadEventHandler(ref: NodeReference<T>): void;
    deletePreviousGamepadEventHandler(): void;
    handleGamepadEvent(gamepadEvent: GamepadEvents): void;
}
