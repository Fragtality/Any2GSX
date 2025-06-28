import { DebounceTimer, MutableSubscribable, Subject, Subscribable, Subscription, VNode } from '@microsoft/msfs-sdk';
import { GamepadUiComponent, GamepadUiComponentProps } from '../../Gamepad';
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
    onKeyPress?: (event: KeyboardEvent) => void;
    charFilter?: (char: string) => boolean;
}
export type InputProps = InputAttributes & InputHooks;
export type ExtendedInputProps = Omit<InputProps, 'type'>;
export declare class Input<T extends InputProps = InputProps> extends GamepadUiComponent<HTMLInputElement, T & GamepadUiComponentProps> {
    protected readonly uuid: string;
    private input_id;
    protected readonly inputRef: import('@microsoft/msfs-sdk').NodeReference<HTMLInputElement>;
    protected readonly model: MutableSubscribable<string, string>;
    private _reloadLocalisation;
    private readonly dispatchFocusOutEvent;
    private readonly _onKeyPress;
    private readonly _onInput;
    private readonly align;
    protected readonly debounce: DebounceTimer;
    protected onKeyPress(event: KeyboardEvent): void;
    protected onInput(): void;
    protected onInputUpdated(value: string): void;
    private reloadLocalisation;
    protected readonly _isFocused: Subject<boolean>;
    readonly isFocused: Subscribable<boolean>;
    protected onFocusIn(): void;
    protected onFocusOut(): void;
    focus(): void;
    blur(): void;
    value(): string;
    clearInput(): void;
    private _dispatchFocusOutEvent;
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
//# sourceMappingURL=Input.d.ts.map