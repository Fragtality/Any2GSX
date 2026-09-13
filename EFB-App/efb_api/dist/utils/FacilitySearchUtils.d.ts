import { EventBus, Facility, FacilitySearchType } from '@microsoft/msfs-sdk';

export declare class FacilitySearchUtils {
    private static INSTANCE;
    private readonly facilityLoader;
    private readonly position;
    private constructor();
    static getSearchUtils(bus: EventBus): FacilitySearchUtils;
    orderByIdentsAndDistance(a: Facility, b: Facility): number;
    /**
     * Loads facilities based on an ident to search for, a search type and whether to exclude terminal facilities
     * @param ident The ident to search for
     * @param facilitySearchType The search type. Defaults to {@link FacilitySearchType.All}
     * @param excludeTerminalFacilities Whether to exclude terminal facilities. Defaults to `true`.
     * @returns a readonly array of facilities
     */
    loadFacilities(ident: string, facilitySearchType?: FacilitySearchType, excludeTerminalFacilities?: boolean): Promise<readonly Facility[]>;
}
