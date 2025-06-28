import { DisplayComponent, FSComponent, Subject } from '@microsoft/msfs-sdk';
import { GamepadUiComponent } from '../../Gamepad';
import { TT } from '../../i18n';
import { mergeClassProp } from '../../utils';
import { Button } from './Button';
export class MultipleButtons extends GamepadUiComponent {
    constructor() {
        var _a;
        super(...arguments);
        this.buttonSelected = (_a = this.props.buttonSelected) !== null && _a !== void 0 ? _a : 0;
        this.buttonRefs = [...Array(this.props.titles.length)].map(() => FSComponent.createRef());
    }
    onAfterRender(node) {
        super.onAfterRender(node);
        if (this.buttonRefs.length) {
            this.buttonRefs[this.buttonSelected].instance.selectButton();
        }
    }
    buttonCallback(buttonIndex) {
        var _a, _b;
        if (this.disabled.get()) {
            return;
        }
        if (!((_b = (_a = this.props).callback) === null || _b === void 0 ? void 0 : _b.call(_a, buttonIndex))) {
            this.selectButton(buttonIndex);
        }
    }
    selectButton(buttonIndex) {
        if (buttonIndex !== this.buttonSelected) {
            this.unselectAllButtons();
            if (buttonIndex > this.buttonRefs.length || buttonIndex < 0) {
                buttonIndex = 0;
            }
            this.buttonSelected = buttonIndex;
            this.buttonRefs[buttonIndex].instance.selectButton();
        }
    }
    unselectAllButtons() {
        this.buttonRefs.forEach((button) => button.instance.unSelectButton());
    }
    renderSelectableButtons() {
        const buttons = [];
        for (let i = 0; i < this.props.titles.length; i++) {
            buttons.push(FSComponent.buildComponent(SelectableButton, { class: "single-button", title: this.props.titles[i], ref: this.buttonRefs[i], callback: () => this.buttonCallback(i) }));
        }
        return buttons;
    }
    render() {
        return (FSComponent.buildComponent("div", { ref: this.gamepadUiComponentRef, class: `multiple-buttons` }, this.renderSelectableButtons()));
    }
}
class SelectableButton extends DisplayComponent {
    constructor() {
        var _a;
        super(...arguments);
        this.selected = Subject.create((_a = this.props.selected) !== null && _a !== void 0 ? _a : false);
    }
    selectButton() {
        this.selected.set(true);
    }
    unSelectButton() {
        this.selected.set(false);
    }
    render() {
        return (FSComponent.buildComponent(Button, { class: mergeClassProp({ selected: this.selected }, this.props.class), style: this.props.style, callback: this.props.callback, hoverable: this.props.hoverable, selected: this.props.selected, disabled: this.props.disabled },
            FSComponent.buildComponent(TT, { class: "title", type: "div", key: this.props.title, format: "uppercase" })));
    }
}
