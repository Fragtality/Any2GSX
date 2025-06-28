import { FSComponent } from '@microsoft/msfs-sdk';
import { GamepadUiComponent } from '../../Gamepad';
export class Tag extends GamepadUiComponent {
    constructor() {
        super(...arguments);
        this.closeButtonRef = FSComponent.createRef();
    }
    onAfterRender(node) {
        super.onAfterRender(node);
        if (this.props.iconPath && this.props.onButtonClick) {
            this.closeButtonRef.instance.addEventListener('click', this.props.onButtonClick);
        }
    }
    destroy() {
        if (this.props.iconPath && this.props.onButtonClick) {
            this.closeButtonRef.instance.removeEventListener('click', this.props.onButtonClick);
        }
        super.destroy();
    }
    render() {
        return (FSComponent.buildComponent("div", { ref: this.gamepadUiComponentRef, class: "tag-container" },
            FSComponent.buildComponent("span", { class: "title light-text" }, this.props.title),
            this.props.iconPath && (FSComponent.buildComponent("div", { class: "tag-button", ref: this.closeButtonRef },
                FSComponent.buildComponent("icon-element", { class: "tag-button-icon", "icon-url": this.props.iconPath })))));
    }
}
