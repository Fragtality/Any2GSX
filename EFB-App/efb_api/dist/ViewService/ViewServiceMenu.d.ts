import { NodeReference, VNode } from '@microsoft/msfs-sdk';
import { TabSelector } from '../Components';
import { GamepadUiComponent, GamepadUiComponentProps } from '../Gamepad';
import { UiView } from '../UiView';
import { ClassProp } from '../types';
import { PublicViewEntry, ViewService } from './ViewService';

interface ViewServiceMenuProps extends GamepadUiComponentProps {
    viewService: ViewService;
    class?: ClassProp;
}
interface ViewServiceMenuTab {
    key: string;
    tabRef: NodeReference<TabSelector>;
    view: Readonly<PublicViewEntry<UiView>>;
}
export declare class ViewServiceMenu extends GamepadUiComponent<HTMLDivElement, ViewServiceMenuProps> {
    private viewsSub?;
    protected tabs: ViewServiceMenuTab[];
    private tabRender;
    render(): VNode;
    onAfterRender(node: VNode): void;
    destroy(): void;
}
export {};
