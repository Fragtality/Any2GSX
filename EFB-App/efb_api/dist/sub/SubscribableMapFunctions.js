/**
 * Generates a function which maps an input to a boolean where input is strictly equal to value.
 * @param value Value you want to compare your input with
 * @returns A function which maps an input to a boolean where input is strictly equal to value.
 */
export function where(value) {
    return (input) => value === input;
}
/**
 * Generates a function which maps an input number to its string value.
 * @returns A function which maps an input number to its string value.
 */
export function toString() {
    return (input) => input.toString();
}
