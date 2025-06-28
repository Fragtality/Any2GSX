import { FSComponent, MappedSubject, SubscribableUtils } from '@microsoft/msfs-sdk';
import { GamepadUiComponent } from '../../Gamepad';
import { mergeClassProp } from '../../utils';
export class Button extends GamepadUiComponent {
    constructor() {
        var _a, _b, _c, _d;
        super(...arguments);
        this.isButtonDisabled = SubscribableUtils.toSubscribable((_a = this.props.disabled) !== null && _a !== void 0 ? _a : false, true);
        this.isButtonHoverable = MappedSubject.create(([isHoverable, isDisabled]) => {
            return isHoverable && !isDisabled;
        }, SubscribableUtils.toSubscribable((_b = this.props.hoverable) !== null && _b !== void 0 ? _b : true, true), this.isButtonDisabled);
        // FIXME Uncomment the following when removing the deprecated props
        // protected readonly isButtonSelected = SubscribableUtils.toSubscribable(this.props.selected ?? false, true);
        this.isButtonSelected = MappedSubject.create(([isSelected, state]) => isSelected || state, SubscribableUtils.toSubscribable((_c = this.props.selected) !== null && _c !== void 0 ? _c : false, true), SubscribableUtils.toSubscribable((_d = this.props.state) !== null && _d !== void 0 ? _d : false, true));
    }
    /**
     * @deprecated Old way to render the button. Instead of extending the `Button` class, render your content as a children of `<Button>...</Button>`.
     */
    buttonRender() {
        return null;
    }
    render() {
        return (FSComponent.buildComponent("button", { ref: this.gamepadUiComponentRef, class: mergeClassProp('button', 'abstract-button', // deprecated
            {
                hoverable: this.isButtonHoverable,
                selected: this.isButtonSelected,
            }, this.props.class), style: this.props.style },
            FSComponent.buildComponent("div", { class: "disabled-layer" }),
            this.props.children,
            this.buttonRender() /* Deprecated */));
    }
    onAfterRender(node) {
        super.onAfterRender(node);
        if (this.props.callback) {
            this.gamepadUiComponentRef.instance.onclick = (event) => {
                var _a;
                if (this.isButtonDisabled.get() === true) {
                    return;
                }
                (_a = this.props.callback) === null || _a === void 0 ? void 0 : _a.call(this, event);
            };
        }
    }
    destroy() {
        this.isButtonHoverable.destroy();
        super.destroy();
    }
}
/**
 * @deprecated AbstractButton has been renamed to Button.
 */
export { Button as AbstractButton };
