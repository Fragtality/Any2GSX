import { VNode } from '@microsoft/msfs-sdk';
import { AbstractButton, AbstractButtonProps } from './Button';

/** @deprecated */
type ButtonType = 'primary' | 'secondary';
/** @deprecated */
export interface TypedButtonProps extends AbstractButtonProps {
    type?: ButtonType;
}
/** @deprecated Deprecated component */
export declare abstract class TypedButton<T extends TypedButtonProps = TypedButtonProps> extends AbstractButton<T> {
    onAfterRender(node: VNode): void;
    protected abstract buttonRender(): VNode;
}
export {};
