/**
 * Define the behavior when the app is booted
 */
export declare enum AppBootMode {
    /** No JSX created until app is launched. */
    COLD = 0,
    /** App JSX is created but not mounted in EFB. */
    WARM = 1,
    /** All JSX is created and mounted. */
    HOT = 2
}
/**
 * Define the behavior when the app is leaved
 */
export declare enum AppSuspendMode {
    /** When leaving app, it goes to sleep. */
    SLEEP = 0,
    /** When leaving app, the whole app is destroyed. */
    TERMINATE = 1
}
