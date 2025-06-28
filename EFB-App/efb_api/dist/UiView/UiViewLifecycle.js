/**
 * Define the behavior of the view when registered.
 */
export var ViewBootMode;
(function (ViewBootMode) {
    /** No JSX created until view is shown. */
    ViewBootMode[ViewBootMode["COLD"] = 0] = "COLD";
    /** JSX is mounted even if view is not shown */
    ViewBootMode[ViewBootMode["HOT"] = 1] = "HOT";
})(ViewBootMode || (ViewBootMode = {}));
/**
 * Define the behavior of the view when it is closed.
 */
export var ViewSuspendMode;
(function (ViewSuspendMode) {
    /** Closed without being destroyed. */
    ViewSuspendMode[ViewSuspendMode["SLEEP"] = 0] = "SLEEP";
    /** Closed then destroyed. */
    ViewSuspendMode[ViewSuspendMode["TERMINATE"] = 1] = "TERMINATE";
})(ViewSuspendMode || (ViewSuspendMode = {}));
