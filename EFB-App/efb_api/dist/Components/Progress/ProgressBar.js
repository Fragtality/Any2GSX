import { FSComponent, ObjectSubject } from '@microsoft/msfs-sdk';
import { ProgressComponent } from './Progress';
export class ProgressBar extends ProgressComponent {
    constructor() {
        super(...arguments);
        this.circularProgressStyle = ObjectSubject.create({
            '--progress-bar-width-percentage': '0%',
        });
    }
    updateProgress(progressRatio) {
        let progressPercentage = progressRatio * 100;
        if (progressPercentage > 100)
            progressPercentage = 100;
        else if (progressPercentage < 0)
            progressPercentage = 0;
        this.circularProgressStyle.set('--progress-bar-width-percentage', `${progressPercentage}%`);
    }
    render() {
        return (FSComponent.buildComponent("div", { class: { 'progress-bar-container': true, 'full-completion': this.fullCompletionSub }, ref: this.gamepadUiComponentRef },
            FSComponent.buildComponent("div", { class: "progress-bar", style: this.circularProgressStyle })));
    }
}
