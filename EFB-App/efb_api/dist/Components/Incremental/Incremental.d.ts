import { MutableSubscribable, Subject, Subscription, VNode } from '@microsoft/msfs-sdk';
import { GamepadUiComponent, GamepadUiComponentProps } from '../../Gamepad';
import { MaybeSubscribable } from '../../types';

export interface IncrementalProps extends GamepadUiComponentProps {
    /** The value that is increased or decreased. If a MutableSubscribable is given, it will be directly modified by the component */
    value: MutableSubscribable<number> | MaybeSubscribable<number>;
    /** The suffix rendered at the right of the value */
    suffix?: MaybeSubscribable<string>;
    /** The step of the increment or decrement */
    step?: MaybeSubscribable<number>;
    /** The lower boundary of the value */
    min?: MaybeSubscribable<number>;
    /** The upper boundary of the value */
    max?: MaybeSubscribable<number>;
    /** A function which formats numbers. */
    formatter?: (value: number, max: number) => string;
    /** Callback triggered when the value is changed, giving directly the new value */
    onChange?: (newValue: number) => void;
    /** Callback triggered when a button is clicked, giving -1 if the first button is clicked and 1 for the second one */
    onButtonClicked?: (direction: -1 | 1, newValue: number) => void;
    /** First icon. Can be a path that will be used in an IconButton, or a VNode. By default, it renders an IconButton with a minus icon */
    firstIcon?: string | VNode;
    /** Second icon. Can be a path that will be used in an IconButton, or a VNode. By default, it renders an IconButton with a plus icon */
    secondIcon?: string | VNode;
    /** If true, the value rendered in the middle will be in a textbox, allowing tà modify it without the buttons. Default to true */
    useTextbox?: boolean;
    /** An alternative to the onChange method, but only when the textbox is changed via the 'keydown' event */
    onKeyDown?: (event: KeyboardEvent, previousValue: number) => void;
    /** An alternative to the onChange method, but only when the textbox is changed via the 'keyup' event */
    onKeyUp?: (event: KeyboardEvent, newValue: number) => void;
    /** An alternative to the onChange method, but only when the textbox is changed via the 'keypress' event */
    onKeyPress?: (event: KeyboardEvent, newValue: number) => void;
}
/** Renders a value along with 2 buttons to increase or decrease it */
export declare class Incremental extends GamepadUiComponent<HTMLDivElement, IncrementalProps> {
    protected readonly value: MutableSubscribable<number>;
    protected readonly valueSubscription: Subscription;
    protected readonly displayedValueSubscription: Subscription;
    protected readonly subscribableValueSubscription?: Subscription;
    protected min: import('@microsoft/msfs-sdk').Subscribable<number>;
    protected max: import('@microsoft/msfs-sdk').Subscribable<number>;
    protected step: import('@microsoft/msfs-sdk').Subscribable<number>;
    protected readonly formatter: (value: number, max: number) => string;
    protected readonly useTextbox: boolean;
    protected readonly displayedValue: Subject<string>;
    constructor(props: IncrementalProps);
    private renderButton;
    private renderData;
    protected onTextBoxFocusIn(): void;
    protected onTextBoxFocusOut(): void;
    render(): VNode;
    destroy(): void;
}
