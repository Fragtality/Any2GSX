import { ArraySubject } from '@microsoft/msfs-sdk';

export declare class SearchFacilityHistoryManager {
    private static readonly DATASTORE_KEY;
    private readonly MAX_ITEMS_STORED;
    protected storedICAOs: ArraySubject<string>;
    constructor();
    /**
     * Retrieves all the stored recent searches
     */
    protected loadICAOsFromStorage(): void;
    protected saveICAOsToStorage(): void;
    mostRecentSearch(icao: string): void;
    /**
     * Retrieve the search facility history as an array
     * @param input the input of the search
     * @param max_items the maximum number of items returned
     * @returns the recent searches as an array of icaos
     */
    getStoredICAOs(input: string, max_items?: number): readonly string[];
}
