import { ArraySubject, EventBus, Facility, FacilityLoader, FacilitySearchType, GeoPoint, MappedSubject, Subject, Subscribable, VNode } from '@microsoft/msfs-sdk';
import { NullFacility } from '../../utils/FacilityUtils';
import { SearchBar, SearchBarProps } from '../SearchBar';
import { SearchFacilityHistoryManager } from './SearchFacilityHistoryManager';

/** The SearchFacility props */
interface SearchFacilityProps extends SearchBarProps<Facility> {
    /** The event bus */
    bus: EventBus;
    /** A callback executed when a facility is selected */
    onFacilityClick: (facility: Facility | null) => void;
    /** A formatter to modify text output in textbox  */
    onFacilitySelectionFormatter?: (facility: Facility) => string;
    onTextBoxFocused?: (state: boolean) => void;
    selectedFacilitySub?: Subscribable<NullFacility>;
    /**
     * An optional function to call that will reset the value of the `selectedFacilitySub`
     * when `resetInput()` is called on the `SearchFacility` instance.
     */
    onResetInput?: () => void;
    maxHistoryItems?: number;
    positionSelectable?: boolean;
    /** Defaults to all. */
    facilitySearchType?: FacilitySearchType;
    /** Defaults to true. */
    excludeTerminalFacilities?: boolean;
}
/** A UI component to look for facilities  */
export declare class SearchFacility extends SearchBar<Facility, SearchFacilityProps> {
    protected readonly facilityLoader: FacilityLoader;
    /** Only used if `selectedFacilitySub` prop was not passed in. */
    private readonly internalSelectedFacilitySub;
    private readonly selectedFacilitySub;
    private selectedFacility;
    protected readonly recentSearchesICAO: ArraySubject<Facility>;
    protected readonly historyManager: SearchFacilityHistoryManager;
    protected DEBOUNCE_DURATION: number;
    readonly ppos: GeoPoint;
    private selectedFacilitySubscription?;
    protected readonly currentHoveredItemIndex: Subject<number>;
    protected readonly gamepadNavigationOptions: {
        onItemHovered: (item: Facility | null, index: number) => void;
        onItemSelected: (item: Facility, index: number) => void;
        isListFocused?: import('@microsoft/msfs-sdk').MutableSubscribable<boolean> | import('../..').MaybeSubscribable<boolean>;
        navigationBetweenItemsDelayMs?: number;
        wrap?: boolean;
    };
    constructor(props: SearchFacilityProps);
    protected hideCustoms: MappedSubject<[boolean, string], boolean>;
    protected customItemSelectRef: import('@microsoft/msfs-sdk').NodeReference<HTMLDivElement>;
    protected customItemPositionRef: import('@microsoft/msfs-sdk').NodeReference<HTMLDivElement>;
    protected renderItem(data: Facility, index: number): VNode;
    protected itemCallback(data: Facility): void;
    protected updateIcon(fac: Facility): void;
    protected updateResultItems(input: string): Promise<readonly Facility[]>;
    protected updateRecentSearches(input: string): Promise<readonly NullFacility[]>;
    resetInput(): void;
    restoreInput(): void;
    protected readonly onDelete: () => void;
    onAfterRender(node: VNode): void;
    destroy(): void;
}
export {};
