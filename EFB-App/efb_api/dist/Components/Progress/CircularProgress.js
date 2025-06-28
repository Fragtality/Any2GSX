import { FSComponent, ObjectSubject, Subject, SubscribableMapFunctions } from '@microsoft/msfs-sdk';
import { IconElement } from '../IconElement';
import { ProgressComponent } from './Progress';
export class CircularProgress extends ProgressComponent {
    constructor() {
        super(...arguments);
        this.circularProgressRef = FSComponent.createRef();
        this.lowerSliceRef = FSComponent.createRef();
        this.higherSliceRef = FSComponent.createRef();
        this.sliceClassToggleSub = Subject.create(false);
        this.sliceClassToggleNotSub = this.sliceClassToggleSub.map(SubscribableMapFunctions.not());
        this.circularProgressStyle = ObjectSubject.create({
            '--circular-progress-rotation-value-left': '0',
            '--circular-progress-rotation-value-right': '0',
        });
        this.circularProgressPrimaryBackgroundClass = 'circular-progress-primary-background';
        this.circularProgressSecondaryBackgroundClass = 'circular-progress-secondary-background';
        this.circularProgressSliceClasses = {
            'circular-progress-slice': true,
            [this.circularProgressSecondaryBackgroundClass]: this.sliceClassToggleSub,
            [this.circularProgressPrimaryBackgroundClass]: this.sliceClassToggleNotSub,
        };
    }
    updateProgressByHalf(progressRatio, overHalf) {
        /* Set rotation */
        /* FIXME Make it cleaner? */
        if (overHalf) {
            this.circularProgressStyle.set('--circular-progress-rotation-value-left', `${progressRatio}turn`);
            this.circularProgressStyle.set('--circular-progress-rotation-value-right', '0');
        }
        else {
            this.circularProgressStyle.set('--circular-progress-rotation-value-left', '0');
            this.circularProgressStyle.set('--circular-progress-rotation-value-right', `${progressRatio}turn`);
        }
        this.sliceClassToggleSub.set(overHalf);
    }
    updateProgress(progressRatio) {
        this.updateProgressByHalf(progressRatio, progressRatio > 0.5);
    }
    render() {
        return (FSComponent.buildComponent("div", { class: { 'circular-progress-container': true, 'full-completion': this.fullCompletionSub }, ref: this.gamepadUiComponentRef },
            FSComponent.buildComponent("div", { class: "circular-progress-circle-container" },
                FSComponent.buildComponent("div", { class: "circular-progress-circle outer" }),
                FSComponent.buildComponent("div", { class: "circular-progress-circle inner" })),
            FSComponent.buildComponent("div", { class: {
                    'circular-progress': true,
                    [this.circularProgressPrimaryBackgroundClass]: this.sliceClassToggleSub,
                    [this.circularProgressSecondaryBackgroundClass]: this.sliceClassToggleNotSub,
                }, ref: this.circularProgressRef, style: this.circularProgressStyle },
                FSComponent.buildComponent("div", { class: Object.assign(Object.assign({}, this.circularProgressSliceClasses), { left: true }), ref: this.higherSliceRef }),
                FSComponent.buildComponent("div", { class: Object.assign(Object.assign({}, this.circularProgressSliceClasses), { right: true }), ref: this.lowerSliceRef }),
                this.props.iconPath && (FSComponent.buildComponent(IconElement, { class: {
                        'full-completion-icon': true,
                        hide: this.fullCompletionSub.map(SubscribableMapFunctions.not()),
                    }, url: this.props.iconPath })))));
    }
}
