/**
 * Define the behavior of the view when registered.
 */
export declare enum ViewBootMode {
    /** No JSX created until view is shown. */
    COLD = 0,
    /** JSX is mounted even if view is not shown */
    HOT = 1
}
/**
 * Define the behavior of the view when it is closed.
 */
export declare enum ViewSuspendMode {
    /** Closed without being destroyed. */
    SLEEP = 0,
    /** Closed then destroyed. */
    TERMINATE = 1
}
