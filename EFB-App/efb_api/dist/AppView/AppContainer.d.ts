import { ComponentProps, DisplayComponent, VNode } from '@microsoft/msfs-sdk';
import { AppViewService } from './AppViewService';

/** A Container for an App viewstack */
export interface AppViewContainer {
    /**
     * Renders a view into this container.
     * @param view An AppView as a node
     */
    renderView(view: VNode): void;
}
export interface AppContainerProps extends ComponentProps {
    /** The AppViewService instance */
    appViewService: AppViewService;
}
export declare class AppContainer extends DisplayComponent<AppContainerProps> {
    private readonly appMainRef;
    private readonly appStackRef;
    onAfterRender(node: VNode): void;
    render(): VNode | null;
}
