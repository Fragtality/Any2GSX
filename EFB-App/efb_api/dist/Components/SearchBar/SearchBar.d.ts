import { ArraySubject, MutableSubscribable, NodeReference, Subject, Subscribable, Subscription, VNode } from '@microsoft/msfs-sdk';
import { GamepadUiComponent, GamepadUiComponentProps } from '../../Gamepad';
import { TVNode } from '../../types';
import { TextBox, TextBoxProps } from '../Input';
import { List, ListProps } from '../List';

type SearchBarListProps<T> = Pick<ListProps<T>, 'isListVisible' | 'refreshOnUpdate' | 'gamepadNavigationOptions'>;
export interface SearchBarProps<T> extends TextBoxProps, SearchBarListProps<T>, GamepadUiComponentProps {
    /** Component ref to the text box */
    textBoxRef?: NodeReference<TextBox>;
    /** Component ref to the search list container */
    searchBarListRef?: NodeReference<HTMLDivElement>;
    /** The callback that renders the list items */
    renderItem?: (data: T, index: number) => VNode;
    /** The function that defines the rule to filter the search, with a given input */
    updateResultItems?: (input: string) => Promise<readonly T[]>;
    /** Perform an empty search immediately on init */
    emptySearchOnInit?: boolean;
    /** Perform an empty search on focus out */
    emptySearchOnFocusOut?: boolean;
    /** Page bottom */
    onListDisplayed?: () => void;
    /** The debounce time in ms */
    debounceDuration?: number;
}
export declare class SearchBar<T, P extends SearchBarProps<T> = SearchBarProps<T>> extends GamepadUiComponent<HTMLDivElement, P> {
    protected readonly resultItems: ArraySubject<T>;
    protected readonly onInputSearchSub: MutableSubscribable<string, string>;
    protected readonly textBoxRef: NodeReference<TextBox>;
    protected readonly searchBarListRef: NodeReference<HTMLDivElement>;
    protected readonly isSearchBarFocus: Subject<boolean>;
    protected readonly isResultListFocus: MutableSubscribable<boolean>;
    protected readonly isListVisible: Subscribable<boolean>;
    protected readonly placeholder: import('../../types').MaybeSubscribable<string>;
    protected DEBOUNCE_DURATION: number;
    protected readonly subs: Subscription[];
    protected currentSearchId: number;
    protected readonly isSearchPending: Subject<boolean>;
    protected readonly gamepadNavigationOptions: import('../List').ListGamepadNavigationOptions<T>;
    protected readonly askForResultNavigationSubscription: Subscription;
    constructor(props: P);
    protected enableSearchResultNavigation(): void;
    protected disableSearchResultNavigation(): void;
    protected tryRenderItem(data: T, index: number): VNode;
    protected renderItem(_data: T, _index: number): VNode;
    protected tryUpdateResultItems(input: string): Promise<readonly T[]>;
    protected updateResultItems(_input: string): Promise<readonly T[]>;
    protected onSearchUpdated(input: string): Promise<void>;
    /** @deprecated Do nothing. */
    onResultItemsUpdated(): void;
    protected readonly onDelete: () => void;
    protected readonly prefix: Subject<string | VNode>;
    protected readonly suffix: Subject<string | VNode>;
    protected readonly listRef: NodeReference<List<T>>;
    render(): TVNode<SearchBar<T>>;
    onAfterRender(node: VNode): void;
    destroy(): void;
}
export {};
