/**
 * @param collection array to filter
 * @returns unique array
 */
export function unique(collection) {
    return collection.reduce((acc, item) => {
        if (!acc.includes(item)) {
            acc.push(item);
        }
        return acc;
    }, []);
}
/**
 * @param collection array to group
 * @param iteratee function or key of array
 * @returns grouped array
 */
export function groupBy(collection, iteratee) {
    return collection.reduce((accumulator, value) => {
        let key = String(typeof iteratee === 'function' ? iteratee(value) : iteratee);
        if (value && typeof value === 'object' && key in value) {
            key = String(value[key]);
        }
        if (Object.prototype.hasOwnProperty.call(accumulator, key)) {
            accumulator[key].push(value);
        }
        else {
            accumulator[key] = [value];
        }
        return accumulator;
    }, {});
}
export function random(collection) {
    const length = collection.length;
    return length ? collection[Math.floor(Math.random() * length)] : undefined;
}
