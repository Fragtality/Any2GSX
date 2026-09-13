import { DisplayComponent, Subscribable, VNode } from '@microsoft/msfs-sdk';
import { MaybeSubscribable } from '../../types';
import { Stopwatch } from '../../utils/Stopwatch';

export interface TimerProps {
    stopwatch: Stopwatch;
    displayedTimeFormatter?: (timeSeconds: number) => string;
    showDotIndicator?: MaybeSubscribable<boolean>;
}
export declare class Timer extends DisplayComponent<TimerProps> {
    protected readonly displayedTime: import('@microsoft/msfs-sdk').MappedSubscribable<string>;
    protected readonly showDotIndicator: Subscribable<boolean>;
    protected readonly hideDotIndicator: import('@microsoft/msfs-sdk').MappedSubscribable<boolean>;
    render(): VNode;
    destroy(): void;
}
