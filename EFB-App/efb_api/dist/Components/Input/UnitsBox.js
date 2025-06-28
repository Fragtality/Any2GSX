import { FSComponent, Subject } from '@microsoft/msfs-sdk';
import { GamepadUiComponent } from '../../Gamepad';
import { Input } from './Input';
// WIP : reworking
export class UnitsBox extends GamepadUiComponent {
    constructor() {
        super(...arguments);
        this.inputSub = Subject.create('');
    }
    render() {
        return (FSComponent.buildComponent("div", { class: "units-box-container", ref: this.gamepadUiComponentRef },
            FSComponent.buildComponent(Input, { type: "text", model: this.inputSub, customClass: "units-box light-text", charFilter: (char) => char >= '0' && char <= '9', onFocusOut: () => {
                    var _a;
                    let value = parseInt(this.inputSub.get());
                    if (Number.isNaN(value) || value < 0) {
                        value = (_a = this.props.defaultValue) !== null && _a !== void 0 ? _a : 0;
                    }
                    this.props.valueSub.set(value);
                } }),
            FSComponent.buildComponent("span", { class: "suffix light-text" }, "ft")));
    }
}
