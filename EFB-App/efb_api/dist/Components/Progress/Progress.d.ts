import { Subject, Subscribable, Subscription, VNode } from '@microsoft/msfs-sdk';
import { GamepadUiComponent, GamepadUiComponentProps } from '../../Gamepad';

export interface ProgressComponentProps extends GamepadUiComponentProps {
    progressRatio: number | Subscribable<number>;
}
export declare abstract class ProgressComponent<P extends ProgressComponentProps = ProgressComponentProps> extends GamepadUiComponent<HTMLDivElement, P> {
    protected progressRatioSubscription?: Subscription;
    protected readonly fullCompletionSub: Subject<boolean>;
    protected abstract updateProgress(progressRatio: number): void;
    private _updateProgress;
    onAfterRender(node: VNode): void;
    destroy(): void;
}
