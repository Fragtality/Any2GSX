import { InputActionDestructor } from '../Listeners';
import { InputAction } from './InputAction';

/**
 * The different input types.
 * When a button is pressed, the input type is 'pressed'.
 * When a button is released, the input type is 'released'.
 * When a joystick is moved, the input type is 'axis'.
 */
export type InputType = 'pressed' | 'released' | 'axis';
/** The input action options. */
export type InputActionOptions = {
    /** The input type. Default to 'released'. */
    readonly inputType?: InputType;
};
/**
 * The different scroll axis.
 * 'vertical' & 'y' represent the vertical axis.
 * 'horizontal' & 'x' represent the horizontal axis.
 * 'all' represent both axis.
 */
export type ScrollAxis = 'vertical' | 'y' | 'horizontal' | 'x' | 'all';
/** The predicate used to compute the scroll speed using the joystick input axis in the range 0..1 as its only parameter. */
type ScrollSpeedPredicate = (axis: number) => number;
/** The scroll input action options. */
export type ScrollInputActionOptions = {
    /** The scroll axis. Default to 'all'. */
    readonly scrollAxis?: ScrollAxis;
    /**
     * The scroll speed, either as a predicate or as a number.
     * By default, the used predicate is ƒ(x)=x².
     * If a number is provided, it will be used as a factor to the scroll speed returned by the default predicate.
     * If neither a predicate nor such factor is provided, the scroll speed factor applied to the default predicate is 10.
     */
    readonly scrollSpeed?: ScrollSpeedPredicate | number;
};
/** The manager that uses the InputStackListener in order to deliver a toolbox for gamepad input use cases. */
export declare class InternalInputManager<E extends number> {
    private readonly inputActionRecord;
    protected readonly defaultInputType: InputType;
    protected readonly defaultScrollAxis: ScrollAxis;
    protected readonly defaultScrollSpeedFactor = 10;
    private readonly inputStackListener;
    constructor(inputActionRecord: Record<E, string>);
    /**
     * Add an action that will be executed when the given input is triggered.
     * @param inputAction The input that, when triggered, will execute the action.
     * @param callback The action callback that will be called when the input is triggered.
     * @param options The optional input action options.
     * @returns A callback that has to be called when the input action is not needed anymore.
     */
    addInputAction(inputAction: E, callback: (value: number) => boolean, options?: InputActionOptions): InputActionDestructor;
    /**
     * Add an action that will be executed when the given input is triggered on hover of the given HTML element.
     * @param element The HTML element that will allow the action to be executed when hovered on.
     * @param inputAction The input that, when triggered, will execute the action.
     * @param callback The action callback that will be called when the input is triggered.
     * @param options The optional input action options.
     * @returns A callback that has to be called when the input action is not needed anymore.
     */
    addInputActionOnHover(element: HTMLElement, inputAction: E, callback: (value: number) => boolean, options?: InputActionOptions): InputActionDestructor;
}
export declare class InputManager extends InternalInputManager<InputAction> {
    constructor();
    /**
     * Add scroll input actions using the gamepad joystick for a given HTML element.
     * @param element The scrollable HTML element.
     * @param options The optional scroll input action options.
     * @returns A callback that has to be called when the scroll input action is not needed anymore.
     */
    addScrollInputAction(element: HTMLElement, options?: ScrollInputActionOptions): InputActionDestructor;
    private setupVerticalScroll;
    private setupHorizontalScroll;
    protected computeScrollSpeedFactor(axisValue: number, factor: number): number;
}
export {};
