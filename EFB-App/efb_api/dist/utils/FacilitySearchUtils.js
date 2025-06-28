import { FacilityLoader, FacilityRepository, FacilitySearchType, GeoPoint, ICAO, } from '@microsoft/msfs-sdk';
export class FacilitySearchUtils {
    constructor(bus) {
        this.facilityLoader = null;
        this.position = new GeoPoint(0, 0);
        bus.getSubscriber()
            .on('gps-position')
            .handle((pos) => {
            this.position.set(pos.lat, pos.long);
        });
        this.facilityLoader = new FacilityLoader(FacilityRepository.getRepository(bus));
    }
    static getSearchUtils(bus) {
        var _a;
        return ((_a = FacilitySearchUtils.INSTANCE) !== null && _a !== void 0 ? _a : (FacilitySearchUtils.INSTANCE = new FacilitySearchUtils(bus)));
    }
    orderByIdentsAndDistance(a, b) {
        const aIdent = ICAO.getIdent(a.icao).trim();
        const bIdent = ICAO.getIdent(b.icao).trim();
        if (aIdent === bIdent) {
            const aDist = this.position.distance(a.lat, a.lon);
            const bDist = this.position.distance(b.lat, b.lon);
            return aDist - bDist;
        }
        else {
            return aIdent.localeCompare(bIdent);
        }
    }
    async loadFacilities(ident, facilitySearchType = FacilitySearchType.All) {
        if (this.facilityLoader === null) {
            return Promise.resolve([]);
        }
        const icaos = await this.facilityLoader.searchByIdent(facilitySearchType, ident, 15);
        let facilities = (await Promise.all(icaos.map((icao) => {
            if (this.facilityLoader === null) {
                return;
            }
            return this.facilityLoader.getFacility(ICAO.getFacilityType(icao), icao);
        })));
        facilities.sort((a, b) => this.orderByIdentsAndDistance(a, b));
        facilities = facilities.filter((fac) => ICAO.getAssociatedAirportIdent(fac.icao) === '');
        return facilities;
    }
}
FacilitySearchUtils.INSTANCE = null;
