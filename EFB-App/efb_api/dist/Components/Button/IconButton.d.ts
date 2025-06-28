import { DisplayComponent, VNode } from '@microsoft/msfs-sdk';
import { MaybeSubscribable } from '../../types';
import { ButtonProps } from './Button';

export interface IconButtonProps extends ButtonProps {
    /** an optionnal SVG icon. The icon path starting from the component that calls SquareButton */
    iconPath: MaybeSubscribable<string>;
    /** @deprecated Unused */
    type?: 'primary' | 'secondary';
}
export declare class IconButton extends DisplayComponent<IconButtonProps> {
    private iconRendering;
    render(): VNode;
}
//# sourceMappingURL=IconButton.d.ts.map