import { Subject } from '@microsoft/msfs-sdk';
import { GamepadUiComponent } from '../../Gamepad';
export class ProgressComponent extends GamepadUiComponent {
    constructor() {
        super(...arguments);
        this.fullCompletionSub = Subject.create(false);
    }
    _updateProgress(progressRatio) {
        this.updateProgress(progressRatio);
        this.fullCompletionSub.set(progressRatio === 1);
    }
    onAfterRender(node) {
        super.onAfterRender(node);
        if (typeof this.props.progressRatio === 'number') {
            this._updateProgress(this.props.progressRatio);
        }
        else {
            this.progressRatioSubscription = this.props.progressRatio.sub(this._updateProgress.bind(this), true);
        }
    }
    destroy() {
        var _a;
        (_a = this.progressRatioSubscription) === null || _a === void 0 ? void 0 : _a.destroy();
        super.destroy();
    }
}
