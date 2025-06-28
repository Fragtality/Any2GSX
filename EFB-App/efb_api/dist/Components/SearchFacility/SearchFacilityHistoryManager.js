import { ArraySubject, DataStore, ICAO } from '@microsoft/msfs-sdk';
export class SearchFacilityHistoryManager {
    constructor() {
        this.MAX_ITEMS_STORED = 5;
        this.storedICAOs = ArraySubject.create();
        this.loadICAOsFromStorage();
    }
    /**
     * Retrieves all the stored recent searches
     */
    loadICAOsFromStorage() {
        let arrayICAOs = [];
        const stringICAOs = DataStore.get(SearchFacilityHistoryManager.DATASTORE_KEY);
        if (stringICAOs === undefined || typeof stringICAOs !== 'string') {
            this.storedICAOs.set(arrayICAOs);
            return;
        }
        try {
            arrayICAOs = JSON.parse(stringICAOs);
        }
        catch (_a) {
            console.error('JSON failed, impossible to parse : ', stringICAOs);
        }
        this.storedICAOs.set(arrayICAOs);
    }
    saveICAOsToStorage() {
        const stringICAOs = JSON.stringify(this.storedICAOs.getArray());
        DataStore.set(SearchFacilityHistoryManager.DATASTORE_KEY, stringICAOs);
    }
    mostRecentSearch(icao) {
        const foundIndex = this.storedICAOs.getArray().findIndex((storedIcao) => storedIcao === icao);
        // if the new ICAO is already in the recent searches, remove it
        if (foundIndex !== -1) {
            this.storedICAOs.removeAt(foundIndex);
        }
        // add the new ICAO on top of the recent searches
        this.storedICAOs.insert(icao, 0);
        // verify that we don't have more than the maximum items stored
        while (this.storedICAOs.length > this.MAX_ITEMS_STORED) {
            this.storedICAOs.removeAt(this.MAX_ITEMS_STORED);
        }
        // save the new recent searches to the data store
        this.saveICAOsToStorage();
    }
    /**
     * Retrieve the search facility history as an array
     * @param input the input of the search
     * @param max_items the maximum number of items returned
     * @returns the recent searches as an array of icaos
     */
    getStoredICAOs(input, max_items = this.MAX_ITEMS_STORED) {
        this.loadICAOsFromStorage();
        return (this.storedICAOs
            .getArray()
            // TODO : filter on ICAO, name, etc
            .filter((icao) => ICAO.getIdent(icao).startsWith(input.toUpperCase()))
            .slice(0, max_items));
    }
}
SearchFacilityHistoryManager.DATASTORE_KEY = 'efb.search-bar-history';
