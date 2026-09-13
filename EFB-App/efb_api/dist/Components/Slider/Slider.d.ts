import { MappedSubject, MappedSubscribable, MutableSubscribable, Subject, Subscription, VNode } from '@microsoft/msfs-sdk';
import { GamepadUiComponent, GamepadUiComponentProps } from '../../Gamepad';
import { InputActionDestructor } from '../../Listeners';
import { MaybeSubscribable } from '../../types';

export interface SliderProps extends GamepadUiComponentProps {
    /** The value that will be modified by the slider */
    value: MutableSubscribable<number> | MaybeSubscribable<number>;
    /** The step of the slider, relative to the value */
    step?: MaybeSubscribable<number>;
    /** The minimum value of the slider. Default to 0. If it is greater than the maximum, the slider will be reversed, and rendered from right to left */
    min?: MaybeSubscribable<number>;
    /** The maximum value of the slider. Default to 100. If it is lower than the minimum, the slider will be reversed, and rendered from right to left */
    max?: MaybeSubscribable<number>;
    /** Callback triggered when the value changes */
    onValueChange?: (newValue: number) => void;
    /** If true, the slider will be vertical. Default to false */
    vertical?: boolean;
    /** If true, the slider will be hoverable. Default to true */
    hoverable?: MaybeSubscribable<boolean>;
    /** If true, it will allow the mouse wheel to modify the slider value. Default to false */
    allowWheel?: boolean;
    onFocusIn?: () => void;
    onFocusOut?: () => void;
}
/** UI component that renders a slider and changes a given value accordingly */
export declare class Slider<P extends SliderProps = SliderProps> extends GamepadUiComponent<HTMLDivElement, P> {
    protected readonly value: MutableSubscribable<number>;
    protected readonly subscribableValueSubscription?: Subscription;
    protected readonly max: import('@microsoft/msfs-sdk').Subscribable<number>;
    protected readonly min: import('@microsoft/msfs-sdk').Subscribable<number>;
    protected readonly isButtonDisabled: import('@microsoft/msfs-sdk').Subscribable<boolean> | import('@microsoft/msfs-sdk').Subscribable<false> | import('@microsoft/msfs-sdk').Subscribable<true>;
    protected readonly isButtonSelected: Subject<boolean>;
    protected readonly precision: MappedSubject<[number, number, number], number>;
    protected readonly isButtonHoverable: MappedSubject<[boolean, boolean, boolean], boolean>;
    protected readonly verticalSlider: boolean;
    protected readonly allowWheel: boolean;
    protected readonly valuePercent: Subject<number>;
    protected readonly completionRatio: MappedSubscribable<string>;
    protected readonly sliderBarRef: import('@microsoft/msfs-sdk').NodeReference<HTMLDivElement>;
    protected sliderBarRect: DOMRect;
    protected allowMovement: boolean;
    protected mousePos: number;
    protected decreaseSliderActionDestructor?: InputActionDestructor;
    protected increaseSliderActionDestructor?: InputActionDestructor;
    protected decreaseSliderActionReleasedDestructor?: InputActionDestructor;
    protected increaseSliderActionReleasedDestructor?: InputActionDestructor;
    protected subs: Subscription[];
    constructor(props: P);
    protected convertValueToPercent(val: number): number;
    protected onMouseDown(): void;
    protected onMouseWheel(e: WheelEvent): void;
    private readonly onGlobalMouseUp;
    protected _onGlobalMouseUp(): void;
    private onGlobalMouseMove;
    protected _onGlobalMouseMove(e: MouseEvent): void;
    render(): VNode;
    onAfterRender(node: VNode): void;
    destroy(): void;
}
