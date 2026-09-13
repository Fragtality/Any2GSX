import { VNode } from '@microsoft/msfs-sdk';
import { MaybeSubscribable } from '../../types';
import { AbstractAccordion, AbstractAccordionProps } from './AbstractAccordion';

interface StringAccordionProps extends AbstractAccordionProps {
    /** Can be a string or a TT key */
    title: MaybeSubscribable<string>;
}
export declare class StringAccordion extends AbstractAccordion<StringAccordionProps> {
    protected renderHeader(): VNode;
}
export {};
