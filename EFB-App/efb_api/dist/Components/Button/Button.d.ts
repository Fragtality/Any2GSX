import { MappedSubject, VNode } from '@microsoft/msfs-sdk';
import { GamepadUiComponent, GamepadUiComponentProps } from '../../Gamepad';
import { MaybeSubscribable, Nullable } from '../../types';

export interface ButtonProps extends GamepadUiComponentProps {
    callback?: (e: MouseEvent) => void;
    hoverable?: MaybeSubscribable<boolean>;
    selected?: MaybeSubscribable<boolean>;
    /** @deprecated */
    state?: MaybeSubscribable<boolean>;
}
export declare class Button<T extends ButtonProps = ButtonProps> extends GamepadUiComponent<HTMLButtonElement, T> {
    protected readonly isButtonDisabled: import('@microsoft/msfs-sdk').Subscribable<boolean> | import('@microsoft/msfs-sdk').Subscribable<false> | import('@microsoft/msfs-sdk').Subscribable<true>;
    protected readonly isButtonHoverable: MappedSubject<[boolean, boolean], boolean>;
    protected readonly isButtonSelected: MappedSubject<[boolean, boolean], boolean>;
    /**
     * @deprecated Old way to render the button. Instead of extending the `Button` class, render your content as a children of `<Button>...</Button>`.
     */
    protected buttonRender(): Nullable<VNode>;
    render(): VNode;
    onAfterRender(node: VNode): void;
    destroy(): void;
}
/**
 * @deprecated AbstractButtonProps has been renamed to ButtonProps.
 * eslint-disable-next-line @typescript-eslint/no-empty-interface
 */
export interface AbstractButtonProps extends ButtonProps {
}
/**
 * @deprecated AbstractButton has been renamed to Button
 */
export declare class AbstractButton<T extends AbstractButtonProps = AbstractButtonProps> extends Button<T> {
}
