import { FSComponent, Subject, SubscribableMapFunctions, SubscribableUtils, } from '@microsoft/msfs-sdk';
import { GamepadUiComponent } from '../../Gamepad';
export class Switch extends GamepadUiComponent {
    constructor() {
        var _a;
        super(...arguments);
        this.turnOnDirection = (_a = this.props.turnOnDirection) !== null && _a !== void 0 ? _a : 'right';
        this.checked = SubscribableUtils.isMutableSubscribable(this.props.checked)
            ? this.props.checked
            : Subject.create(!!this.props.checked);
        this.turnOnDirectionSub = this.turnOnDirection === 'right' ? this.checked : this.checked.map(SubscribableMapFunctions.not());
        this.sliderRef = FSComponent.createRef();
    }
    onAfterRender(node) {
        super.onAfterRender(node);
        /* Update the switch checked state on click when it is enabled */
        this.gamepadUiComponentRef.instance.onclick = () => {
            if (SubscribableUtils.toSubscribable(this.props.disabled, true).get() === true) {
                return;
            }
            this.checked.set(!this.checked.get());
        };
        this.checked.sub((checked) => {
            if (this.props.callback)
                this.props.callback(checked);
        });
    }
    render() {
        return (FSComponent.buildComponent("div", { class: { 'switch-container': true, checked: this.checked }, ref: this.gamepadUiComponentRef },
            FSComponent.buildComponent("div", { class: { slider: true, 'slider--right': this.turnOnDirectionSub }, ref: this.sliderRef },
                FSComponent.buildComponent("div", { class: "disabled-layer" }))));
    }
}
