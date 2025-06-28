import { ArraySubject, DebounceTimer, FSComponent, Subject, SubscribableUtils, Wait, } from '@microsoft/msfs-sdk';
import { GamepadUiComponent } from '../../Gamepad';
import { TextBox } from '../Input';
import { List } from '../List';
export class SearchBar extends GamepadUiComponent {
    constructor() {
        var _a, _b, _c;
        super(...arguments);
        this.resultItems = ArraySubject.create([]);
        this.onInputSearchSub = Subject.create('');
        this.textBoxRef = (_a = this.props.textBoxRef) !== null && _a !== void 0 ? _a : FSComponent.createRef();
        this.searchBarListRef = (_b = this.props.searchBarListRef) !== null && _b !== void 0 ? _b : FSComponent.createRef();
        this.isSearchBarFocus = Subject.create(false);
        this.placeholder = (_c = this.props.placeholder) !== null && _c !== void 0 ? _c : '@fs-base-efb,TT:EFB.COMMON.SEARCH_PLACEHOLDER';
        this.subs = [];
        this.debounce = new DebounceTimer();
        this.onDelete = () => {
            //
        };
        this.prefix = Subject.create(SubscribableUtils.toSubscribable(this.props.prefix || '', true).get());
        this.suffix = Subject.create(SubscribableUtils.toSubscribable(this.props.suffix || '', true).get());
    }
    tryRenderItem(data) {
        return this.props.renderItem ? this.props.renderItem(data) : this.renderItem(data);
    }
    /* Must be implemented by children classes */
    renderItem(_data) {
        throw new Error('Missing renderItem props');
    }
    tryUpdateResultItems(input) {
        return this.props.updateResultItems ? this.props.updateResultItems(input) : this.updateResultItems(input);
    }
    /* Must be implemented by children classes */
    updateResultItems(_input) {
        throw new Error('Missing updateResultItems props');
    }
    onSearchUpdated(input) {
        var _a;
        this.debounce.schedule(() => {
            this.tryUpdateResultItems(input).then((values) => {
                this.resultItems.set(values);
            });
        }, (_a = this.props.debounce) !== null && _a !== void 0 ? _a : SearchBar.DEBOUNCE_TIME);
    }
    onResultItemsUpdated() {
        this.onInputSearchSub.notify();
    }
    render() {
        var _a;
        return (FSComponent.buildComponent("div", { class: "search-bar", ref: this.gamepadUiComponentRef },
            FSComponent.buildComponent(TextBox, { model: this.onInputSearchSub, ref: this.textBoxRef, placeholder: this.placeholder, disabled: this.props.disabled, hidePlaceholderOnFocus: this.props.hidePlaceholderOnFocus, focusOnInit: this.props.focusOnInit, onFocusIn: () => {
                    var _a, _b;
                    Wait.awaitFrames(1)
                        .catch()
                        .then(() => this.isSearchBarFocus.set(true));
                    (_b = (_a = this.props).onFocusIn) === null || _b === void 0 ? void 0 : _b.call(_a);
                }, onFocusOut: () => {
                    var _a, _b;
                    Wait.awaitFrames(1)
                        .catch()
                        .then(() => this.isSearchBarFocus.set(false));
                    (_b = (_a = this.props).onFocusOut) === null || _b === void 0 ? void 0 : _b.call(_a);
                    // Clear input if emptySearchOnFocusOut is true or if input only contains whitespace
                    if (this.props.emptySearchOnFocusOut || !this.onInputSearchSub.get().trim()) {
                        this.textBoxRef.instance.inputRef.instance.clearInput();
                    }
                }, onInput: this.props.onInput, onKeyPress: this.props.onKeyPress, charFilter: this.props.charFilter, onDelete: this.onDelete, focusOnClear: this.props.focusOnClear, showDeleteIcon: (_a = this.props.showDeleteIcon) !== null && _a !== void 0 ? _a : true, customDeleteIcon: this.props.customDeleteIcon, prefix: this.prefix, suffix: this.suffix }),
            FSComponent.buildComponent("div", { class: "search-bar-list-container", ref: this.searchBarListRef },
                FSComponent.buildComponent(List, { data: this.resultItems, isListVisible: this.isSearchBarFocus, isScrollable: true, customClass: "search-bar-list", renderItem: this.tryRenderItem.bind(this) }))));
    }
    onAfterRender(node) {
        super.onAfterRender(node);
        if (this.props.emptySearchOnInit) {
            this.onSearchUpdated('');
        }
        this.subs.push(this.onInputSearchSub.sub((value) => {
            this.onSearchUpdated(value);
        }));
        if (this.props.onListDisplayed !== undefined) {
            this.subs.push(this.isSearchBarFocus.sub((focus) => {
                var _a, _b;
                if (focus) {
                    (_b = (_a = this.props).onListDisplayed) === null || _b === void 0 ? void 0 : _b.call(_a);
                }
            }));
        }
    }
    destroy() {
        this.subs.forEach((s) => s.destroy());
        super.destroy();
    }
}
SearchBar.DEBOUNCE_TIME = 300;
