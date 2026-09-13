import { ArraySubject, EventBus, Subject, Subscribable, SubscribableArray, SubscribableArrayEventType, Subscription } from '@microsoft/msfs-sdk';
import { EfbNotification, EfbPermanentNotification } from '../../types';

export declare class NotificationManager {
    private readonly bus;
    private static INSTANCE;
    /** If false, the notification manager will be paused */
    private readonly allowNotification;
    /** True if the notification app, with all the permanent notifications is open */
    private isNotificationAppOpen;
    /** Array of pending notifications. A notification is stored in it until it is shown */
    protected pendingNotifications: EfbNotification[];
    /** Array of shown notifications. A notification is stored in it until it is not shown anymore */
    protected readonly _shownNotifications: ArraySubject<EfbNotification>;
    /** Public array of notifications to show. For the Notification Container */
    readonly shownNotifications: SubscribableArray<EfbNotification>;
    /** Array of stored notifications. A permanent notifications is stored in it until it is deleted from the notifications page */
    protected readonly _storedNotifications: ArraySubject<EfbPermanentNotification>;
    /** Public array of stored notifications. For the Notification Page */
    readonly storedNotifications: SubscribableArray<EfbPermanentNotification>;
    /** Total count of unseen permanent notifications */
    protected readonly _unseenNotificationsCount: Subject<number>;
    /** Total count of unseen permanent notifications */
    readonly unseenNotificationsCount: Subscribable<number>;
    /** Number of notifications shown at the same time. Will determine the maximum length of the shownNotifications array */
    protected readonly maxShownItems: Subject<number>;
    /** Delay between notifications */
    protected readonly timeBetweenNotifsMs = 500;
    protected subs: Subscription[];
    constructor(bus: EventBus);
    static getManager(bus: EventBus): NotificationManager;
    /**
     * internaly called by efb os
     * @internal
     */
    update(): void;
    protected onShownNotifsUpdate(_index: number, eventType: SubscribableArrayEventType, notif?: EfbNotification | readonly EfbNotification[]): void;
    protected onStoredNotifsUpdate(_i: number, _t: SubscribableArrayEventType, _n?: EfbPermanentNotification | readonly EfbPermanentNotification[], arr?: readonly EfbPermanentNotification[]): void;
    /**
     * Adds a notification to the stack
     * @param notif the notification to add, could be a TemporaryNotification or a PermanentNotification
     */
    addNotification(notif: EfbNotification): void;
    /**
     * Removes a permanent notification from the notification menu
     * @param notif the notification to remove
     */
    deletePermanentNotification(notif: EfbPermanentNotification): void;
    clearNotifications(): void;
    onNotificationAppOpen(): void;
    onNotificationAppClosed(): void;
}
