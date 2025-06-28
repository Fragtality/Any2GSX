/**
 * Check wether a given value is a Promise or not.
 * @param value The value you wan't to check.
 * @returns True if value is a promise.
 */
export function isPromise(value) {
    return value instanceof Promise;
}
/**
 * Convert value to a Promise if it isn't already one.
 * @param value The value you wan't to convert.
 * @returns A promise of value.
 */
export function toPromise(value) {
    return isPromise(value) ? value : Promise.resolve(value);
}
