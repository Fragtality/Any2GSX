import { FSComponent, SubscribableUtils } from '@microsoft/msfs-sdk';
import { GamepadUiComponent } from '../../Gamepad';
import { TT } from '../../i18n';
export class TabSelector extends GamepadUiComponent {
    render() {
        return (FSComponent.buildComponent("div", { ref: this.gamepadUiComponentRef, class: {
                'tab-selector': true,
                active: this.props.active,
                hidden: this.props.hidden,
            } },
            FSComponent.buildComponent("div", { class: "disabled-layer" }),
            FSComponent.buildComponent(TT, { key: this.props.tabName, class: "text", format: "ucfirst" }),
            FSComponent.buildComponent("div", { class: "state" })));
    }
    onAfterRender(node) {
        super.onAfterRender(node);
        if (this.props.callback) {
            this.gamepadUiComponentRef.instance.onclick = (e) => {
                var _a;
                if (SubscribableUtils.toSubscribable(this.props.disabled, true).get() === true) {
                    return;
                }
                (_a = this.props.callback) === null || _a === void 0 ? void 0 : _a.call(e);
            };
        }
    }
}
