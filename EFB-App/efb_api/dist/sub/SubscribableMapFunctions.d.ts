/**
 * Generates a function which maps an input to a boolean where input is strictly equal to value.
 * @param value Value you want to compare your input with
 * @returns A function which maps an input to a boolean where input is strictly equal to value.
 */
export declare function where<T>(value: T): (input: T, currentVal?: boolean) => boolean;
/**
 * Generates a function which maps an input number to its string value.
 * @returns A function which maps an input number to its string value.
 */
export declare function toString<T extends number>(): (input: T, currentVal?: string) => string;
