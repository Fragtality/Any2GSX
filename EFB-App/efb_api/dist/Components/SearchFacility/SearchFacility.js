import { ArraySubject, FacilityLoader, FacilityRepository, FacilitySearchType, FacilityType, FSComponent, GeoPoint, ICAO, MappedSubject, Subject, } from '@microsoft/msfs-sdk';
import { TT } from '../../i18n';
import { unique } from '../../utils';
import { FacilitySearchUtils } from '../../utils/FacilitySearchUtils';
import { createCustomFacility, getFacilityIconPath } from '../../utils/FacilityUtils';
import { IconElement } from '../IconElement';
import { SearchBar } from '../SearchBar';
import { FacilityResultItem } from './FacilityResultItem';
import { SearchFacilityHistoryManager } from './SearchFacilityHistoryManager';
/** A UI component to look for facilities  */
export class SearchFacility extends SearchBar {
    constructor(props) {
        super(props);
        this.facilityLoader = new FacilityLoader(FacilityRepository.getRepository(this.props.bus));
        /** Only used if `selectedFacilitySub` prop was not passed in. */
        this.internalSelectedFacilitySub = Subject.create(null);
        this.selectedFacilitySub = this.props.selectedFacilitySub || this.internalSelectedFacilitySub;
        this.selectedFacility = null;
        this.recentSearchesICAO = ArraySubject.create();
        this.historyManager = new SearchFacilityHistoryManager();
        this.ppos = new GeoPoint(0, 0);
        this.hideCustoms = MappedSubject.create(([focus, input]) => !focus || input.length !== 0, this.isSearchBarFocus, this.onInputSearchSub);
        this.customItemSelectRef = FSComponent.createRef();
        this.customItemPositionRef = FSComponent.createRef();
        this.onDelete = () => {
            var _a, _b;
            (_b = (_a = this.props).onDelete) === null || _b === void 0 ? void 0 : _b.call(_a);
            this.prefix.set('');
        };
        props.bus
            .getSubscriber()
            .on('gps-position')
            .atFrequency(1)
            .handle((pos) => this.ppos.set(pos.lat, pos.long));
    }
    renderItem(data) {
        return FSComponent.buildComponent(FacilityResultItem, { facility: data, separator: ", ", callback: this.itemCallback.bind(this) });
    }
    itemCallback(data) {
        this.selectedFacility = data;
        if (this.props.onFacilitySelectionFormatter) {
            this.onInputSearchSub.set(this.props.onFacilitySelectionFormatter(data));
        }
        this.prefix.set(FSComponent.buildComponent(IconElement, { url: getFacilityIconPath(ICAO.getFacilityType(data.icao)) }));
        this.props.onFacilityClick(data);
        if (ICAO.getFacilityType(data.icao) !== FacilityType.USR) {
            this.historyManager.mostRecentSearch(data.icao);
        }
        this.textBoxRef.instance.inputRef.instance.blur();
    }
    async updateResultItems(input) {
        const searchInput = input.trim();
        const facilities = [];
        // Load recent searches filtered by facility type
        const recentSearchs = (await this.updateRecentSearches(searchInput)).filter((e) => {
            if (e === null) {
                return false;
            }
            switch (this.props.facilitySearchType) {
                case FacilitySearchType.Airport:
                    return ICAO.getFacilityType(e.icao) === FacilityType.Airport;
                case FacilitySearchType.Intersection:
                    return ICAO.getFacilityType(e.icao) === FacilityType.Intersection;
                case FacilitySearchType.Vor:
                    return ICAO.getFacilityType(e.icao) === FacilityType.VOR;
                case FacilitySearchType.Ndb:
                    return ICAO.getFacilityType(e.icao) === FacilityType.NDB;
                case FacilitySearchType.User:
                    return ICAO.getFacilityType(e.icao) === FacilityType.USR;
                case FacilitySearchType.Visual:
                    return ICAO.getFacilityType(e.icao) === FacilityType.VIS;
                case FacilitySearchType.AllExceptVisual:
                    return ICAO.getFacilityType(e.icao) !== FacilityType.VIS;
                case FacilitySearchType.Boundary:
                case FacilitySearchType.All:
                default:
                    return true;
            }
        }); // `as Facility[]` can be remove if using Typescript 5.5+ (https://devblogs.microsoft.com/typescript/announcing-typescript-5-5-beta/#inferred-type-predicates)
        facilities.push(...recentSearchs);
        if (searchInput.length > 0) {
            // search facilities and filter to avoid duplicates between history and search results
            facilities.push(...(await FacilitySearchUtils.getSearchUtils(this.props.bus).loadFacilities(searchInput, this.props.facilitySearchType)));
        }
        return new Promise((resolve) => resolve(unique(facilities)));
    }
    async updateRecentSearches(input) {
        const facilitiesPromises = this.historyManager
            .getStoredICAOs(input, this.props.maxHistoryItems)
            .map((icao) => {
            return this.facilityLoader.getFacility(ICAO.getFacilityType(icao), icao).catch(() => {
                return null;
            });
        });
        return Promise.all(facilitiesPromises);
    }
    resetInput() {
        var _a, _b;
        if (this.selectedFacilitySub.get() !== null) {
            this.onInputSearchSub.set('');
            // As selectedFacilitySub is no longer mutable, instead invoke the onResetInput
            // callback to handle resetting the value to some default state.
            (_b = (_a = this.props).onResetInput) === null || _b === void 0 ? void 0 : _b.call(_a);
            this.internalSelectedFacilitySub.set(null);
            this.prefix.set('');
        }
    }
    restoreInput() {
        const inputHasBeenCleared = !this.onInputSearchSub.get();
        if (this.selectedFacility && inputHasBeenCleared) {
            this.itemCallback(this.selectedFacility);
        }
    }
    onAfterRender(node) {
        var _a;
        super.onAfterRender(node);
        this.selectedFacilitySubscription = (_a = this.props.selectedFacilitySub) === null || _a === void 0 ? void 0 : _a.sub((facility) => {
            var _a, _b;
            const localFacility = this.selectedFacilitySub.get();
            this.selectedFacility = facility;
            if ((facility === null || facility === void 0 ? void 0 : facility.icao) !== (localFacility === null || localFacility === void 0 ? void 0 : localFacility.icao) || this.props.selectedFacilitySub === this.selectedFacilitySub) {
                this.onInputSearchSub.set(facility ? ((_b = (_a = this.props).onFacilitySelectionFormatter) === null || _b === void 0 ? void 0 : _b.call(_a, facility)) || facility.icao : '');
            }
        }, true);
        this.subs.push(this.isSearchBarFocus.sub((focus) => {
            var _a, _b;
            (_b = (_a = this.props).onTextBoxFocused) === null || _b === void 0 ? void 0 : _b.call(_a, focus);
            // on focus, update the search in order to update the search history
            if (focus) {
                this.onSearchUpdated(this.onInputSearchSub.get());
            }
        }));
        if (this.props.positionSelectable) {
            FSComponent.render(FSComponent.buildComponent("div", { class: { 'search-facility-custom-items': true, hide: this.hideCustoms } },
                FSComponent.buildComponent("div", { class: "facility-result-item", ref: this.customItemPositionRef },
                    FSComponent.buildComponent("div", { class: "icon-container" },
                        FSComponent.buildComponent("icon-element", { "icon-url": `coui://html_ui/efb_ui/efb_os/Assets/icons/facilities/Challenges.svg` })),
                    FSComponent.buildComponent(TT, { key: "@fs-base-efb-app-navigation-map,TT:EFB.NAVIGATION_MAP.YOUR_POSITION", format: "ucfirst" }))), this.searchBarListRef.instance, 0);
            this.customItemPositionRef.instance.onmousedown = () => {
                this.itemCallback(createCustomFacility(FacilityRepository.getRepository(this.props.bus), this.ppos.lat, this.ppos.lon));
            };
        }
        // Load the search history for the first focus
        this.onSearchUpdated('');
    }
    destroy() {
        var _a;
        (_a = this.selectedFacilitySubscription) === null || _a === void 0 ? void 0 : _a.destroy();
        super.destroy();
    }
}
