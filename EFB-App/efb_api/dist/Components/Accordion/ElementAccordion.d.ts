import { VNode } from '@microsoft/msfs-sdk';
import { Valuable } from '../../utils';
import { AbstractAccordion, AbstractAccordionProps } from './AbstractAccordion';

interface ElementAccordionProps extends AbstractAccordionProps {
    header: Valuable<VNode>;
}
export declare class ElementAccordion extends AbstractAccordion<ElementAccordionProps> {
    protected renderHeader(): VNode;
}
export {};
