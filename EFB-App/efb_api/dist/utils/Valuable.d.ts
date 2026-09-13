/** Mark as Valuable a type (ie: you can have elements that are string or a function returning a string) */
export type Valuable<T> = T extends unknown ? T | (() => T) : T;
/**
 * Get value by calling the `arg` if it is a function or by returning the `arg`.
 */
export declare function value<T>(arg: Valuable<T>): T;
