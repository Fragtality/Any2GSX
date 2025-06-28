import { HandlerSubscription } from '@microsoft/msfs-sdk';
export var SubscribableMapEventType;
(function (SubscribableMapEventType) {
    SubscribableMapEventType[SubscribableMapEventType["Added"] = 0] = "Added";
    SubscribableMapEventType[SubscribableMapEventType["Updated"] = 1] = "Updated";
    SubscribableMapEventType[SubscribableMapEventType["Removed"] = 2] = "Removed";
})(SubscribableMapEventType || (SubscribableMapEventType = {}));
export class MapSubject {
    get length() {
        return this.obj.size;
    }
    constructor(entries) {
        this.isSubscribable = true;
        this.isMapSubscribable = true;
        this.obj = new Map();
        this.subs = [];
        this.notifyDepth = 0;
        this.initialNotifyFunc = this.initialNotify.bind(this);
        this.onSubDestroyedFunc = this.onSubDestroyed.bind(this);
        if (entries) {
            for (const [k, v] of entries) {
                this.obj.set(k, v);
            }
        }
    }
    static create(entries) {
        return new MapSubject(entries);
    }
    get(key) {
        return this.obj.get(key);
    }
    set(key, value) {
        const oldValue = this.obj.get(key);
        const exists = this.has(key);
        // console.log('MapSubject.set()', exists, key, value);
        this.obj.set(key, value);
        this.notify(key, exists ? SubscribableMapEventType.Updated : SubscribableMapEventType.Added, value, oldValue);
    }
    has(key) {
        return this.obj.has(key);
    }
    delete(key) {
        const deleted = this.obj.delete(key);
        if (deleted) {
            this.notify(key, SubscribableMapEventType.Removed);
        }
        return deleted;
    }
    sub(handler, initialNotify = false, paused = false) {
        const sub = new HandlerSubscription(handler, this.initialNotifyFunc, this.onSubDestroyedFunc);
        this.subs.push(sub);
        if (paused) {
            sub.pause();
        }
        else if (initialNotify) {
            sub.initialNotify();
        }
        return sub;
    }
    unsub(handler) {
        const toDestroy = this.subs.find((sub) => sub.handler === handler);
        toDestroy === null || toDestroy === void 0 ? void 0 : toDestroy.destroy();
    }
    map(_fn, _equalityFunc, _mutateFunc, _initialVal) {
        throw new Error('Method not implemented.');
    }
    pipe(_to, _map, _paused) {
        throw new Error('Method not implemented.');
    }
    notify(key, type, modifiedItem, previousValue) {
        let needCleanUpSubs = false;
        this.notifyDepth++;
        for (const sub of this.subs) {
            try {
                if (sub.isAlive && !sub.isPaused) {
                    sub.handler(key, type, modifiedItem, previousValue);
                }
                needCleanUpSubs || (needCleanUpSubs = !sub.isAlive);
            }
            catch (error) {
                console.error(`MapSubject: error in handler: ${error}`);
            }
        }
        this.notifyDepth--;
        if (needCleanUpSubs && this.notifyDepth === 0) {
            this.subs = this.subs.filter((sub) => sub.isAlive);
        }
    }
    initialNotify(sub) {
        for (const key of this.obj.keys()) {
            const v = this.obj.get(key);
            try {
                sub.handler(key, SubscribableMapEventType.Added, v, undefined);
            }
            catch (error) {
                console.error(`MapSubject: error in handker: ${error}`);
            }
        }
    }
    onSubDestroyed(sub) {
        if (this.notifyDepth === 0) {
            this.subs.splice(this.subs.indexOf(sub), 1);
        }
    }
}
