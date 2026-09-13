import { DisplayComponent, VNode } from '@microsoft/msfs-sdk';
import { MaybeSubscribable } from '../types';

type FormatCallable = (text: string) => string;
export interface TTProps {
    key: MaybeSubscribable<string>;
    type?: string;
    format?: FormatCallable | 'upper' | 'uppercase' | 'lower' | 'lowercase' | 'ucfirst' | 'capitalize';
    arguments?: Map<string, MaybeSubscribable<string>>;
}
export declare class TT<P extends TTProps = TTProps> extends DisplayComponent<P> {
    private readonly ref;
    private readonly key;
    private readonly formatter;
    private reloadSubscription?;
    private readonly subs;
    private _reloadText;
    private readonly reloadText;
    private getTextTransformed;
    private getEmphasisText;
    private updateText;
    render(): VNode;
    onAfterRender(node: VNode): void;
    destroy(): void;
}
export {};
