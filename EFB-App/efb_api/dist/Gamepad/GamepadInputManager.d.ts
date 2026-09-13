/**
 * @deprecated
 */
export type ScrollDirection = 'vertical' | 'y' | 'horizontal' | 'x' | 'all';
/**
 * @deprecated
 */
export declare class GamepadInputManager {
    private readonly element;
    /**
     * @deprecated
     */
    readonly defaultScrollDirection: ScrollDirection;
    /**
     * @deprecated
     */
    readonly defaultScrollSpeed = 1000;
    /**
     * @deprecated
     * @param element The HTML element used for scroll inputs.
     */
    constructor(element: HTMLElement);
    /**
     * @deprecated
     * @param inputContext The input context.
     * @param inputAction The input action.
     * @param callback The callback that will be called when the input is triggered.
     * @returns The input watcher ID.
     */
    addCustomEvent(inputContext: string, inputAction: string, callback: (value: boolean) => void): string;
    /**
     * @deprecated
     * @param eventId The event ID.
     */
    removeCustonEvent(eventId: string): void;
    /**
     * @deprecated
     * Allows an HTML element to be scrolled through using a gamepad.
     * @param direction The direction where it is allowed to scroll. All directions are allowed by default.
     * @param scrollSpeed The scroll speed in px/s. 1000 by default.
     */
    enableScroll(direction?: ScrollDirection, scrollSpeed?: number): void;
    /**
     * @deprecated
     * Prevents an HTML element to be scrolled through using a gamepad.
     */
    disableScroll(): void;
}
