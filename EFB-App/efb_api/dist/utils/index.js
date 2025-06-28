export * from './Array';
export * from './Components';
export * from './Date';
export * from './FacilityUtils';
export * from './FlightPhaseManager';
export * from './GameModeManager';
export * from './MetadataReader';
export * from './NotificationUtils';
export * from './Promise';
export * from './SettingsUtils';
export * from './Stopwatch';
export * from './Unit';
export * from './Valuable';
/**
 * Sets the mouse position offsets recursively until reaching the element.
 * @param e The mouse event to process.
 * @param vec The vector to assign to.
 * @param element The element to reach.
 */
export function offsetMousePosition(e, vec, element = null) {
    const mousePosition = elementOffset(e.target, element);
    vec[0] = mousePosition[0] + e.offsetX;
    vec[1] = mousePosition[1] + e.offsetY;
}
/**
 * Get offsets recursively until reaching the limit element or the top element.
 * @param from The element from starting.
 * @param limit The element to stop.
 * @returns Coordinates from top-left offset.
 */
export function elementOffset(from, limit = null) {
    let leftOffset = 0;
    let topOffset = 0;
    let currentElement = from;
    do {
        leftOffset += currentElement.offsetLeft;
        topOffset += currentElement.offsetTop;
        currentElement = currentElement.parentNode;
    } while (currentElement !== null && currentElement !== limit);
    return new Float64Array([leftOffset, topOffset]);
}
/**
 * Debug decorator
 * @internal
 */
export const measure = (_target, propertyKey, descriptor) => {
    const originalMethod = descriptor.value;
    descriptor.value = function (...args) {
        const start = performance.now();
        const result = originalMethod.apply(this, args);
        const finish = performance.now();
        console.log(`Execution time of ${_target.constructor.name}.${propertyKey} took ${finish - start} ms`);
        return result;
    };
    return descriptor;
};
