import { Subscribable } from '@microsoft/msfs-sdk';

/**
 * The destructor callback that has to be called when the input action is no longer needed.
 */
export type InputActionDestructor = () => void;
/**
 * The listener used to interface the back-end input stack.
 */
export declare class InputStackListener {
    private readonly _isReady;
    /**
     * A subscribable that is set to true (and never set back to false) once the listener has been successfully registered.
     */
    readonly isReady: Subscribable<boolean>;
    private readonly inputActionMap;
    private readonly inputStackListener;
    constructor();
    private _onInputTriggeredCallback;
    private onInputTriggeredCallback;
    /**
     * Add an action that will be executed when the given input is triggered.
     * @param input The input that, when triggered, will execute the action.
     * @param callback The action callback that will be called when the input is triggered.
     * @param inputType The trigger type of the input between 'pressed', 'released' and 'axis'. Default to 'released'.
     * @param context The context which the input will be applied to. Default to 'DEFAULT'.
     * @returns A callback that has to be called when the input action is not needed anymore.
     */
    addInputAction(input: string, callback: (value: number) => boolean, inputType?: string, context?: string): InputActionDestructor;
}
