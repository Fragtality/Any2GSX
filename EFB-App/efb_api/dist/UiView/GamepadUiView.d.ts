import { Subscribable, VNode } from '@microsoft/msfs-sdk';
import { ChainResponsabilityGamepadUiViewEventHandler, GamepadEvents, GamepadUiComponent } from '../Gamepad';
import { UiView, UiViewProps } from './UiView';

export declare abstract class GamepadUiView<T extends HTMLElement, P extends UiViewProps = UiViewProps> extends UiView<P> implements ChainResponsabilityGamepadUiViewEventHandler {
    protected readonly gamepadUiViewRef: import('@microsoft/msfs-sdk').NodeReference<T>;
    private readonly gamepadUiParser;
    private readonly _nextHandler;
    readonly nextHandler: Subscribable<GamepadUiComponent<HTMLElement> | undefined>;
    onAfterRender(node: VNode): void;
    setNextGamepadEventHandler(ref: GamepadUiComponent<HTMLElement>): void;
    deletePreviousGamepadEventHandler(): void;
    handleGamepadEvent(_gamepadEvent: GamepadEvents): void;
}
//# sourceMappingURL=GamepadUiView.d.ts.map