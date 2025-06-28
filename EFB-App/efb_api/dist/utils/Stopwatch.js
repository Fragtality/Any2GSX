import { Subject } from '@microsoft/msfs-sdk';
export var StopwatchState;
(function (StopwatchState) {
    StopwatchState[StopwatchState["READY"] = 0] = "READY";
    StopwatchState[StopwatchState["RUNNING"] = 1] = "RUNNING";
    StopwatchState[StopwatchState["PAUSED"] = 2] = "PAUSED";
})(StopwatchState || (StopwatchState = {}));
export class Stopwatch {
    constructor() {
        this._timerSeconds = Subject.create(0);
        this.timerSeconds = this._timerSeconds;
        this._state = Subject.create(StopwatchState.READY);
        this.state = this._state;
        this.initialTime = 0;
    }
    start() {
        // When resuming after a pause, the time elapsed until now shall remain in the initialTime
        const offset = this._state.get() === StopwatchState.PAUSED ? this._timerSeconds.get() : 0;
        this.initialTime = SimVar.GetSimVarValue('E:SIMULATION TIME', 'seconds') - offset;
        this.intervalObj = setInterval(this.callback.bind(this), 500);
        this._state.set(StopwatchState.RUNNING);
    }
    pause() {
        clearInterval(this.intervalObj);
        this._state.set(StopwatchState.PAUSED);
    }
    reset() {
        clearInterval(this.intervalObj);
        this.initialTime = 0;
        this._timerSeconds.set(0);
        this._state.set(StopwatchState.READY);
    }
    callback() {
        this._timerSeconds.set(SimVar.GetSimVarValue('E:SIMULATION TIME', 'seconds') - this.initialTime);
    }
}
