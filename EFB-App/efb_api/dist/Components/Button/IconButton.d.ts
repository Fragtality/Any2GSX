import { DisplayComponent, VNode } from '@microsoft/msfs-sdk';
import { MaybeSubscribable } from '../../types';
import { ButtonProps } from './Button';

/** The properties of the IconButton component */
export interface IconButtonProps extends ButtonProps {
    /** The button icon path. */
    iconPath: MaybeSubscribable<string>;
    /** @deprecated Unused */
    type?: 'primary' | 'secondary';
}
/** A button component that displays an icon. */
export declare class IconButton extends DisplayComponent<IconButtonProps> {
    render(): VNode;
}
