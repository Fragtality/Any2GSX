import { DisplayComponent, FSComponent, SubscribableMapFunctions, SubscribableUtils, } from '@microsoft/msfs-sdk';
export class Timer extends DisplayComponent {
    constructor() {
        var _a;
        super(...arguments);
        this.displayedTime = this.props.displayedTimeFormatter
            ? this.props.stopwatch.timerSeconds.map(this.props.displayedTimeFormatter)
            : this.props.stopwatch.timerSeconds.map((timeSeconds) => {
                const date = new Date(0);
                date.setSeconds(timeSeconds);
                return `${date.toISOString().substring(11, 19)}`;
            });
        this.showDotIndicator = SubscribableUtils.toSubscribable((_a = this.props.showDotIndicator) !== null && _a !== void 0 ? _a : false, true);
        this.hideDotIndicator = this.showDotIndicator.map(SubscribableMapFunctions.not());
    }
    render() {
        return (FSComponent.buildComponent("div", { class: "timer" },
            FSComponent.buildComponent("div", { class: {
                    dot: true,
                    hide: this.hideDotIndicator,
                } }),
            FSComponent.buildComponent("div", { class: "timer-text" }, this.displayedTime)));
    }
    destroy() {
        this.displayedTime.destroy();
        this.hideDotIndicator.destroy();
        super.destroy();
    }
}
