/**
 * Get value by calling the `arg` if it is a function or by returning the `arg`.
 */
export function value(arg) {
    return typeof arg === 'function' ? arg() : arg;
}
