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
export declare function random<T>(collection: readonly T[]): undefined | T;
//# sourceMappingURL=Array.d.ts.map