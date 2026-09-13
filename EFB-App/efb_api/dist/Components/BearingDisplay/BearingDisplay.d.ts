import { AbstractNumberUnitDisplay, AbstractNumberUnitDisplayProps, NavAngleUnit, NavAngleUnitFamily, NumberUnitInterface, Subscribable, SubscribableSet, ToggleableClassNameRecord, Unit, VNode } from '@microsoft/msfs-sdk';

/**
 * Component props for BearingDisplay.
 */
export interface BearingDisplayProps extends Omit<AbstractNumberUnitDisplayProps<NavAngleUnitFamily>, 'value' | 'displayUnit'> {
    /** The {@link NumberUnitInterface} value to display, or a subscribable which provides it. */
    value: NumberUnitInterface<NavAngleUnitFamily, NavAngleUnit> | Subscribable<NumberUnitInterface<NavAngleUnitFamily, NavAngleUnit>>;
    /**
     * The unit type in which to display the value, or a subscribable which provides it. If the unit is `null`, then the
     * native type of the value is used instead.
     */
    displayUnit: NavAngleUnit | null | Subscribable<NavAngleUnit | null>;
    /** A function which formats numbers. */
    formatter: (number: number) => string;
    /**
     * A function which formats units. The formatted unit text should be written to the 2-tuple passed to the `out`
     * parameter, as `[bigText, smallText]`. `bigText` and `smallText` will be rendered into separate `<span>` elements
     * representing the big and small components of the rendered unit text, respectively. If not defined, then units
     * will be formatted such that `bigText` is always the degree symbol (°) and `smallText` is empty for magnetic
     * bearing or `'T'` for true bearing.
     */
    unitFormatter?: (out: [string, string], unit: NavAngleUnit, number: number) => void;
    /** Whether to display `'360'` in place of `'0'`. Defaults to `true`. */
    use360?: boolean;
    /** Whether to hide the unit text when the displayed value is equal to `NaN`. Defaults to `false`. */
    hideDegreeSymbolWhenNan?: boolean;
    /** CSS class(es) to add to the root of the bearing display component. */
    class?: string | SubscribableSet<string> | ToggleableClassNameRecord;
}
/**
 * Displays a bearing value.
 */
export declare class BearingDisplay extends AbstractNumberUnitDisplay<NavAngleUnitFamily, BearingDisplayProps> {
    /**
     * A function which formats units to default text for BearingDisplay.
     * @param out The 2-tuple to which to write the formatted text, as `[bigText, smallText]`.
     * @param unit The unit to format.
     */
    static readonly DEFAULT_UNIT_FORMATTER: (out: [string, string], unit: NavAngleUnit) => void;
    private static readonly unitTextCache;
    private readonly unitFormatter;
    private readonly unitTextBigDisplay;
    private readonly unitTextSmallDisplay;
    private readonly numberText;
    private readonly unitTextBig;
    private readonly unitTextSmall;
    /** @inheritdoc */
    protected onValueChanged(value: NumberUnitInterface<NavAngleUnitFamily>): void;
    /** @inheritdoc */
    protected onDisplayUnitChanged(displayUnit: Unit<NavAngleUnitFamily> | null): void;
    /**
     * Updates this component's displayed number text.
     * @param numberValue The numeric value to display.
     */
    private updateNumberText;
    /**
     * Updates this component's displayed unit text.
     * @param numberValue The numeric value to display.
     * @param displayUnit The unit type in which to display the value.
     */
    private updateUnitText;
    /**
     * Updates whether this component's unit text spans are visible.
     * @param numberValue The numeric value displayed by this component.
     */
    private updateUnitTextVisibility;
    /** @inheritdoc */
    render(): VNode;
}
