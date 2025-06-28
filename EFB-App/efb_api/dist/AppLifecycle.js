/**
 * Define the behavior when the app is booted
 */
export var AppBootMode;
(function (AppBootMode) {
    /** No JSX created until app is launched. */
    AppBootMode[AppBootMode["COLD"] = 0] = "COLD";
    /** App JSX is created but not mounted in EFB. */
    AppBootMode[AppBootMode["WARM"] = 1] = "WARM";
    /** All JSX is created and mounted. */
    AppBootMode[AppBootMode["HOT"] = 2] = "HOT";
})(AppBootMode || (AppBootMode = {}));
/**
 * Define the behavior when the app is leaved
 */
export var AppSuspendMode;
(function (AppSuspendMode) {
    /** When leaving app, it goes to sleep. */
    AppSuspendMode[AppSuspendMode["SLEEP"] = 0] = "SLEEP";
    /** When leaving app, the whole app is destroyed. */
    AppSuspendMode[AppSuspendMode["TERMINATE"] = 1] = "TERMINATE";
})(AppSuspendMode || (AppSuspendMode = {}));
