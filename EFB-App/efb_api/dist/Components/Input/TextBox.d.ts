import { NodeReference, Subject, Subscribable, Subscription, VNode } from '@microsoft/msfs-sdk';
import { GamepadUiComponent, GamepadUiComponentProps } from '../../Gamepad';
import { MaybeSubscribable } from '../../types';
import { ExtendedInputProps, Input } from './Input';

type PrefixSuffixTypes = string | VNode;
/**
 * You can set an input but when clicking the formatter can return a different value
 * @internal
 */
export interface TextBoxProps extends ExtendedInputProps {
    /** The optional "delete" icon */
    showDeleteIcon?: boolean;
    customDeleteIcon?: HTMLElement;
    focusOnClear?: boolean;
    prefix?: MaybeSubscribable<PrefixSuffixTypes>;
    suffix?: MaybeSubscribable<PrefixSuffixTypes>;
    showPrefix?: MaybeSubscribable<boolean>;
    showSuffix?: MaybeSubscribable<boolean>;
    onDelete?: () => void;
    debounceDuration?: number;
}
/**
 * This TextBox component
 * @internal
 */
export declare class TextBox extends GamepadUiComponent<HTMLDivElement, TextBoxProps & GamepadUiComponentProps> {
    protected readonly subscriptions: Subscription[];
    readonly inputRef: NodeReference<Input<import('./Input').InputProps>>;
    private readonly model;
    protected hideDeleteTextButton: Subject<boolean>;
    protected _onDelete(event: MouseEvent): void;
    protected readonly onDelete: (event: MouseEvent) => void;
    protected onInput(input: HTMLInputElement): void;
    private readonly onmousedown;
    protected onFocusIn(): void;
    protected onFocusOut(): void;
    protected readonly prefix: Subscribable<PrefixSuffixTypes>;
    protected readonly suffix: Subscribable<PrefixSuffixTypes>;
    private readonly showPrefix;
    private readonly showSuffix;
    protected readonly prefixRef: NodeReference<HTMLDivElement>;
    protected readonly suffixRef: NodeReference<HTMLDivElement>;
    protected readonly showPrefixOrSuffixMapFunction: ([value, show]: readonly [PrefixSuffixTypes, boolean]) => boolean;
    render(): VNode;
    onAfterRender(node: VNode): void;
    destroy(): void;
}
export {};
