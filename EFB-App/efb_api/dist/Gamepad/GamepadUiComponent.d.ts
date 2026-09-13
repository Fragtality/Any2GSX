import { ComponentProps, DisplayComponent, NodeReference, Subject, Subscribable, VNode } from '@microsoft/msfs-sdk';
import { AppViewService } from '../AppView';
import { InputManager } from '../Input';
import { ClassProp, StyleProp } from '../types/ComponentProps';
import { ChainResponsabilityGamepadUiViewEventHandler } from './GamepadContext';
import { GamepadEvents } from './GamepadEvents';

/** @internal */
export interface GamepadUiComponentProps extends ComponentProps {
    /** the app view service */
    appViewService?: AppViewService;
    /** Additionnal custom class */
    class?: ClassProp;
    style?: StyleProp;
    /** The callback to call on push button A */
    onButtonAPressed?(): void;
    /** The callback to call on push button B */
    onButtonBPressed?(): void;
    /** an optional parameter to disable the component */
    disabled?: boolean | Subscribable<boolean>;
    visible?: boolean | Subscribable<boolean>;
    onboardingStepId?: string;
}
/** @internal */
export declare abstract class GamepadUiComponent<T extends HTMLElement, P extends GamepadUiComponentProps = GamepadUiComponentProps> extends DisplayComponent<P> implements ChainResponsabilityGamepadUiViewEventHandler {
    private static FOCUS_CLASS;
    private readonly classSubscriptions;
    private isVisibleSubscription?;
    private isDisabledSubscription?;
    readonly gamepadUiComponentRef: NodeReference<T>;
    protected readonly inputManager: InputManager;
    protected readonly areGamepadInputsEnabled: Subject<boolean>;
    private readonly _nextHandler;
    readonly nextHandler: Subscribable<GamepadUiComponent<T> | undefined>;
    protected readonly disabled: Subscribable<boolean> | Subscribable<false> | Subscribable<true>;
    private readonly visible;
    private readonly componentClickListener;
    setNextGamepadEventHandler(ref: GamepadUiComponent<T>): void;
    deletePreviousGamepadEventHandler(): void;
    handleGamepadEvent(gamepadEvent: GamepadEvents): void;
    onAfterRender(node: VNode): void;
    private handleComponentClick;
    onButtonAPressed(): void;
    onButtonBPressed(): void;
    protected onClickOutOfComponent(_e: Event): void;
    enableGamepadInputs(): void;
    disableGamepadInputs(): void;
    private enable;
    private disable;
    show(): void;
    hide(): void;
    toggleFocus(value?: boolean): void;
    getComponentRect(): DOMRect;
    destroy(): void;
}
