import { MutableSubscribable, NumberFormatterOptions, NumberUnitInterface, Subject, Subscription, Unit, VNode } from '@microsoft/msfs-sdk';
import { GamepadUiComponent, GamepadUiComponentProps } from '../../Gamepad';
import { MaybeSubscribable, Nullable } from '../../types';
import { TextBoxProps } from './TextBox';

interface UnitBoxProps extends Omit<TextBoxProps, 'prefix' | 'model' | 'value' | 'align' | 'suffix'> {
    value: MutableSubscribable<NumberUnitInterface<string, Unit<string>>, number>;
    unit?: Nullable<MaybeSubscribable<Nullable<Unit<string>>>>;
    min?: NumberUnitInterface<string, Unit<string>>;
    max?: NumberUnitInterface<string, Unit<string>>;
    /**
     * The number formatter options to be used when formatting for display.
     *
     * FIXME This currently gets applied the user types. This means this is only really useful for specifying `precision: 1`.
     * FIXME Refactor this component to use different code paths for handling user typing vs. formatting entered values.
     */
    numberFormatterOptions?: NumberFormatterOptions;
}
export declare class UnitBox extends GamepadUiComponent<HTMLDivElement, UnitBoxProps & GamepadUiComponentProps> {
    protected readonly valueNumber: MutableSubscribable<NumberUnitInterface<string, Unit<string>>, number>;
    protected readonly unit: import('@microsoft/msfs-sdk').Subscribable<Nullable<Unit<string>>> | import('@microsoft/msfs-sdk').Subscribable<null> | import('@microsoft/msfs-sdk').Subscribable<Unit<string>>;
    protected readonly showUnitSuffix: import('@microsoft/msfs-sdk').Subscribable<boolean> | import('@microsoft/msfs-sdk').Subscribable<false> | import('@microsoft/msfs-sdk').Subscribable<true>;
    protected readonly textValue: Subject<string>;
    protected readonly formattedTextValue: Subject<string>;
    protected readonly subs: Subscription[];
    private static readonly DEFAULT_UNIT_TEXT_MAP;
    static readonly DEFAULT_UNIT_FORMATTER: (unit: Unit<string>) => Nullable<[string, string]>;
    private static createDefaultUnitTextMap;
    private setValue;
    protected canUpdateDisplay: boolean;
    private updateDisplay;
    protected readonly suffixSmall: Subject<string>;
    protected readonly suffixBig: Subject<string>;
    render(): VNode;
    onAfterRender(node: VNode): void;
    destroy(): void;
}
export {};
