import { FSComponent, NumberFormatter, Subject, SubscribableUtils, } from '@microsoft/msfs-sdk';
import { GamepadUiComponent } from '../../Gamepad';
import { TT } from '../../i18n';
import { IconButton } from '../Button';
import { TextBox } from '../Input';
/** Renders a value along with 2 buttons to increase or decrease it */
export class Incremental extends GamepadUiComponent {
    constructor(props) {
        var _a, _b, _c, _d, _e;
        super(props);
        this.min = SubscribableUtils.toSubscribable((_a = this.props.min) !== null && _a !== void 0 ? _a : 0, true);
        this.max = SubscribableUtils.toSubscribable((_b = this.props.max) !== null && _b !== void 0 ? _b : Number.MAX_SAFE_INTEGER, true);
        this.step = SubscribableUtils.toSubscribable((_c = this.props.step) !== null && _c !== void 0 ? _c : 1, true);
        this.formatter = (_d = this.props.formatter) !== null && _d !== void 0 ? _d : NumberFormatter.create({
            precision: Utils.countDecimals(this.step.get()),
            // Gets the number of digits of the maximum + 2 digits for the decimals
            maxDigits: (Math.log(this.max.get()) * Math.LOG10E + 3) | 0,
            forceDecimalZeroes: false,
            nanString: '-',
        });
        this.useTextbox = (_e = this.props.useTextbox) !== null && _e !== void 0 ? _e : true;
        this.displayedValue = Subject.create('');
        if (SubscribableUtils.isMutableSubscribable(props.value)) {
            this.value = props.value;
        }
        else if (SubscribableUtils.isSubscribable(props.value)) {
            this.value = Subject.create(props.value.get());
            this.subscribableValueSubscription = props.value.sub((value) => this.value.set(value));
        }
        else {
            this.value = Subject.create(props.value);
        }
        this.valueSubscription = this.value.sub((newValue) => {
            var _a, _b;
            (_b = (_a = this.props).onChange) === null || _b === void 0 ? void 0 : _b.call(_a, newValue);
        });
        this.displayedValueSubscription = this.value.sub((newValue) => {
            this.displayedValue.set(this.formatter(newValue));
        }, true);
    }
    renderButton(icon, direction = -1) {
        if (icon !== undefined && typeof icon !== 'string') {
            return icon;
        }
        return (FSComponent.buildComponent(IconButton, { class: direction === -1 ? 'incremental-decrease' : 'incremental-increase', iconPath: (icon !== null && icon !== void 0 ? icon : direction === -1)
                ? 'coui://html_ui/efb_ui/efb_os/Assets/icons/Remove.svg'
                : 'coui://html_ui/efb_ui/efb_os/Assets/icons/Add.svg', callback: () => {
                var _a, _b;
                const newValue = Utils.Clamp(this.value.get() + this.step.get() * direction, this.min.get(), this.max.get());
                this.value.set(newValue);
                (_b = (_a = this.props).onButtonClicked) === null || _b === void 0 ? void 0 : _b.call(_a, direction);
            }, disabled: this.props.disabled }));
    }
    renderData() {
        if (this.useTextbox) {
            return (FSComponent.buildComponent(TextBox, { model: this.displayedValue, class: "data-display", charFilter: (char) => (char >= '0' && char <= '9') || char === '.', suffix: this.props.suffix, align: "center", onInput: (e) => {
                    const newValue = Number(e.value);
                    if (newValue < this.min.get() || newValue > this.max.get()) {
                        this.displayedValue.set(this.formatter(Utils.Clamp(newValue, this.min.get(), this.max.get())));
                        return;
                    }
                    if (!Number.isNaN(newValue)) {
                        this.value.set(Utils.Clamp(newValue, this.min.get(), this.max.get()));
                    }
                }, onFocusIn: this.onTextBoxFocusIn.bind(this), onFocusOut: this.onTextBoxFocusOut.bind(this), disabled: this.props.disabled }));
        }
        else {
            const dataSuffix = this.props.suffix ? FSComponent.buildComponent(TT, { key: this.props.suffix, class: "suffix" }) : null;
            return (FSComponent.buildComponent("div", { class: "data-display" },
                FSComponent.buildComponent("span", { class: "data-value" }, this.displayedValue),
                dataSuffix));
        }
    }
    // Allow the user to properly enter a value by pausing it's update
    onTextBoxFocusIn() {
        this.displayedValueSubscription.pause();
    }
    onTextBoxFocusOut() {
        this.displayedValueSubscription.resume();
    }
    render() {
        return (FSComponent.buildComponent("div", { class: "incremental", ref: this.gamepadUiComponentRef },
            this.renderButton(this.props.firstIcon, -1),
            this.renderData(),
            this.renderButton(this.props.firstIcon, 1)));
    }
    destroy() {
        var _a;
        (_a = this.subscribableValueSubscription) === null || _a === void 0 ? void 0 : _a.destroy();
        this.valueSubscription.destroy();
        this.displayedValueSubscription.destroy();
        super.destroy();
    }
}
