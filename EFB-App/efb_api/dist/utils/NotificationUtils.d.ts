import { VNode } from '@microsoft/msfs-sdk';
import { EfbNotification, EfbNotificationStyle, EfbPermanentNotification, EfbTemporaryNotification, MaybeSubscribable } from '../types';

/**
 * Check if a notification is permanent or not
 * @param notif the notification to check
 * @returns an EfbPermanentNotification if possible
 */
export declare function isNotificationPermanent(notif: EfbNotification): notif is EfbPermanentNotification;
/**
 * @deprecated
 * Check if a notification is permanent or not
 * @param notif the notification to check
 * @returns an EfbPermanentNotification if possible
 */
export declare function isNotifPermanent(notif: EfbNotification): notif is EfbPermanentNotification;
/**
 * Utility class for creating a temporary EFB notification
 * @param description text rendered in the notification
 * @param displayTimeMs notification display time in milliseconds, default to 3000
 * @param style style of the notification
 * @param descriptionArguments arguments of the description text
 * @param icon icon rendered in the notification
 * @returns a temporary EFB notification
 */
export declare function createTemporaryNotification(description: string, displayTimeMs?: number, style?: EfbNotificationStyle, descriptionArguments?: Map<string, MaybeSubscribable<string>>, icon?: string | VNode): EfbTemporaryNotification;
/**
 * @deprecated
 * Utility class for creating a temporary EFB notification
 * @param displayTimeMs notification display time in milliseconds
 * @param description text rendered in the notification
 * @param style style of the notification
 * @param descriptionArguments arguments of the description text
 * @param icon icon rendered in the notification
 * @returns a temporary EFB notification
 */
export declare function createTemporaryNotif(displayTimeMs: number, description: string, style?: EfbNotificationStyle, descriptionArguments?: Map<string, MaybeSubscribable<string>>, icon?: string | VNode): EfbTemporaryNotification;
/**
 * Utility class for creating a permanent EFB notification
 * @param title title rendered in the notification
 * @param description text rendered in the notification
 * @param displayTimeMs notification display time in milliseconds, default to 3000
 * @param style style of the notification
 * @param descriptionArguments arguments of the description text
 * @param icon icon rendered in the notification
 * @param color optional color on the left side of the notification
 * @param action optional action rendered in the notification
 * @returns a permanent EFB notification
 */
export declare function createPermanentNotification(title: string, description: string, displayTimeMs?: number, style?: EfbNotificationStyle, descriptionArguments?: Map<string, MaybeSubscribable<string>>, icon?: string | VNode, color?: string | number, action?: () => VNode): EfbPermanentNotification;
/**
 * @deprecated
 * Utility class for creating a permanent EFB notification
 * @param displayTimeMs notification display time in milliseconds
 * @param title title rendered in the notification
 * @param description text rendered in the notification
 * @param style style of the notification
 * @param descriptionArguments arguments of the description text
 * @param icon icon rendered in the notification
 * @param color optional color on the left side of the notification
 * @param action optional action rendered in the notification
 * @returns a permanent EFB notification
 */
export declare function createPermanentNotif(displayTimeMs: number, title: string, description: string, style?: EfbNotificationStyle, descriptionArguments?: Map<string, MaybeSubscribable<string>>, icon?: string | VNode, color?: string | number, action?: () => VNode): EfbPermanentNotification;
