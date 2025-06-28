export type ScrollDirection = 'vertical' | 'y' | 'horizontal' | 'x' | 'all';
export declare class GamepadInputManager {
    private readonly element;
    readonly defaultScrollDirection: ScrollDirection;
    readonly defaultScrollSpeed = 1000;
    private isScrollEnabled;
    private readonly scrollInputs;
    private readonly scrollIntervalDelayMs;
    private pxPerScroll;
    private scrollDirection;
    constructor(element: HTMLElement);
    addCustomEvent(inputContext: string, inputAction: string, callback: (value: boolean) => void): string;
    removeCustonEvent(eventId: string): void;
    /**
     * Allows an HTML element to be scrolled through using a gamepad.
     * @param direction The direction where it is allowed to scroll. All directions are allowed by default.
     * @param scrollSpeed The scroll speed in px/s. 1000 by default.
     */
    enableScroll(direction?: ScrollDirection, scrollSpeed?: number): void;
    /**
     * Prevents an HTML element to be scrolled through using a gamepad.
     */
    disableScroll(): void;
    private handleScroll;
    private setupVerticalScroll;
    private setupHorizontalScroll;
    private initScrollInputs;
    private mouseEnterScrollCallback;
    private clearScrollInputs;
    private mouseLeaveScrollCallback;
}
//# sourceMappingURL=GamepadInputManager.d.ts.map