import { ArraySubject, Subject, SubscribableArrayEventType, } from '@microsoft/msfs-sdk';
import { isNotifPermanent } from '../../utils';
export class NotificationManager {
    constructor(bus) {
        this.bus = bus;
        /** If false, the notification manager will be paused */
        this.allowNotification = true;
        /** True if the notification app, with all the permanent notifications is open */
        this.isNotificationAppOpen = false;
        /** Array of pending notifications. A notification is stored in it until it is shown */
        this.pendingNotifications = [];
        /** Array of shown notifications. A notification is stored in it until it is not shown anymore */
        this._shownNotifications = ArraySubject.create([]);
        /** Public array of notifications to show. For the Notification Container */
        this.shownNotifications = this._shownNotifications;
        /** Array of stored notifications. A permanent notifications is stored in it until it is deleted from the notifications page */
        this._storedNotifications = ArraySubject.create([]);
        /** Public array of stored notifications. For the Notification Page */
        this.storedNotifications = this._storedNotifications;
        /** Total count of unseen permanent notifications */
        this._unseenNotificationsCount = Subject.create(0);
        /** Total count of unseen permanent notifications */
        this.unseenNotificationsCount = this._unseenNotificationsCount;
        /** Number of notifications shown at the same time. Will determine the maximum length of the shownNotifications array */
        this.maxShownItems = Subject.create(1);
        /** Delay between notifications */
        this.timeBetweenNotifsMs = 500;
        this.subs = [];
        this.subs.push(this._shownNotifications.sub(this.onShownNotifsUpdate.bind(this)), this._storedNotifications.sub(this.onStoredNotifsUpdate.bind(this)));
    }
    static getManager(bus) {
        var _a;
        return ((_a = NotificationManager.INSTANCE) !== null && _a !== void 0 ? _a : (NotificationManager.INSTANCE = new NotificationManager(bus)));
    }
    /**
     * internaly called by efb os
     * @internal
     */
    update() {
        // If the notification is in pause, we do nothing in the update
        if (!this.allowNotification) {
            return;
        }
        // We don't have to process notifications if the shownNotifications array is full or the pending notifications array is empty
        if (this._shownNotifications.length >= this.maxShownItems.get() || this.pendingNotifications.length === 0) {
            return;
        }
        const notifToShow = this.pendingNotifications.shift();
        if (!notifToShow) {
            return;
        }
        // console.log(`Show notif ${notifToShow.description}, delay : ${notifToShow.delayMs}`);
        this._shownNotifications.insert(notifToShow);
    }
    onShownNotifsUpdate(_index, eventType, notif) {
        if (eventType !== SubscribableArrayEventType.Added || notif === undefined) {
            return;
        }
        const notifs = Array.isArray(notif) ? notif : [notif];
        notifs.forEach((notifToDelete) => {
            const notifDelay = notifToDelete.delayMs;
            // Hiding the notif before it is removed to see the notification getting replaced
            setTimeout(() => {
                notifToDelete.hide.set(true);
            }, notifDelay);
            setTimeout(() => {
                // console.log(`Hide notif ${notifToDelete.description}, delay : ${notifToDelete.delay}`);
                this._shownNotifications.removeItem(notifToDelete);
            }, notifToDelete.delayMs + this.timeBetweenNotifsMs);
        });
    }
    onStoredNotifsUpdate(_i, _t, _n, arr) {
        var _a;
        this._unseenNotificationsCount.set((_a = arr === null || arr === void 0 ? void 0 : arr.reduce((accumulator, notif) => accumulator + (notif.viewed.get() ? 0 : 1), 0)) !== null && _a !== void 0 ? _a : 0);
    }
    addNotification(notif) {
        notif.createdAt = new Date();
        if (isNotifPermanent(notif)) {
            this._storedNotifications.insert(notif);
            if (this.isNotificationAppOpen) {
                return;
            }
        }
        this.pendingNotifications.push(notif);
    }
    deletePermanentNotification(notif) {
        this._storedNotifications.removeItem(notif);
    }
    clearNotifications() {
        this._storedNotifications.clear();
    }
    onNotificationAppOpen() {
        this.isNotificationAppOpen = true;
        this.pendingNotifications = this.pendingNotifications.filter((notif) => !isNotifPermanent(notif));
        this._shownNotifications.getArray().forEach((notif) => {
            if (isNotifPermanent(notif)) {
                this._shownNotifications.removeItem(notif);
            }
        });
    }
    onNotificationAppClosed() {
        this.isNotificationAppOpen = false;
        for (let i = 0; i < this._storedNotifications.length; i++) {
            this._storedNotifications.get(i).viewed.set(true);
        }
        this._unseenNotificationsCount.set(0);
    }
}
