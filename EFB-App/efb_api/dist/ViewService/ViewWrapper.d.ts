import { ComponentProps, DisplayComponent, Subscribable, VNode } from '@microsoft/msfs-sdk';

interface ViewWrapperProps extends ComponentProps {
    isActive: Subscribable<boolean>;
    viewName: string;
}
export declare class ViewWrapper extends DisplayComponent<ViewWrapperProps> {
    private readonly rootRef;
    private readonly classList;
    private readonly subs;
    onAfterRender(node: VNode): void;
    render(): VNode;
    destroy(): void;
}
export {};
