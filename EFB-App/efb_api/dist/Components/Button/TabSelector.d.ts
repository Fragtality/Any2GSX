import { Subscribable, VNode } from '@microsoft/msfs-sdk';
import { GamepadUiComponent, GamepadUiComponentProps } from '../../Gamepad';
import { TTProps } from '../../i18n';

interface TabSelectorProps extends GamepadUiComponentProps {
    tabName: TTProps['key'];
    active: Subscribable<boolean>;
    hidden: Subscribable<boolean>;
    callback?: () => void;
}
export declare class TabSelector extends GamepadUiComponent<HTMLDivElement, TabSelectorProps> {
    render(): VNode;
    onAfterRender(node: VNode): void;
}
export {};
