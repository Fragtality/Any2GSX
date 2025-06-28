import { ArraySubject, NodeReference, Subject, Subscription, VNode } from '@microsoft/msfs-sdk';
import { GamepadUiComponent, GamepadUiComponentProps } from '../../Gamepad';
import { TVNode } from '../../types';
import { TextBox, TextBoxProps } from '../Input';

export interface SearchBarProps<T> extends TextBoxProps, GamepadUiComponentProps {
    /** Component ref to the text box */
    textBoxRef?: NodeReference<TextBox>;
    /** Component ref to the search list container */
    searchBarListRef?: NodeReference<HTMLDivElement>;
    /** The callback that renders the list items */
    renderItem?: (data: T) => VNode;
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
    protected readonly onInputSearchSub: Subject<string>;
    protected readonly textBoxRef: NodeReference<TextBox>;
    protected readonly searchBarListRef: NodeReference<HTMLDivElement>;
    protected readonly isSearchBarFocus: Subject<boolean>;
    protected readonly placeholder: import('../../types').MaybeSubscribable<string>;
    protected DEBOUNCE_DURATION: number;
    protected readonly subs: Subscription[];
    protected currentSearchId: number;
    protected tryRenderItem(data: T): VNode;
    protected renderItem(_data: T): VNode;
    protected tryUpdateResultItems(input: string): Promise<readonly T[]>;
    protected updateResultItems(_input: string): Promise<readonly T[]>;
    protected onSearchUpdated(input: string): Promise<void>;
    onResultItemsUpdated(): void;
    protected readonly onDelete: () => void;
    protected readonly prefix: Subject<string | VNode>;
    protected readonly suffix: Subject<string | VNode>;
    render(): TVNode<SearchBar<T>>;
    onAfterRender(node: VNode): void;
    destroy(): void;
}
//# sourceMappingURL=SearchBar.d.ts.map