import { AbstractSubscribableArray, SubscribableArray } from '@microsoft/msfs-sdk';

type ArrayFilterPredicate<T> = (value: T, index: number, array: readonly T[]) => unknown;
/** @internal */
export declare class ArrayFilterSubject<T> extends AbstractSubscribableArray<T> {
    private array;
    private readonly sourceSub;
    get length(): number;
    private constructor();
    static create<AT>(arr: SubscribableArray<AT>, predicate: ArrayFilterPredicate<AT>): ArrayFilterSubject<AT>;
    getArray(): readonly T[];
    destroy(): void;
}
export {};
