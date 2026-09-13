import { DebounceTimer, MutableSubscribable, Subject, Subscribable, Subscription, VNode } from '@microsoft/msfs-sdk';
import { GamepadUiComponent, GamepadUiComponentProps } from '../../Gamepad';
import { InputActionDestructor } from '../../Listeners';
import { MaybeSubscribable } from '../../types';

type InputHandledTypes = 'text' | 'number';
export interface InputAttributes {
    type: InputHandledTypes;
    align?: 'left' | 'center' | 'right';
    model?: MutableSubscribable<string>;
    value?: MaybeSubscribable<string>;
    placeholder?: MaybeSubscribable<string>;
    disabled?: MaybeSubscribable<boolean>;
    hidePlaceholderOnFocus?: boolean;
    focusOnInit?: boolean;
    /** The debounce time in ms */
    debounceDuration?: number;
}
export interface InputHooks {
    onFocusIn?: () => void;
    onFocusOut?: () => void;
    onInput?: (element: HTMLInputElement) => void;
    onKeyDown?: (event: KeyboardEvent) => void;
    onKeyUp?: (event: KeyboardEvent) => void;
    onKeyPress?: (event: KeyboardEvent) => void;
    charFilter?: (char: string) => boolean;
    onValueValidated?: (text: string) => void;
}
export type InputProps = InputAttributes & InputHooks;
export type ExtendedInputProps = Omit<InputProps, 'type'>;
export declare class Input<T extends InputProps = InputProps> extends GamepadUiComponent<HTMLInputElement, T & GamepadUiComponentProps> {
    protected readonly uuid: string;
    protected readonly inputRef: import('@microsoft/msfs-sdk').NodeReference<HTMLInputElement>;
    protected readonly model: MutableSubscribable<string, string>;
    private readonly internalInputManager;
    protected focusOutActionDestructor?: InputActionDestructor;
    protected validateInputActionDestructor?: InputActionDestructor;
    private _reloadLocalisation;
    private readonly dispatchFocusOutEvent;
    private readonly setValueFromOS;
    private readonly onKeyDown;
    private readonly onKeyUp;
    private readonly onKeyPress;
    private readonly onInput;
    private readonly align;
    protected readonly debounce: DebounceTimer;
    protected _onKeyDown(event: KeyboardEvent): void;
    protected _onKeyUp(event: KeyboardEvent): void;
    protected _onKeyPress(event: KeyboardEvent): void;
    protected _onInput(): void;
    protected onInputUpdated(value: string): void;
    private reloadLocalisation;
    protected readonly _isFocused: Subject<boolean>;
    readonly isFocused: Subscribable<boolean>;
    private readonly focusSubscription;
    protected onFocusIn(): void;
    protected onFocusOut(): void;
    value(): string;
    clearInput(): void;
    focus(): void;
    blur(): void;
    private _dispatchFocusOutEvent;
    private _setValueFromOS;
    /** Placeholder i18n/visibility */
    private readonly placeholderKey;
    private readonly placeholderShown;
    private readonly placeholderTranslation;
    private readonly hidePlaceholderOnFocus;
    render(): VNode;
    protected readonly subs: Subscription[];
    onAfterRender(node: VNode): void;
    destroy(): void;
}
export {};
