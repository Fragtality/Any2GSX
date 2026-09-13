import { ComponentProps, DisplayComponent, VNode } from '@microsoft/msfs-sdk';
import { ClassProp } from '../types';
import { ViewContainer } from './ViewService';

interface ViewStackContainerProps extends ComponentProps {
    class?: ClassProp;
    id?: string;
}
export declare class ViewStackContainer extends DisplayComponent<ViewStackContainerProps> implements ViewContainer {
    private readonly rootRef;
    renderView(view: VNode): void;
    render(): VNode;
}
export {};
