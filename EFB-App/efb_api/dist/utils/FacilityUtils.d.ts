import { Facility, FacilityRepository, FacilityType, OneWayRunway, AirportFacility as WTAirportFacility } from '@microsoft/msfs-sdk';
import { Nullable } from '../types';
import { AirportFacility, SelectedAirportFacility } from '../types/Facilities';

export type NullFacility = Nullable<Facility>;
/**
 * @param facility the facility
 * @returns if the facility implements SelectedAirportFacility or not
 */
export declare function isSelectedAirportFacility(facility: Facility): facility is SelectedAirportFacility;
/**
 * @param facility the facility
 * @returns if the facility implements SelectedAirportFacility or not
 */
export declare function isAirportFacility(facility: Facility): facility is AirportFacility;
/**
 * Airport size.
 */
export declare enum AirportSize {
    Large = "Large",
    Medium = "Medium",
    Small = "Small"
}
export declare const LargeAirportThresholdFt = 8100;
export declare const MediumAirportThresholdFt = 5000;
/**
 * Gets the size of an airport according to its longest runway
 * @param airport An airport
 * @returns the size of the airport.
 */
export declare function getAirportSize(airport: WTAirportFacility): AirportSize;
/**
 * @param facility the facility corresponding to the ICAO
 * @returns the ICAO
 */
export declare function getICAOIdent(facility: NullFacility): string;
/**
 * @param facility the facility which translated name is expected
 * @returns the translated name
 */
export declare function getFacilityName(facility: NullFacility): string;
/**
 * @param runway the runway
 * @param shortened if the name should be shortened to RWY
 * @returns the runway display name
 */
export declare function getRunwayName(runway: OneWayRunway, shortened?: boolean): string;
/**
 * @param facility the facility
 * @returns the runway display name
 */
export declare function getCurrentRunwayName(facility: NullFacility): string;
/**
 * @param facilityType The type of the facility
 * @returns The path to the facility type icon
 */
export declare function getFacilityIconPath(facilityType: FacilityType): string;
/**
 * Create a Facility of type USR at the given coordinate
 * @param repository the facility repository that will receive the new facility
 * @param lat the latitude of the custom point
 * @param lon the longitude of the custom point
 * @returns The custom facility added to the FacilityRepository
 */
export declare function createCustomFacility(repository: FacilityRepository, lat: number, lon: number): Facility;
/**
 * Parse coordinates to get a string with the '1234N5678E' format
 * @param lat the latitude of the point to parse
 * @param lon the longitude of the point to parse
 * @returns the string of parsed coordinates
 */
export declare function getLatLonStr(lat: number, lon: number): string;
