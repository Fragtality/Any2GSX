/**
 * @param collection array to filter
 * @returns unique array
 */
export declare function unique<T>(collection: T[]): T[];
/**
 * @param collection array to group
 * @param iteratee function or key of array
 * @returns grouped array
 */
export declare function groupBy<T, RetType extends keyof T | PropertyKey, Func extends (value: T) => RetType = (value: T) => RetType>(collection: readonly T[], iteratee: RetType | Func): Record<RetType, T[]>;
/**
 * Returns a random element or undefined if array is empty
 * @param collection Array
 * @returns Returns a random element or undefined
 */
export declare function random<T>(collection: readonly T[]): undefined | T;
/**
 * Creates an array of elements split into groups of `size`
 * @param collection Array to chunk
 * @param size The length of each chunk
 * @returns Returns the new array of chunks
 */
export declare function chunk<T>(collection: readonly T[], size: number): T[][];
declare global {
    interface ArrayConstructor {
        isArray(arg: readonly any[] | any): arg is readonly any[];
    }
}
/**
 * Returns the argument if already an array or create an array of argument
 * @param collection Array of items or item
 * @returns Returns an array of items
 */
export declare function wrap<T>(collection: T | readonly T[]): readonly T[];
