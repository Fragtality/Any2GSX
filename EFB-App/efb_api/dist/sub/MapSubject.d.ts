import { MappedSubscribable, MutableSubscribable, Subscription } from '@microsoft/msfs-sdk';

export declare enum SubscribableMapEventType {
    Added = 0,
    Updated = 1,
    Removed = 2
}
export type MapSubjectHandler<K, V> = (key: K, type: SubscribableMapEventType, item: undefined | Readonly<V>, previousItem: undefined | Readonly<V>) => void;
export interface MapSubscribable<K, V> {
    readonly isSubscribable: true;
    readonly isMapSubscribable: true;
    readonly length: number;
    get(key: K): undefined | V;
    set(key: K, value: Readonly<V>): void;
    has(key: K): boolean;
    delete(key: K): boolean;
    sub(handler: MapSubjectHandler<K, V>, initialNotify?: boolean, paused?: boolean): Subscription;
    unsub(handler: MapSubjectHandler<K, V>): void;
}
export declare class MapSubject<K, V> implements MapSubscribable<K, V> {
    readonly isSubscribable = true;
    readonly isMapSubscribable = true;
    private readonly obj;
    private subs;
    private notifyDepth;
    private readonly initialNotifyFunc;
    private readonly onSubDestroyedFunc;
    get length(): number;
    private constructor();
    static create<K, V>(entries?: ReadonlyArray<readonly [K, V]> | null): MapSubject<K, V>;
    get(key: K): undefined | V;
    set(key: K, value: Readonly<V>): void;
    has(key: K): boolean;
    delete(key: K): boolean;
    sub(handler: MapSubjectHandler<K, V>, initialNotify?: boolean, paused?: boolean): Subscription;
    unsub(handler: MapSubjectHandler<K, V>): void;
    map<M>(fn: (input: V, previousVal?: M | undefined) => M, equalityFunc?: ((a: M, b: M) => boolean) | undefined): MappedSubscribable<M>;
    map<M>(fn: (input: V, previousVal?: M | undefined) => M, equalityFunc: (a: M, b: M) => boolean, mutateFunc: (oldVal: M, newVal: M) => void, initialVal: M): MappedSubscribable<M>;
    pipe(to: MutableSubscribable<any, V>, paused?: boolean | undefined): Subscription;
    pipe<M>(to: MutableSubscribable<any, M>, map: (fromVal: V, toVal: M) => M, paused?: boolean | undefined): Subscription;
    private notify;
    private initialNotify;
    private onSubDestroyed;
}
