import { VNode } from '@microsoft/msfs-sdk';
import { GamepadUiComponent, GamepadUiComponentProps } from '../../Gamepad';

interface TagProps extends GamepadUiComponentProps {
    title: string;
    iconPath?: string;
    onButtonClick?: () => void;
}
export declare class Tag extends GamepadUiComponent<HTMLDivElement, TagProps> {
    protected readonly closeButtonRef: import('@microsoft/msfs-sdk').NodeReference<HTMLDivElement>;
    onAfterRender(node: VNode): void;
    destroy(): void;
    render(): VNode;
}
export {};
//# sourceMappingURL=Tag.d.ts.map