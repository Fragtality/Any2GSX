import { DisplayComponent, VNode } from '@microsoft/msfs-sdk';
import { GamepadUiComponentProps } from '../../Gamepad';
import { TTProps } from '../../i18n';
import { Button, ButtonProps } from './Button';

export type TTButtonProps = ButtonProps & TTProps & GamepadUiComponentProps;
export declare class TTButton<P extends TTButtonProps = TTButtonProps> extends DisplayComponent<P> {
    render(): VNode;
}
/**
 * @deprecated ClassicButton is the old version of TTButton. Use TTButton instead.
 */
export interface ClassicButtonProps extends ButtonProps, Omit<TTProps, 'key' | 'type'> {
    /** the classic button title */
    title: TTProps['key'];
    /** @deprecated Unused */
    type?: 'primary' | 'secondary';
}
/**
 * @deprecated ClassicButton is the old version of TTButton. Use TTButton instead.
 */
export declare class ClassicButton extends Button<ClassicButtonProps> {
    protected buttonRender(): VNode;
}
