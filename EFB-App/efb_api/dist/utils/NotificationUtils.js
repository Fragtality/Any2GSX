import { Subject, UUID } from '@microsoft/msfs-sdk';
/**
 * Check if a notification is permanent or not
 * @param notif the notification to check
 * @returns an EfbPermanentNotification if possible
 */
export function isNotifPermanent(notif) {
    return notif.type === 'permanent';
}
/**
 * Utility class for creating a temporary EFB notification
 * @param delayMs notification display time in milliseconds
 * @param description text rendered in the notification
 * @param style style of the notification
 * @param descriptionArguments arguments of the description text
 * @param icon icon rendered in the notification
 * @returns a temporary EFB notification
 */
export function createTemporaryNotif(delayMs, description, style, descriptionArguments, icon) {
    const notifStyle = style !== null && style !== void 0 ? style : 'info';
    return {
        uuid: UUID.GenerateUuid(),
        type: 'temporary',
        createdAt: new Date(),
        hide: Subject.create(false),
        delayMs: Utils.Clamp(delayMs, 0, 60000),
        description,
        style: notifStyle,
        descriptionArguments,
        icon: icon !== null && icon !== void 0 ? icon : getNotifIconFromStyle(notifStyle),
    };
}
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
export function createPermanentNotif(delayMs, title, description, style, descriptionArguments, icon, color, action) {
    const notifStyle = style !== null && style !== void 0 ? style : 'info';
    return {
        uuid: UUID.GenerateUuid(),
        type: 'permanent',
        createdAt: new Date(),
        hide: Subject.create(false),
        delayMs: Utils.Clamp(delayMs, 0, 60000),
        description,
        style: notifStyle,
        descriptionArguments,
        icon: icon !== null && icon !== void 0 ? icon : getNotifIconFromStyle(notifStyle),
        title,
        color,
        action,
        viewed: Subject.create(false),
    };
}
/**
 * Utility function to retrieve the default icon of a notification according to its style attribute
 * @param style the style attribute of an efb notification
 * @returns the icon url related to the style
 */
function getNotifIconFromStyle(style) {
    switch (style) {
        case 'warning':
            return 'coui://html_ui/efb_ui/efb_os/Assets/icons/NoMargin/Warning.svg';
        case 'success':
            return 'coui://html_ui/efb_ui/efb_os/Assets/icons/NoMargin/Check_Full.svg';
        case 'error':
            return 'coui://html_ui/efb_ui/efb_os/Assets/icons/NoMargin/Failure_Full.svg';
        default:
            return 'coui://html_ui/efb_ui/efb_os/Assets/icons/NoMargin/Info_Full.svg';
    }
}
