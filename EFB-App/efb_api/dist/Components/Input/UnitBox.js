import { FSComponent, NumberFormatter, Subject, SubscribableUtils, } from '@microsoft/msfs-sdk';
import { GamepadUiComponent } from '../../Gamepad';
import { UnitFormatter } from '../../utils';
import { TextBox } from './TextBox';
export class UnitBox extends GamepadUiComponent {
    constructor() {
        super(...arguments);
        this.valueNumber = this.props.value;
        this.unit = SubscribableUtils.toSubscribable(this.props.unit || null, true);
        this.valueText = Subject.create('0');
        this.subs = [];
        this.suffixSmall = Subject.create('');
        this.suffixBig = Subject.create('');
    }
    static createDefaultUnitTextMap() {
        const originalMap = UnitFormatter.getUnitTextMap();
        const map = {};
        for (const family in originalMap) {
            const nameMap = (map[family] = {});
            const originalNameMap = originalMap[family];
            for (const name in originalNameMap) {
                const text = (nameMap[name] = ['', '']);
                const originalText = originalNameMap[name];
                if (originalText[0].startsWith('°')) {
                    text[0] = '°';
                    text[1] = originalText.substring(1);
                }
                else {
                    text[1] = originalText;
                }
            }
        }
        return map;
    }
    updateDisplay(value, displayUnit) {
        if (!displayUnit || !value.unit.canConvert(displayUnit)) {
            displayUnit = value.unit;
        }
        const numberValue = value.asUnit(displayUnit);
        // prevent scientific notation error
        if (numberValue >= 9999999) {
            this.valueText.set('9999999');
            return;
        }
        const numberText = NumberFormatter.create({
            nanString: '0',
        })(numberValue);
        if (numberText.includes('.')) {
            const dotPos = numberText.indexOf('.');
            this.valueText.set(numberText.substring(0, dotPos + 3)); // crop to 2 decimals after dot
        }
        else {
            this.valueText.set(this.valueText.get().includes('.') ? `${numberText}.` : numberText); // allow last dot to allows decimals typing
        }
        const unitTexts = UnitBox.DEFAULT_UNIT_FORMATTER(displayUnit);
        if (unitTexts === null) {
            this.suffixSmall.set('');
            this.suffixBig.set('');
            return;
        }
        this.suffixSmall.set(unitTexts[0]);
        this.suffixBig.set(unitTexts[1]);
    }
    render() {
        return (FSComponent.buildComponent("div", { ref: this.gamepadUiComponentRef, class: { unitbox: true, 'unitbox--disabled': this.props.disabled || false } },
            FSComponent.buildComponent(TextBox, { align: "right", showDeleteIcon: this.props.showDeleteIcon || false, customDeleteIcon: this.props.customDeleteIcon, focusOnClear: this.props.focusOnClear, onDelete: this.props.onDelete, model: this.valueText, placeholder: this.props.placeholder, hidePlaceholderOnFocus: this.props.hidePlaceholderOnFocus, disabled: this.props.disabled, focusOnInit: this.props.focusOnInit, onFocusIn: this.props.onFocusIn, onFocusOut: this.props.onFocusOut, onInput: this.props.onInput, onKeyPress: this.props.onKeyPress, charFilter: (char) => (char >= '0' && char <= '9') || [',', '.'].includes(char), suffix: FSComponent.buildComponent(FSComponent.Fragment, null,
                    FSComponent.buildComponent("span", null, this.suffixSmall),
                    FSComponent.buildComponent("span", null, this.suffixBig)) })));
    }
    onAfterRender(node) {
        super.onAfterRender(node);
        this.subs.push(this.valueText.sub((value) => {
            value = value.replace(',', '.').replace(/\.+$/, '');
            let displayUnit = this.unit.get();
            if (!displayUnit || !this.valueNumber.get().unit.canConvert(displayUnit)) {
                displayUnit = this.valueNumber.get().unit;
            }
            const unitValue = displayUnit.convertTo(Number(value), this.valueNumber.get().unit);
            this.valueNumber.set(unitValue);
            this.updateDisplay(this.valueNumber.get(), this.unit.get());
        }), this.valueNumber.sub((value) => {
            this.updateDisplay(value, this.unit.get());
        }, true), this.unit.sub((unit) => {
            this.updateDisplay(this.valueNumber.get(), unit);
        }, true));
    }
    destroy() {
        this.subs.forEach((s) => s.destroy());
        super.destroy();
    }
}
UnitBox.DEFAULT_UNIT_TEXT_MAP = UnitBox.createDefaultUnitTextMap();
UnitBox.DEFAULT_UNIT_FORMATTER = (unit) => {
    var _a;
    const text = (_a = UnitBox.DEFAULT_UNIT_TEXT_MAP[unit.family]) === null || _a === void 0 ? void 0 : _a[unit.name];
    if (text) {
        return text;
    }
    return null;
};
