import { Subject, VNode } from '@microsoft/msfs-sdk';
import { MaybeSubscribable } from './Subscribable';

export type EfbNotificationType = 'temporary' | 'permanent';
export type EfbNotificationStyle = 'info' | 'warning' | 'error' | 'success';
export interface EfbNotification {
    /** Unique id of the notification */
    readonly uuid: string;
    /** Whether the notification is permanent or temporary. Permanent notifications will be added to the notification page */
    readonly type: EfbNotificationType;
    /** Time at which the notification was added to the notification manager */
    createdAt: Date;
    /** Notification display time in milliseconds */
    readonly delayMs: number;
    /** Text rendered in the notification */
    readonly description: string;
    /** Style of the notification. Defines its color and its icon by default */
    readonly style: EfbNotificationStyle;
    /** Arguments of the description text */
    readonly descriptionArguments?: Map<string, MaybeSubscribable<string>>;
    /** Icon rendered in the notification, either an url or a VNode */
    readonly icon?: string | VNode;
    /** The notification will be hidden thanks to this parameter */
    readonly hide: Subject<boolean>;
}
export interface EfbTemporaryNotification extends EfbNotification {
    readonly type: 'temporary';
}
export interface EfbPermanentNotification extends EfbNotification {
    readonly type: 'permanent';
    /** Title rendered in the notification */
    readonly title: string;
    /** Optional color on the left side of the notification. By default, this color is defined by the style of the EfbNotification */
    readonly color?: string | number;
    /** Optional action rendered in the notification */
    readonly action?: () => VNode;
    /** Whether the notification has been viewed in the notification page or not */
    readonly viewed: Subject<boolean>;
}
