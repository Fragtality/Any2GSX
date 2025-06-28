import { AbstractNumberUnitDisplay, FSComponent, NumberFormatter, Subject, } from '@microsoft/msfs-sdk';
import { UnitFormatter } from '../../utils';
/**
 * A component which displays a number with units.
 */
export class NumberUnitDisplay extends AbstractNumberUnitDisplay {
    constructor() {
        var _a, _b;
        super(...arguments);
        // Default formatter if it is not given in the props
        this.formatter = (_a = this.props.formatter) !== null && _a !== void 0 ? _a : NumberFormatter.create({
            // TODO Properly format the time
            precision: 0.01,
            maxDigits: 5,
            forceDecimalZeroes: false,
            nanString: '0',
        });
        this.unitFormatter = (_b = this.props.unitFormatter) !== null && _b !== void 0 ? _b : NumberUnitDisplay.DEFAULT_UNIT_FORMATTER;
        this.unitTextBigDisplay = Subject.create('');
        this.unitTextSmallDisplay = Subject.create('');
        this.numberText = Subject.create('');
        this.unitTextBig = Subject.create('');
        this.unitTextSmall = Subject.create('');
    }
    /** @inheritdoc */
    onValueChanged(value) {
        this.updateDisplay(value, this.displayUnit.get());
    }
    /** @inheritdoc */
    onDisplayUnitChanged(displayUnit) {
        this.updateDisplay(this.value.get(), displayUnit);
    }
    /**
     * Updates this component's displayed number and unit text.
     * @param value The value to display.
     * @param displayUnit The unit type in which to display the value, or `null` if the value should be displayed in its
     * native unit type.
     */
    updateDisplay(value, displayUnit) {
        if (!displayUnit || !value.unit.canConvert(displayUnit)) {
            displayUnit = value.unit;
        }
        const numberValue = value.asUnit(displayUnit);
        const numberText = this.formatter(numberValue);
        this.numberText.set(numberText);
        NumberUnitDisplay.unitTextCache[0] = '';
        NumberUnitDisplay.unitTextCache[1] = '';
        this.unitFormatter(NumberUnitDisplay.unitTextCache, displayUnit, numberValue);
        this.unitTextBig.set(NumberUnitDisplay.unitTextCache[0]);
        this.unitTextSmall.set(NumberUnitDisplay.unitTextCache[1]);
        this.updateUnitTextVisibility(numberValue, NumberUnitDisplay.unitTextCache[0], NumberUnitDisplay.unitTextCache[1]);
    }
    /**
     * Updates whether this component's unit text spans are visible.
     * @param numberValue The numeric value displayed by this component.
     * @param unitTextBig The text to display in the big text span.
     * @param unitTextSmall The text to display in the small text span.
     */
    updateUnitTextVisibility(numberValue, unitTextBig, unitTextSmall) {
        if (this.props.hideUnitWhenNaN === true && isNaN(numberValue)) {
            this.unitTextBigDisplay.set('none');
            this.unitTextSmallDisplay.set('none');
            return;
        }
        // We have to hide the unit text when empty because an empty string will get rendered as a space.
        this.unitTextBigDisplay.set(unitTextBig === '' ? 'none' : '');
        this.unitTextSmallDisplay.set(unitTextSmall === '' ? 'none' : '');
    }
    /**
     * Creates the default mapping from unit to displayed text.
     * @returns The default mapping from unit to displayed text.
     */
    static createDefaultUnitTextMap() {
        const originalMap = UnitFormatter.getUnitTextMap();
        const map = {};
        for (const family in originalMap) {
            const nameMap = (map[family] = {});
            const originalNameMap = originalMap[family];
            for (const name in originalNameMap) {
                const text = (nameMap[name] = ['', '']);
                const originalText = originalNameMap[name];
                if (originalText[0] === '°') {
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
    render() {
        var _a;
        return (FSComponent.buildComponent("div", { class: (_a = this.props.class) !== null && _a !== void 0 ? _a : '', style: "white-space: nowrap;" },
            FSComponent.buildComponent("span", { class: "numberunit-num" }, this.numberText),
            FSComponent.buildComponent("span", { class: "numberunit-unit-big", style: { display: this.unitTextBigDisplay } }, this.unitTextBig),
            FSComponent.buildComponent("span", { class: "numberunit-unit-small", style: { display: this.unitTextSmallDisplay } }, this.unitTextSmall)));
    }
}
// We create our own map instead of using UnitFormatter.create() so that we don't have to generate new big and small
// text substrings with every call to the default unit formatter function.
NumberUnitDisplay.DEFAULT_UNIT_TEXT_MAP = NumberUnitDisplay.createDefaultUnitTextMap();
/**
 * A function which formats units to default text for NumberUnitDisplay.
 * @param out The 2-tuple to which to write the formatted text, as `[bigText, smallText]`.
 * @param unit The unit to format.
 */
NumberUnitDisplay.DEFAULT_UNIT_FORMATTER = (out, unit) => {
    var _a;
    const text = (_a = NumberUnitDisplay.DEFAULT_UNIT_TEXT_MAP[unit.family]) === null || _a === void 0 ? void 0 : _a[unit.name];
    if (text) {
        out[0] = text[0];
        out[1] = text[1];
    }
};
NumberUnitDisplay.unitTextCache = ['', ''];
