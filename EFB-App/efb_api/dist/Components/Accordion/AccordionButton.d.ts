import { DisplayComponent, Subscribable, VNode } from '@microsoft/msfs-sdk';
import { ButtonProps } from '../Button';

interface AccordionButtonProps extends ButtonProps {
    isFolded: Subscribable<boolean>;
}
/** @internal */
export declare class AccordionButton extends DisplayComponent<AccordionButtonProps> {
    private readonly iconStyle;
    private readonly isFoldedSubscription;
    render(): VNode;
    destroy(): void;
}
export {};
