import { Subject } from '@microsoft/msfs-sdk';

/**
 * @deprecated
 */
export declare class InputsListener extends ViewListener.ViewListener {
    /**
     * @deprecated
     */
    static isLoaded: Subject<boolean>;
    private static inputsListener;
    /**
     * @deprecated
     * @param context The input context.
     * @param action The input action.
     * @param callback The callback that will be called when the input is triggered.
     * @returns The input watcher ID.
     */
    static addInputChangeCallback(context: string, action: string, callback: (down: boolean) => void): string;
    /**
     * @deprecated
     * @param id The event ID.
     */
    static removeInputChangeCallback(id: string): void;
}
