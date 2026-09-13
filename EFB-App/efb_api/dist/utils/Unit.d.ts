import { Unit } from '@microsoft/msfs-sdk';

/**
 * A utility class for creating unit formatters.
 *
 * Each unit formatter is a function which generates output strings from input measurement units.
 */
export declare class UnitFormatter {
    private static readonly UNIT_TEXT;
    private static UNIT_TEXT_LOWER?;
    private static UNIT_TEXT_UPPER?;
    /**
     * Creates a function which formats measurement units to strings representing their abbreviated names.
     * @param defaultString The string to output when the input unit cannot be formatted. Defaults to the empty string.
     * @param charCase The case to enforce on the output string. Defaults to `'normal'`.
     * @returns A function which formats measurement units to strings representing their abbreviated names.
     */
    static create(defaultString?: string, charCase?: 'normal' | 'upper' | 'lower'): (unit: Unit<string>) => string;
    /**
     * Creates a record of lowercase unit abbreviated names.
     * @returns A record of lowercase unit abbreviated names.
     */
    private static createLowerCase;
    /**
     * Creates a record of uppercase unit abbreviated names.
     * @returns A record of uppercase unit abbreviated names.
     */
    private static createUpperCase;
    /**
     * Gets a mapping of unit family and name to text used by UnitFormatter to format units. The returned object maps
     * unit families to objects that map unit names within each family to formatted text.
     * @returns A mapping of unit family and name to text used by UnitFormatter to format units.
     */
    static getUnitTextMap(): Readonly<Partial<Record<string, Readonly<Partial<Record<string, string>>>>>>;
}
