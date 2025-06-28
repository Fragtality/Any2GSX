import { Subject } from '@microsoft/msfs-sdk';
import { where } from '../sub';
export var FlightPhaseState;
(function (FlightPhaseState) {
    FlightPhaseState[FlightPhaseState["PREFLIGHT"] = 0] = "PREFLIGHT";
    FlightPhaseState[FlightPhaseState["STARTUP"] = 1] = "STARTUP";
    FlightPhaseState[FlightPhaseState["BEFORE_TAXI"] = 2] = "BEFORE_TAXI";
    FlightPhaseState[FlightPhaseState["TAXI"] = 3] = "TAXI";
    FlightPhaseState[FlightPhaseState["TAKEOFF"] = 4] = "TAKEOFF";
    FlightPhaseState[FlightPhaseState["CLIMB"] = 5] = "CLIMB";
    FlightPhaseState[FlightPhaseState["CRUISE"] = 6] = "CRUISE";
    FlightPhaseState[FlightPhaseState["DESCENT"] = 7] = "DESCENT";
    FlightPhaseState[FlightPhaseState["LANDING"] = 8] = "LANDING";
    FlightPhaseState[FlightPhaseState["TAXITOGATE"] = 9] = "TAXITOGATE";
    FlightPhaseState[FlightPhaseState["SHUTDOWN"] = 10] = "SHUTDOWN";
    FlightPhaseState[FlightPhaseState["FLIGHT_OVER"] = 11] = "FLIGHT_OVER";
    FlightPhaseState[FlightPhaseState["UNKNOWN"] = 100] = "UNKNOWN";
})(FlightPhaseState || (FlightPhaseState = {}));
class _FlightPhaseManager {
    // eslint-disable-next-line @typescript-eslint/no-empty-function
    constructor() {
        this._flightPhase = Subject.create(FlightPhaseState.UNKNOWN);
        /* Public getters */
        this.flightPhase = this._flightPhase;
        this.isFlightOver = this._flightPhase.map(where(FlightPhaseState.FLIGHT_OVER));
        this.onFlightPhaseStateChangedSubscription = null;
    }
    /**
     * Static singleton instance of the Flight Phase Manager
     * @internal
     */
    static get instance() {
        return (window.FLIGHT_PHASE_MANAGER = _FlightPhaseManager._instance =
            window.FLIGHT_PHASE_MANAGER || _FlightPhaseManager._instance || new _FlightPhaseManager());
    }
    /**
     * The bus is set once at EFB initialization from efb_ui.tsx
     * @internal
     */
    setBus(bus) {
        var _a;
        (_a = this.onFlightPhaseStateChangedSubscription) === null || _a === void 0 ? void 0 : _a.destroy();
        this.onFlightPhaseStateChangedSubscription = bus.on('FlightPhaseChanged', (flightPhase) => {
            switch (flightPhase) {
                // FIXME Some flight phases are probably missing
                case 'PREFLIGHT_RTC':
                case 'PREFLIGHT':
                case 'SKIP_TRANSITION_PREFLIGHT':
                    this._flightPhase.set(FlightPhaseState.PREFLIGHT);
                    break;
                case 'STARTUP':
                    this._flightPhase.set(FlightPhaseState.STARTUP);
                    break;
                case 'BEFORE_TAXI':
                case 'TAXI':
                case 'SKIP_TRANSITION_TAXI':
                    this._flightPhase.set(FlightPhaseState.TAXI);
                    break;
                case 'TAKEOFF':
                    this._flightPhase.set(FlightPhaseState.TAKEOFF);
                    break;
                case 'CLIMB':
                    this._flightPhase.set(FlightPhaseState.CLIMB);
                    break;
                case 'CRUISE':
                case 'SKIP_TRANSITION_CRUISE':
                    this._flightPhase.set(FlightPhaseState.CRUISE);
                    break;
                case 'DESCENT':
                    this._flightPhase.set(FlightPhaseState.DESCENT);
                    break;
                case 'LANDING':
                    this._flightPhase.set(FlightPhaseState.LANDING);
                    break;
                case 'TAXITOGATE':
                case 'SKIP_TRANSITION_TAXITOGATE':
                    this._flightPhase.set(FlightPhaseState.TAXITOGATE);
                    break;
                case 'SHUTDOWN':
                    this._flightPhase.set(FlightPhaseState.SHUTDOWN);
                    break;
                case 'RTC':
                    // Do nothing, changing to a dedicated flight phase will compromise the regular flight phase order
                    break;
                case 'MISSIONSUCCESS':
                case 'MISSIONABORTED':
                    this._flightPhase.set(FlightPhaseState.FLIGHT_OVER);
                    break;
                // FIXME Mission specific flight phases, the information could probably be given in some way
                case 'REACH BANNER APPROACH':
                case 'HOOKBANNER':
                case 'REACH PASS':
                case 'PASS':
                case 'CRUISEBACK':
                case 'REACH DROP':
                case 'DROP BANNER':
                    // Aerial advertising specific
                    break;
                case 'WATERDROP':
                    // Firefighting specific
                    break;
                default:
                    console.warn(`Received unknown flight phase '${flightPhase}'`);
                    this._flightPhase.set(FlightPhaseState.UNKNOWN);
            }
        });
    }
    /**
     * @description This function is used in order to verify if a given flight phase has been reached.
     * Always return true when the flight phase is unknown.
     * @param flightPhase The flight phase that has been reached or not
     * @returns A subscribable that returns whether the given flight phase has been reached
     */
    hasReachedFlightPhaseState(flightPhase) {
        return this.flightPhase.map((currentFlightPhaseState) => currentFlightPhaseState >= flightPhase);
    }
}
export const FlightPhaseManager = _FlightPhaseManager.instance;
