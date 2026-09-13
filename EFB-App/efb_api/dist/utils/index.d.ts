import { Nullable } from '../types';

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
export declare function offsetMousePosition(e: MouseEvent, vec: Float64Array, element?: HTMLElement | null): void;
/**
 * Get offsets recursively until reaching the limit element or the top element.
 * @param from The element from starting.
 * @param limit The element to stop.
 * @returns Coordinates from top-left offset.
 */
export declare function elementOffset(from: HTMLElement, limit?: Nullable<HTMLElement>): Float64Array;
/**
 * Debug decorator
 * @internal
 */
export declare const measure: (_target: any, propertyKey: string, descriptor: PropertyDescriptor) => PropertyDescriptor;
