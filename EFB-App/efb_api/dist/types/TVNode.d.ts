import { NodeInstance, VNode } from '@microsoft/msfs-sdk';

/**
 * Templated virtual DOM node.
 * @typeParam T Node instance
 * @typeParam P Node Props
 */
export interface TVNode<T = NodeInstance, P = unknown> extends VNode {
    instance: T & NodeInstance;
    props: P;
}
