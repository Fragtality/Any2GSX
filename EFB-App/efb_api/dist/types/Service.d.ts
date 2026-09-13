export interface Service {
    /**
     * Called once when the view is opened for the first time.
     */
    onOpen?(): void;
    /**
     * Called once when the view is destroyed.
     */
    onClose?(): void;
    /**
     * Called each time the view is resumed.
     */
    onResume?(): void;
    /**
     * Called each time the view is closed.
     */
    onPause?(): void;
    /**
     * On Update loop
     * @param time in milliseconds
     */
    onUpdate?(time: number): void;
    /**
     * Called once when the view is removed from DOM.
     */
    destroy?(): void;
}
