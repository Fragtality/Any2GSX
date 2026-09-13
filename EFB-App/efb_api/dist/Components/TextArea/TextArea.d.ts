import { MutableSubscribable, Subject, Subscribable, Subscription, VNode } from '@microsoft/msfs-sdk';
import { GamepadUiComponent, GamepadUiComponentProps } from '../../Gamepad';
import { MaybeSubscribable } from '../../types';

export interface TextAreaAttributes {
    model?: MutableSubscribable<string>;
    value?: MaybeSubscribable<string>;
    placeholder?: MaybeSubscribable<string>;
    placeholderFormat?: 'upper' | 'uppercase' | 'lower' | 'lowercase' | 'ucfirst' | 'capitalize';
    placeholderArguments?: Map<string, MaybeSubscribable<string>>;
    disabled?: MaybeSubscribable<boolean>;
    hidePlaceholderOnFocus?: boolean;
    focusOnInit?: boolean;
    rows?: number;
}
export interface TextAreaHooks {
    onFocusIn?: () => void;
    onFocusOut?: () => void;
    onInput?: (element: HTMLTextAreaElement) => void;
    onKeyPress?: (event: KeyboardEvent) => void;
    charFilter?: (char: string) => boolean;
}
export type TextAreaProps = TextAreaAttributes & TextAreaHooks;
export declare class TextArea extends GamepadUiComponent<HTMLTextAreaElement, TextAreaProps & GamepadUiComponentProps> {
    protected readonly uuid: string;
    protected readonly textAreaRef: import('@microsoft/msfs-sdk').NodeReference<HTMLTextAreaElement>;
    protected readonly model: MutableSubscribable<string, string>;
    private _reloadLocalisation;
    private readonly dispatchFocusOutEvent;
    private readonly setValueFromOS;
    private readonly _onKeyPress;
    private readonly _onInput;
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
    private _setValueFromOS;
    private getTranslation;
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
