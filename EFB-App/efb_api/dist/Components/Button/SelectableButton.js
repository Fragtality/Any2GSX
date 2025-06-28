import { FSComponent } from '@microsoft/msfs-sdk';
import { TT } from '../../i18n';
import { TypedButton } from './TypedButton';
/** @deprecated Deprecated component */
export class SelectableButton extends TypedButton {
    constructor() {
        var _a;
        super(...arguments);
        this.selected = (_a = this.props.selected) !== null && _a !== void 0 ? _a : false;
    }
    selectButton() {
        if (!this.selected) {
            this.selected = true;
            this.switchSelection(true);
        }
    }
    unSelectButton() {
        if (this.selected) {
            this.selected = false;
            this.switchSelection(false);
        }
    }
    switchSelection(selected) {
        this.gamepadUiComponentRef.instance.classList.toggle('selected', selected);
    }
    buttonRender() {
        return (FSComponent.buildComponent("div", { class: "selectable-button" },
            FSComponent.buildComponent(TT, { class: "title", type: "div", key: this.props.title, format: "upper" })));
    }
}
