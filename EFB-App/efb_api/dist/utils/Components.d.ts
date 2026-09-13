import { ToggleableClassNameRecord, VNode } from '@microsoft/msfs-sdk';
import { IApp } from '../App';
import { ClassProp } from '../types';

export declare function isVNode(object: object): object is VNode;
export declare function isIApp(object: object): object is IApp;
export declare function isConstructor(func: object): boolean;
/**
 * Check if argument `fn` is a function.
 * @template T Expected type of `fn`.
 * @param value
 */
export declare function isFunction<T = unknown>(fn: unknown): fn is T;
export declare function toArray(list: unknown[], start?: number): unknown[];
/**
 * @param classProp Convert {string} to {ToggleableClassNameRecord}
 * @returns Converted ClassProp
 */
export declare function toClassProp(classProp: ClassProp): undefined | ToggleableClassNameRecord;
/**
 *
 * @param baseProp Base props to merge
 * @param args Array of ClassProp to merge onto baseProp
 * @returns Merged ClassProps
 */
export declare function mergeClassProp(baseProp: ClassProp, ...args: ClassProp[]): ToggleableClassNameRecord;
