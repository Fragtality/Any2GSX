import { EventBus, MutableSubscribable, Subscribable, Subscription } from '@microsoft/msfs-sdk';
import { Nullable } from '../types';

export declare enum FlightPhaseState {
    PREFLIGHT = 0,
    STARTUP = 1,
    BEFORE_TAXI = 2,
    TAXI = 3,
    TAKEOFF = 4,
    CLIMB = 5,
    CRUISE = 6,
    DESCENT = 7,
    LANDING = 8,
    TAXITOGATE = 9,
    SHUTDOWN = 10,
    FLIGHT_OVER = 11,
    UNKNOWN = 100
}
declare global {
    interface Window {
        FLIGHT_PHASE_MANAGER: _FlightPhaseManager | undefined;
    }
}
declare class _FlightPhaseManager {
    private static _instance?;
    protected readonly _flightPhase: MutableSubscribable<number, FlightPhaseState>;
    readonly flightPhase: Subscribable<FlightPhaseState>;
    readonly isFlightOver: import('@microsoft/msfs-sdk').MappedSubscribable<boolean>;
    protected onFlightPhaseStateChangedSubscription: Nullable<Subscription>;
    private constructor();
    /**
     * Static singleton instance of the Flight Phase Manager
     * @internal
     */
    static get instance(): _FlightPhaseManager;
    /**
     * The bus is set once at EFB initialization from efb_ui.tsx
     * @internal
     */
    setBus(bus: EventBus): void;
    /**
     * @description This function is used in order to verify if a given flight phase has been reached.
     * Always return true when the flight phase is unknown.
     * @param flightPhase The flight phase that has been reached or not
     * @returns A subscribable that returns whether the given flight phase has been reached
     */
    hasReachedFlightPhaseState(flightPhase: FlightPhaseState): Subscribable<boolean>;
}
export declare const FlightPhaseManager: _FlightPhaseManager;
export {};
