export declare const dayKeys: readonly string[];
export declare const monthKeys: readonly string[];
export declare const monthShortKeys: readonly string[];
export interface WeekPeriod {
    startDay: number;
    endDay: number;
}
/**
 * @param year The full year (e.g. 2024)
 * @param month The month in the range 0..11
 * @returns The array of weeks
 * @description Returns the array of all the weeks (from sunday to saturday) of the given month, including weeks shorter than 7 days (e.g. if the first day of the month is a Wednesday, then the first week of the array will be 4 days long).
 */
export declare function getWeeksInMonth(year: number, month: number): WeekPeriod[];
/**
 * @param year The full year (e.g. 2024)
 * @param month The month in the range 0..11
 * @param day The day of the month in the range 1..31
 * @returns the date corresponding to the start (the sunday) of the given week
 */
export declare function getStartOfWeek(year: number, month: number, day: number): Date;
/**
 * @param day The day of the month in the range 1..31
 * @returns The formatted day (e.g. 13 -> 13th / 21 -> 21st)
 */
export declare function formatDay(day: number): string;
