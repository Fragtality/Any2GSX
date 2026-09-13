import { ComponentProps, DisplayComponent, NodeReference, Subject, VNode } from '@microsoft/msfs-sdk';
import { ClassProp, MaybeSubscribable, Nullable } from '../../types';
import { default as ResizeObserver } from 'resize-observer-polyfill';
import { GamepadUiComponent } from '../../Gamepad';

export interface MarqueeProps extends ComponentProps {
    /**
     * Additionnal classes to apply on marquee's container
     */
    class?: ClassProp;
    /**
     * Speed calculated in pixels/second
     * @default 50
     */
    speed?: MaybeSubscribable<number>;
    /**
     * Delay in ms before starting and resetting transition
     * @default 500
     */
    delay?: MaybeSubscribable<number>;
    /**
     * Delay in ms before starting transition
     * Override delay
     * @default 500
     */
    startDelay?: MaybeSubscribable<number>;
    /**
     * Delay in ms before resetting transition to start
     * Override delay
     * @default 500
     */
    resetDelay?: MaybeSubscribable<number>;
    /**
     * Explicitly sets the Marquee's activator
     * @default Marquee's instance
     */
    activator?: NodeReference<GamepadUiComponent<HTMLElement> | HTMLElement>;
}
export declare class Marquee extends DisplayComponent<MarqueeProps> {
    protected readonly containerRef: NodeReference<HTMLDivElement>;
    protected readonly marqueeRef: NodeReference<HTMLDivElement>;
    protected readonly speedPxPerSeconds: import('@microsoft/msfs-sdk').Subscribable<number>;
    protected readonly startDelayMs: import('@microsoft/msfs-sdk').Subscribable<number>;
    protected readonly resetDelayMs: import('@microsoft/msfs-sdk').Subscribable<number>;
    protected readonly transitionActive: Subject<boolean>;
    protected readonly delayMs: Subject<number>;
    protected readonly durationMs: Subject<number>;
    protected readonly translationPx: Subject<number>;
    protected observer?: ResizeObserver;
    protected getDurationForDistance(distance: number): number;
    protected containerWidth: number;
    protected marqueeWidth: number;
    protected transitionRightPx: number;
    protected isLeftToRight: boolean;
    protected timeoutId?: NodeJS.Timeout;
    protected _onMouseEnter(): void;
    protected readonly onMouseEnter: () => void;
    protected _onMouseLeave(): void;
    protected readonly onMouseLeave: () => void;
    protected _onTransitionEnded(event: TransitionEvent): void;
    protected readonly onTransitionEnded: (event: TransitionEvent) => void;
    protected getMarqueeActivator(): Nullable<HTMLElement>;
    render(): VNode;
    onAfterRender(node: VNode): void;
    destroy(): void;
}
