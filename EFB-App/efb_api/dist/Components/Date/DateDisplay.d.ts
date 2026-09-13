import { ComponentProps, DisplayComponent, VNode } from '@microsoft/msfs-sdk';
import { ClassProp, MaybeSubscribable, StyleProp } from '../../types';

type DateType = number | Date;
export interface DateDisplayProps extends ComponentProps {
    /** the date as a Date object or as UNIX timestamp in millesconds */
    date: MaybeSubscribable<DateType>;
    /** The format the date will be formatted to (e.g. 'DD MM YYYY hh:mm:ss') */
    format: MaybeSubscribable<string>;
    /** Additionnal custom class */
    class?: ClassProp;
    /** Additionnal custom style */
    style?: StyleProp;
}
export declare class DateDisplay extends DisplayComponent<DateDisplayProps> {
    private readonly ready;
    private readonly date;
    private readonly format;
    private readonly dateSubscription;
    private readonly formatSubscription;
    private readonly dateSpanRef;
    private dateNodes;
    private readonly cachedSubject;
    private renderFormat;
    private updateDate;
    onAfterRender(node: VNode): void;
    render(): VNode;
    destroy(): void;
}
export {};
