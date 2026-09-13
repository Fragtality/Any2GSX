import { Subject, Subscribable } from '@microsoft/msfs-sdk';

export declare enum StopwatchState {
    READY = 0,
    RUNNING = 1,
    PAUSED = 2
}
export declare class Stopwatch {
    protected readonly _timerSeconds: Subject<number>;
    readonly timerSeconds: Subscribable<number>;
    protected readonly _state: Subject<StopwatchState>;
    readonly state: Subscribable<StopwatchState>;
    protected initialTime: number;
    protected intervalObj: NodeJS.Timeout | undefined;
    start(): void;
    pause(): void;
    reset(): void;
    private callback;
}
