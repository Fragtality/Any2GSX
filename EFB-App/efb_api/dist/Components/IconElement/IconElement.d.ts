import { DisplayComponent, Subscription, VNode } from '@microsoft/msfs-sdk';
import { ClassProp, MaybeSubscribable } from '../../types';

interface IconElementProps {
    /** Icon url */
    url: MaybeSubscribable<string>;
    /** Additionnal custom class */
    class?: ClassProp;
}
export declare class IconElement extends DisplayComponent<IconElementProps> {
    protected readonly url: import('@microsoft/msfs-sdk').Subscribable<string>;
    protected el: import('@microsoft/msfs-sdk').NodeReference<HTMLDivElement>;
    protected urlSub?: Subscription;
    private readonly onSVGIconLoaded;
    render(): VNode;
    onAfterRender(node: VNode): void;
    destroy(): void;
}
export {};
