import { ComponentProps, DisplayComponent, NodeReference, Subject, VNode } from '@microsoft/msfs-sdk';
import { AppViewService } from '../AppView';
import { GamepadUiView, UiView, UiViewProps } from '../UiView';
import { MapSubscribable } from '../sub';
import { ClassProp, TVNode } from '../types';
import { ViewWrapper } from './ViewWrapper';

export type ViewEntryNode = TVNode<UiView<UiViewProps>, UiViewProps>;
type ViewRef = null | UiView | GamepadUiView<HTMLElement>;
interface ViewEntry<Ref extends ViewRef = ViewRef> {
    readonly key: string;
    readonly render: () => ViewEntryNode;
    vNode: null | ViewEntryNode;
    ref: Ref;
    readonly containerRef: NodeReference<ViewWrapper>;
    readonly isActive: Subject<boolean>;
    readonly isDisabled: Subject<boolean>;
    isInit: boolean;
    readonly isTabVisible: Subject<boolean>;
}
export type PublicViewEntry<Ref extends ViewRef = ViewRef> = Pick<ViewEntry<Ref>, 'key' | 'ref' | 'isActive' | 'isDisabled' | 'isTabVisible'>;
export declare class ViewService {
    private readonly registeredViews;
    private viewRef?;
    private hasInitialized;
    private activeViewEntry;
    constructor(viewKey?: string, appViewService?: AppViewService);
    getRegisteredViews<Ref extends ViewRef = ViewRef>(): Readonly<MapSubscribable<string, PublicViewEntry<Ref>>>;
    private getViewEntry;
    registerView(key: string, vNodeFactory: () => ViewEntryNode): PublicViewEntry<null>;
    onContainerRendered(viewRef: ViewContainer): void;
    private initViewEntry;
    initialize(key?: string): void;
    openPage<Ref extends ViewRef = ViewRef>(key: string): PublicViewEntry<Ref>;
    onUpdate(time: number): void;
}
export interface ViewContainer {
    renderView(view: VNode): void;
}
export interface ViewServiceContainerProps extends ComponentProps {
    viewService: ViewService;
    class?: ClassProp;
    id?: string;
}
export declare class ViewServiceContainer extends DisplayComponent<ViewServiceContainerProps> {
    private readonly stackContainerRef;
    onAfterRender(node: VNode): void;
    render(): VNode;
}
export {};
//# sourceMappingURL=ViewService.d.ts.map