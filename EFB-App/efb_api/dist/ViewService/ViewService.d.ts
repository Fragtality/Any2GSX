import { ComponentProps, DisplayComponent, NodeReference, Subject, VNode } from '@microsoft/msfs-sdk';
import { AppViewService } from '../AppView';
import { InputActionDestructor } from '../Listeners';
import { GamepadUiView, UiView, UiViewProps } from '../UiView';
import { MapSubscribable } from '../sub';
import { ClassProp, Service, TVNode } from '../types';
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
export declare class ViewService implements Service {
    private readonly registeredViews;
    private readonly viewTabs;
    private visibleViewTabs;
    private readonly viewTabVisibilitySubscriptions;
    private viewRef?;
    private hasInitialized;
    private activeViewEntry;
    constructor(_viewKey?: string, _appViewService?: AppViewService);
    private readonly internalInputStackManager;
    protected prevTabActionDestructor?: InputActionDestructor;
    protected nextTabActionDestructor?: InputActionDestructor;
    protected openNextVisibleTab(forward?: boolean): boolean;
    getRegisteredViews<Ref extends ViewRef = ViewRef>(): Readonly<MapSubscribable<string, PublicViewEntry<Ref>>>;
    private getViewEntry;
    registerView(key: string, vNodeFactory: () => ViewEntryNode): PublicViewEntry<null>;
    onContainerRendered(viewRef: ViewContainer): void;
    private initViewEntry;
    initialize(key?: string): void;
    openPage<Ref extends ViewRef = ViewRef>(key: string): PublicViewEntry<Ref>;
    onPause(): void;
    onResume(): void;
    onUpdate(time: number): void;
    destroy(): void;
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
