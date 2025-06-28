import { VNode } from '@microsoft/msfs-sdk';
import { EfbNotification, EfbNotificationStyle, EfbPermanentNotification, EfbTemporaryNotification, MaybeSubscribable } from '../types';

/**
 * Check if a notification is permanent or not
 * @param notif the notification to check
 * @returns an EfbPermanentNotification if possible
 */
export declare function isNotifPermanent(notif: EfbNotification): notif is EfbPermanentNotification;
/**
 * Utility class for creating a temporary EFB notification
 * @param delayMs notification display time in milliseconds
 * @param description text rendered in the notification
 * @param style style of the notification
 * @param descriptionArguments arguments of the description text
 * @param icon icon rendered in the notification
 * @returns a temporary EFB notification
 */
export declare function createTemporaryNotif(delayMs: number, description: string, style?: EfbNotificationStyle, descriptionArguments?: Map<string, MaybeSubscribable<string>>, icon?: string | VNode): EfbTemporaryNotification;
/**
 * Utility class for creating a permanent EFB notification
 * @param delayMs notification display time in milliseconds
 * @param title title rendered in the notification
 * @param description text rendered in the notification
 * @param style style of the notification
 * @param descriptionArguments arguments of the description text
 * @param icon icon rendered in the notification
 * @param color optional color on the left side of the notification
 * @param action optional action rendered in the notification
 * @returns a permanent EFB notification
 */
export declare function createPermanentNotif(delayMs: number, title: string, description: string, style?: EfbNotificationStyle, descriptionArguments?: Map<string, MaybeSubscribable<string>>, icon?: string | VNode, color?: string | number, action?: () => VNode): EfbPermanentNotification;
//# sourceMappingURL=NotificationUtils.d.ts.map