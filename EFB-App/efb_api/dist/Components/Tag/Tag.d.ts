import { VNode } from '@microsoft/msfs-sdk';
import { GamepadUiComponent } from '../../Gamepad';
import { TTProps } from '../../i18n';
import { MaybeSubscribable } from '../../types';
import { IconButtonProps } from '../Button';
import { MarqueeProps } from '../Marquee';

/** The properties of the Tag component. */
interface TagProps extends Omit<IconButtonProps, 'iconPath' | 'type'>, MarqueeProps, Omit<TTProps, 'key'> {
    /** The title that will be displayed in the tag. */
    title: MaybeSubscribable<string>;
    /** The button icon path. No button will be displayed if not specified. */
    iconPath?: MaybeSubscribable<string>;
    /**
     * The function that will be called when the button is clicked.
     * If not provided, the "callback" property will be used instead.
     */
    onButtonClick?: (e: MouseEvent) => void;
}
/** A component that allows text to be displayed in a tag along with a button. */
export declare class Tag extends GamepadUiComponent<HTMLDivElement, TagProps> {
    protected readonly closeButtonRef: import('@microsoft/msfs-sdk').NodeReference<HTMLDivElement>;
    private renderButton;
    render(): VNode;
}
export {};
