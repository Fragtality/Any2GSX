import { UnitFamily, UnitType } from '@microsoft/msfs-sdk';
/**
 * A utility class for creating unit formatters.
 *
 * Each unit formatter is a function which generates output strings from input measurement units.
 */
export class UnitFormatter {
    /**
     * Creates a function which formats measurement units to strings representing their abbreviated names.
     * @param defaultString The string to output when the input unit cannot be formatted. Defaults to the empty string.
     * @param charCase The case to enforce on the output string. Defaults to `'normal'`.
     * @returns A function which formats measurement units to strings representing their abbreviated names.
     */
    static create(defaultString = '', charCase = 'normal') {
        var _a, _b;
        switch (charCase) {
            case 'upper':
                (_a = UnitFormatter.UNIT_TEXT_UPPER) !== null && _a !== void 0 ? _a : (UnitFormatter.UNIT_TEXT_UPPER = UnitFormatter.createUpperCase());
                return (unit) => { var _a, _b; 
                // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                return (_b = (_a = UnitFormatter.UNIT_TEXT_UPPER[unit.family]) === null || _a === void 0 ? void 0 : _a[unit.name]) !== null && _b !== void 0 ? _b : defaultString; };
            case 'lower':
                (_b = UnitFormatter.UNIT_TEXT_LOWER) !== null && _b !== void 0 ? _b : (UnitFormatter.UNIT_TEXT_LOWER = UnitFormatter.createLowerCase());
                return (unit) => { var _a, _b; 
                // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                return (_b = (_a = UnitFormatter.UNIT_TEXT_LOWER[unit.family]) === null || _a === void 0 ? void 0 : _a[unit.name]) !== null && _b !== void 0 ? _b : defaultString; };
            default:
                return (unit) => { var _a, _b; return (_b = (_a = UnitFormatter.UNIT_TEXT[unit.family]) === null || _a === void 0 ? void 0 : _a[unit.name]) !== null && _b !== void 0 ? _b : defaultString; };
        }
    }
    /**
     * Creates a record of lowercase unit abbreviated names.
     * @returns A record of lowercase unit abbreviated names.
     */
    static createLowerCase() {
        const lower = {};
        for (const family in UnitFormatter.UNIT_TEXT) {
            const familyText = UnitFormatter.UNIT_TEXT[family];
            lower[family] = {};
            for (const unit in familyText) {
                // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                lower[family][unit] = familyText[unit].toLowerCase();
            }
        }
        return lower;
    }
    /**
     * Creates a record of uppercase unit abbreviated names.
     * @returns A record of uppercase unit abbreviated names.
     */
    static createUpperCase() {
        const upper = {};
        for (const family in UnitFormatter.UNIT_TEXT) {
            const familyText = UnitFormatter.UNIT_TEXT[family];
            upper[family] = {};
            for (const unit in familyText) {
                // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                upper[family][unit] = familyText[unit].toUpperCase();
            }
        }
        return upper;
    }
    /**
     * Gets a mapping of unit family and name to text used by UnitFormatter to format units. The returned object maps
     * unit families to objects that map unit names within each family to formatted text.
     * @returns A mapping of unit family and name to text used by UnitFormatter to format units.
     */
    static getUnitTextMap() {
        return UnitFormatter.UNIT_TEXT;
    }
}
UnitFormatter.UNIT_TEXT = {
    [UnitFamily.Distance]: {
        [UnitType.METER.name]: 'M',
        [UnitType.FOOT.name]: 'FT',
        [UnitType.KILOMETER.name]: 'KM',
        [UnitType.NMILE.name]: 'NM',
        [UnitType.MILE.name]: 'SM',
    },
    [UnitFamily.Angle]: {
        [UnitType.DEGREE.name]: '°',
        [UnitType.RADIAN.name]: 'rad',
    },
    [UnitFamily.Duration]: {
        [UnitType.SECOND.name]: 'SEC',
        [UnitType.MINUTE.name]: 'MIN',
        [UnitType.HOUR.name]: 'HR',
    },
    [UnitFamily.Weight]: {
        [UnitType.KILOGRAM.name]: 'KG',
        [UnitType.POUND.name]: 'LBS',
        [UnitType.LITER_FUEL.name]: 'LT',
        [UnitType.GALLON_FUEL.name]: 'GAL',
        [UnitType.IMP_GALLON_FUEL.name]: 'IG',
    },
    [UnitFamily.Volume]: {
        [UnitType.LITER.name]: 'L',
        [UnitType.GALLON.name]: 'GAL',
    },
    [UnitFamily.Pressure]: {
        [UnitType.HPA.name]: 'HPA',
        [UnitType.IN_HG.name]: 'IN',
    },
    [UnitFamily.Temperature]: {
        [UnitType.CELSIUS.name]: '°C',
        [UnitType.FAHRENHEIT.name]: '°F',
    },
    [UnitFamily.TemperatureDelta]: {
        [UnitType.DELTA_CELSIUS.name]: '°C',
        [UnitType.DELTA_FAHRENHEIT.name]: '°F',
    },
    [UnitFamily.Speed]: {
        [UnitType.KNOT.name]: 'KT',
        [UnitType.KPH.name]: 'KM/H',
        [UnitType.MPM.name]: 'MPM',
        [UnitType.FPM.name]: 'FPM',
    },
    [UnitFamily.WeightFlux]: {
        [UnitType.KGH.name]: 'KG/HR',
        [UnitType.PPH.name]: 'LB/HR',
        [UnitType.LPH_FUEL.name]: 'LT/HR',
        [UnitType.GPH_FUEL.name]: 'GAL/HR',
        [UnitType.IGPH_FUEL.name]: 'IG/HR',
    },
};
