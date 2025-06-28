import { ArraySubject, DisplayComponent, FSComponent, ObjectSubject, Subject, } from '@microsoft/msfs-sdk';
import { GamepadUiComponent } from '../../Gamepad';
import { TT } from '../../i18n';
import { mergeClassProp } from '../../utils';
import { List } from '../List';
import { Button } from './Button';
const arrowIconPath = 'coui://html_ui/efb_ui/efb_os/Assets/icons/NoMargin/Arrow.svg';
export class DropdownButton extends GamepadUiComponent {
    constructor() {
        super(...arguments);
        this.listDataset = Array.isArray(this.props.listDataset)
            ? ArraySubject.create(this.props.listDataset)
            : this.props.listDataset;
        this.isListVisible = Subject.create(false);
    }
    /* Close the list when clicking out of it */
    onClickOutOfComponent() {
        this.isListVisible.set(false);
    }
    onAfterRender(node) {
        var _a;
        super.onAfterRender(node);
        this.isListVisibleSub = (_a = this.props.isListVisible) === null || _a === void 0 ? void 0 : _a.sub(this.isListVisible.set);
    }
    destroy() {
        var _a;
        (_a = this.isListVisibleSub) === null || _a === void 0 ? void 0 : _a.destroy();
        super.destroy();
    }
    render() {
        return (FSComponent.buildComponent("div", { class: "dropdown-button", ref: this.gamepadUiComponentRef },
            FSComponent.buildComponent(DropdownButtonSelector, { class: "button--full-width", key: this.props.title, isOpen: this.isListVisible, type: this.props.type, callback: () => {
                    if (this.props.disabled) {
                        return;
                    }
                    this.isListVisible.set(!this.isListVisible.get());
                }, showArrowIcon: this.props.showArrowIcon, disabled: this.props.disabled }),
            FSComponent.buildComponent(List, { class: "dropdown-button-list", data: this.listDataset, isListVisible: this.isListVisible, isScrollable: true, renderItem: (item, index) => (FSComponent.buildComponent(DropdownButtonListItem, { name: this.props.getItemLabel(item), onClick: () => {
                        this.isListVisible.set(false);
                        this.props.onItemClick(item, index);
                    } })) })));
    }
}
class DropdownButtonSelector extends DisplayComponent {
    constructor(props) {
        super(props);
        this.iconScale = 1;
        this.iconStyle = ObjectSubject.create({
            transform: `scale(${this.iconScale}, ${this.iconScale})`,
        });
        if (this.props.showArrowIcon) {
            this.isOpenSub = this.props.isOpen.sub((isOpen) => {
                /*
                 * Theoretically, since the Arrow.svg icon remains the same when it is mirrored on the X axis,
                 * the transform property could be set with a 1-parameter call to the scale function
                 * (e.g. `scale(A) instead of scale(X, Y)`. However, this could not work properly if another
                 * SVG icon is used instead, hence keeping the code as is.
                 */
                this.iconStyle.set('transform', `scale(${this.iconScale}, ${isOpen ? -1 * this.iconScale : this.iconScale})`);
            }, true);
        }
    }
    destroy() {
        var _a;
        (_a = this.isOpenSub) === null || _a === void 0 ? void 0 : _a.destroy();
        super.destroy();
    }
    render() {
        return (FSComponent.buildComponent(Button, { class: mergeClassProp('dropdown-button-selector', this.props.class), style: this.props.style, callback: this.props.callback, hoverable: this.props.hoverable, selected: this.props.selected, disabled: this.props.disabled },
            FSComponent.buildComponent(TT, { class: "text bold-text", key: this.props.key }),
            this.props.showArrowIcon && (FSComponent.buildComponent("icon-element", { class: "dropdown-button-selector-icon", "icon-url": arrowIconPath, style: this.iconStyle }))));
    }
}
class DropdownButtonListItem extends GamepadUiComponent {
    onAfterRender(node) {
        super.onAfterRender(node);
        this.gamepadUiComponentRef.instance.onclick = this.props.onClick;
    }
    render() {
        return (FSComponent.buildComponent("div", { class: "dropdown-button-list-item", ref: this.gamepadUiComponentRef },
            FSComponent.buildComponent(TT, { key: this.props.name, type: "div", class: "text regular-text" })));
    }
}
