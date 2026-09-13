import { AirportFacility as BaseAirportFacility, Facility, OneWayRunway } from '@microsoft/msfs-sdk';

export interface AirportFacility extends BaseAirportFacility {
    altitude: number;
}
export interface SelectedAirportFacility extends Facility {
    currentRunway: OneWayRunway;
}
