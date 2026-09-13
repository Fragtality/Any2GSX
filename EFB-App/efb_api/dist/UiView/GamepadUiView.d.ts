import { Subject, Subscribable, VNode } from '@microsoft/msfs-sdk';
import { ChainResponsabilityGamepadUiViewEventHandler, GamepadEvents, GamepadUiComponent } from '../Gamepad';
import { InputManager } from '../Input/InputManager';
import { UiView, UiViewProps } from './UiView';

export declare abstract class GamepadUiView<T extends HTMLElement, P extends UiViewProps = UiViewProps> extends UiView<P> implements ChainResponsabilityGamepadUiViewEventHandler {
    protected readonly gamepadUiViewRef: import('@microsoft/msfs-sdk').NodeReference<T>;
    private readonly gamepadUiParser;
    private readonly _nextHandler;
    readonly nextHandler: Subscribable<GamepadUiComponent<HTMLElement> | undefined>;
    protected readonly inputManager: InputManager;
    protected readonly gamepadComponentChildren: Array<GamepadUiComponent<HTMLElement>>;
    protected readonly areGamepadInputsEnabled: Subject<boolean>;
    private readonly viewCoreInputManager;
    private goBackActionDestructor?;
    /**
     * Enable the gamepad inputs for this UI view.
     * Gamepad inputs of any UI view shall be added in its own enableGamepadInputs function via inputManager.
     */
    protected enableGamepadInputs(): void;
    /**
     * Disable the gamepad inputs for this UI view.
     * Gamepad input destructors of any UI view shall be called in its own disableGamepadInputs function.
     */
    protected disableGamepadInputs(): void;
    onClose(): void;
    onResume(): void;
    onPause(): void;
    onAfterRender(node: VNode): void;
    destroy(): void;
    setNextGamepadEventHandler(ref: GamepadUiComponent<HTMLElement>): void;
    deletePreviousGamepadEventHandler(): void;
    handleGamepadEvent(_gamepadEvent: GamepadEvents): void;
}
