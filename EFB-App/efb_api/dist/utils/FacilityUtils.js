import { AirportUtils, DmsFormatter, FacilityType, FacilityUtils, ICAO, UnitType, } from '@microsoft/msfs-sdk';
/**
 * @param facility the facility
 * @returns if the facility implements SelectedAirportFacility or not
 */
export function isSelectedAirportFacility(facility) {
    return 'currentRunway' in facility;
}
/**
 * @param facility the facility
 * @returns if the facility implements SelectedAirportFacility or not
 */
export function isAirportFacility(facility) {
    return FacilityUtils.isFacilityType(facility, FacilityType.Airport) && 'altitude' in facility;
}
/**
 * Airport size.
 */
export var AirportSize;
(function (AirportSize) {
    AirportSize["Large"] = "Large";
    AirportSize["Medium"] = "Medium";
    AirportSize["Small"] = "Small";
})(AirportSize || (AirportSize = {}));
export const LargeAirportThresholdFt = 8100;
export const MediumAirportThresholdFt = 5000;
/**
 * Gets the size of an airport according to its longest runway
 * @param airport An airport
 * @returns the size of the airport.
 */
export function getAirportSize(airport) {
    const longestRunway = AirportUtils.getLongestRunway(airport);
    if (!longestRunway) {
        return AirportSize.Small;
    }
    const longestRwyLengthFeet = UnitType.METER.convertTo(longestRunway.length, UnitType.FOOT);
    return longestRwyLengthFeet >= LargeAirportThresholdFt
        ? AirportSize.Large
        : longestRwyLengthFeet >= MediumAirportThresholdFt || airport.towered
            ? AirportSize.Medium
            : AirportSize.Small;
}
/**
 * @param facility the facility
 * @param propertyName the property name of the facility
 * @param getter the function computing the wanted field
 * @returns the facility field
 */
function getFacilityStrField(facility, propertyName, getter) {
    return facility ? getter(facility[propertyName]) : '';
}
/**
 * @param facility the facility corresponding to the ICAO
 * @returns the ICAO
 */
export function getICAOIdent(facility) {
    return getFacilityStrField(facility, 'icao', ICAO.getIdent);
}
/**
 * @param facility the facility which translated name is expected
 * @returns the translated name
 */
export function getFacilityName(facility) {
    return getFacilityStrField(facility, 'name', Utils.Translate);
}
/**
 * @param runway the runway
 * @returns the runway display name
 */
export function getRunwayName(runway) {
    return `runway ${runway.designation}`;
}
/**
 * @param facility the facility
 * @returns the runway display name
 */
export function getCurrentRunwayName(facility) {
    return facility && isSelectedAirportFacility(facility) ? getRunwayName(facility.currentRunway) : '';
}
/**
 * @param facilityType The type of the facility
 * @returns The path to the facility type icon
 */
export function getFacilityIconPath(facilityType) {
    let svgName = '';
    switch (facilityType) {
        case FacilityType.Airport:
        case FacilityType.RWY:
            svgName = 'Airport';
            break;
        case FacilityType.VOR:
            svgName = 'VOR';
            break;
        case FacilityType.NDB:
            svgName = 'NDB';
            break;
        case FacilityType.USR:
            svgName = 'Intersection';
            break;
        case FacilityType.Intersection:
        default:
            svgName = 'Waypoint';
    }
    return `coui://html_ui/efb_ui/efb_os/Assets/icons/facilities/${svgName}.svg`;
}
/**
 * Create a Facility of type USR at the given coordinate
 * @param repository the facility repository that will receive the new facility
 * @param lat the latitude of the custom point
 * @param lon the longitude of the custom point
 * @returns The custom facility added to the FacilityRepository
 */
export function createCustomFacility(repository, lat, lon) {
    const customIdent = getLatLonStr(lat, lon);
    // Spaces in the ICAO for the indent to begin at index 7
    const customFac = {
        icao: `U      ${customIdent}`,
        city: 'none',
        lat: lat,
        lon: lon,
        magvar: 0,
        name: 'Custom point',
        region: 'none',
    };
    repository.add(customFac);
    return customFac;
}
/**
 * Parse coordinates to get a string with the '1234N5678E' format
 * @param lat the latitude of the point to parse
 * @param lon the longitude of the point to parse
 * @returns the string of parsed coordinates
 */
export function getLatLonStr(lat, lon) {
    const dmsFormatter = new DmsFormatter();
    const partsLat = dmsFormatter.parseLat(lat);
    let customName = partsLat.degrees.toFixed(0) + partsLat.minutes.toFixed(0) + partsLat.direction;
    const partsLon = dmsFormatter.parseLon(lon);
    customName += partsLon.degrees.toFixed(0) + partsLon.minutes.toFixed(0) + partsLon.direction;
    return customName;
}
