import { Promiseable } from '../types';

/**
 * Check wether a given value is a Promise or not.
 * @param value The value you wan't to check.
 * @returns True if value is a promise.
 */
export declare function isPromise<T>(value: unknown): value is Promise<T>;
/**
 * Convert value to a Promise if it isn't already one.
 * @param value The value you wan't to convert.
 * @returns A promise of value.
 */
export declare function toPromise<T>(value: Promiseable<T>): Promise<T>;
