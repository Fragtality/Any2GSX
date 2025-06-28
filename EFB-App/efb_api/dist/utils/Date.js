export const dayKeys = [
    '@fs-base,TT:TIME.SUNDAY',
    '@fs-base,TT:TIME.MONDAY',
    '@fs-base,TT:TIME.TUESDAY',
    '@fs-base,TT:TIME.WEDNESDAY',
    '@fs-base,TT:TIME.THURSDAY',
    '@fs-base,TT:TIME.FRIDAY',
    '@fs-base,TT:TIME.SATURDAY',
];
export const monthKeys = [
    '@fs-base,TT:TIME.JANUARY',
    '@fs-base,TT:TIME.FEBRUARY',
    '@fs-base,TT:TIME.MARCH',
    '@fs-base,TT:TIME.APRIL',
    '@fs-base,TT:TIME.MAY',
    '@fs-base,TT:TIME.JUNE',
    '@fs-base,TT:TIME.JULY',
    '@fs-base,TT:TIME.AUGUST',
    '@fs-base,TT:TIME.SEPTEMBER',
    '@fs-base,TT:TIME.OCTOBER',
    '@fs-base,TT:TIME.NOVEMBER',
    '@fs-base,TT:TIME.DECEMBER',
];
export const monthShortKeys = [
    '@fs-base,TT:TIME.monthShort1',
    '@fs-base,TT:TIME.monthShort2',
    '@fs-base,TT:TIME.monthShort3',
    '@fs-base,TT:TIME.monthShort4',
    '@fs-base,TT:TIME.monthShort5',
    '@fs-base,TT:TIME.monthShort6',
    '@fs-base,TT:TIME.monthShort7',
    '@fs-base,TT:TIME.monthShort8',
    '@fs-base,TT:TIME.monthShort9',
    '@fs-base,TT:TIME.monthShort10',
    '@fs-base,TT:TIME.monthShort11',
    '@fs-base,TT:TIME.monthShort12',
];
/**
 * @param year The full year (e.g. 2024)
 * @param month The month in the range 0..11
 * @returns The array of weeks
 * @description Returns the array of all the weeks (from sunday to saturday) of the given month, including weeks shorter than 7 days (e.g. if the first day of the month is a Wednesday, then the first week of the array will be 4 days long).
 */
export function getWeeksInMonth(year, month) {
    const weeks = [];
    const firstDate = new Date(year, month, 1);
    const lastDate = new Date(year, month + 1, 0);
    const numDays = lastDate.getDate();
    let dayOfWeekCounter = firstDate.getDay();
    for (let date = 1; date <= numDays; date++) {
        if (dayOfWeekCounter === 0 || weeks.length === 0) {
            weeks.push([]);
        }
        weeks[weeks.length - 1].push(date);
        dayOfWeekCounter = (dayOfWeekCounter + 1) % 7;
    }
    return weeks
        .filter((w) => w.length > 0)
        .map((w) => ({
        startDay: w[0],
        endDay: w[w.length - 1],
    }));
}
/**
 * @param year The full year (e.g. 2024)
 * @param month The month in the range 0..11
 * @param day The day of the month in the range 1..31
 * @returns the date corresponding to the start (the sunday) of the given week
 */
export function getStartOfWeek(year, month, day) {
    const date = new Date(year, month, day);
    date.setDate(date.getDate() - date.getDay());
    return date;
}
/**
 * @param day The day of the month in the range 1..31
 * @returns The formatted day (e.g. 13 -> 13th / 21 -> 21st)
 */
export function formatDay(day) {
    if (day === 31)
        return 'st';
    switch (day % 20) {
        case 1:
            return 'st';
        case 2:
            return 'nd';
        case 3:
            return 'rd';
        default:
            return 'th';
    }
}
