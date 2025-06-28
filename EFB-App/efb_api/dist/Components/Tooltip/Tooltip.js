import { FSComponent, SubscribableUtils } from '@microsoft/msfs-sdk';
import { GamepadUiComponent } from '../../Gamepad';
import { TT } from '../../i18n';
import { isVNode, value } from '../../utils';
export class Tooltip extends GamepadUiComponent {
    constructor() {
        var _a;
        super(...arguments);
        this.forceHide = (_a = this.props.condition) !== null && _a !== void 0 ? _a : false;
    }
    render() {
        var _a;
        const position = (_a = this.props.position) !== null && _a !== void 0 ? _a : 'bottom';
        const content = value(this.props.content);
        return (FSComponent.buildComponent("div", { class: "tooltip-component", ref: this.gamepadUiComponentRef },
            this.props.children,
            FSComponent.buildComponent("div", { class: { tooltip: true, forceHide: this.forceHide, [position]: true } },
                !SubscribableUtils.isSubscribable(content) && typeof content !== 'string' && isVNode(content) ? (content) : (FSComponent.buildComponent(TT, { class: "tooltip-description", key: content, format: "ucfirst" })),
                FSComponent.buildComponent("svg", { class: "pointer-svg" },
                    FSComponent.buildComponent("path", { d: "M 0 9 l10 -8 l10 8", fill: "rgb(15, 20, 27)", stroke: "rgb(63, 67, 73)", "stroke-width": "2" })))));
    }
}
