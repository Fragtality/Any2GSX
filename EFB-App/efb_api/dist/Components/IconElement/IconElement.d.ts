import { DisplayComponent, SubscribableSet, Subscription, ToggleableClassNameRecord, VNode } from '@microsoft/msfs-sdk';
import { MaybeSubscribable } from '../../types';

interface IconElementProps {
    url: MaybeSubscribable<string>;
    class?: MaybeSubscribable<string> | SubscribableSet<string> | ToggleableClassNameRecord;
}
export declare class IconElement extends DisplayComponent<IconElementProps> {
    protected readonly url: import('@microsoft/msfs-sdk').Subscribable<string>;
    protected el: import('@microsoft/msfs-sdk').NodeReference<HTMLDivElement>;
    protected urlSub?: Subscription;
    protected classes: {
        'icon-element': boolean;
    };
    constructor(props: IconElementProps);
    private readonly onIconLoaded;
    render(): VNode;
    onAfterRender(node: VNode): void;
    destroy(): void;
}
export {};
//# sourceMappingURL=IconElement.d.ts.map