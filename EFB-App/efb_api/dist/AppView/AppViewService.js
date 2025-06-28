/** A service to manager an application views  */
import { ArrayUtils, DisplayComponent, FSComponent, Subject, SubscribableMapFunctions, } from '@microsoft/msfs-sdk';
import { GamepadUiView } from '../UiView';
import { ViewBootMode, ViewSuspendMode } from '../UiView/UiViewLifecycle';
import { value } from '../utils';
import { toString, where } from '../sub';
/**
 * @param BootMode defaults to {@link ViewBootMode} COLD
 * @param SuspendMode defaults to {@link ViewSuspendMode} SLEEP
 */
const defaultViewOptions = {
    BootMode: ViewBootMode.COLD,
    SuspendMode: ViewSuspendMode.SLEEP,
};
/**
 * Handle and track page/popup usage for an App
 */
export class AppViewService {
    constructor(bus) {
        /** Keeps track of registered views with the {@link registerView} method  */
        this.registeredUiViewEntries = new Map();
        /** keeps track of {@link UiViewEntry} history */
        this.appViewStack = [];
        this.registeredUiViewPromises = [];
        this.hasInitialized = false;
        this._currentUiView = Subject.create(null);
        /** The page that is currently open in the active view stack. */
        this.currentUiView = this._currentUiView;
        this.eventListener = {
            pause: [],
            resume: [],
        };
        this.bus = bus;
        this.goHomeSub = this.bus.on('_efb_appviewservice_go_home', () => {
            const steps = this.appViewStack.length - 1;
            if (steps >= 1) {
                this.goBack(steps);
            }
        }, true);
    }
    onOpen() {
        var _a, _b;
        this.goHomeSub.resume();
        (_b = (_a = this.activeUiViewEntry) === null || _a === void 0 ? void 0 : _a.ref) === null || _b === void 0 ? void 0 : _b.onOpen();
    }
    onClose() {
        var _a, _b;
        this.goHomeSub.pause();
        (_b = (_a = this.activeUiViewEntry) === null || _a === void 0 ? void 0 : _a.ref) === null || _b === void 0 ? void 0 : _b.onClose();
    }
    onResume() {
        var _a, _b;
        this.goHomeSub.resume();
        (_b = (_a = this.activeUiViewEntry) === null || _a === void 0 ? void 0 : _a.ref) === null || _b === void 0 ? void 0 : _b.onResume();
    }
    onPause() {
        var _a, _b;
        this.goHomeSub.pause();
        (_b = (_a = this.activeUiViewEntry) === null || _a === void 0 ? void 0 : _a.ref) === null || _b === void 0 ? void 0 : _b.onPause();
    }
    /**
     * Registers and renders a view (page or popup) to be opened by the service.
     * @param key The UiView string key.
     * @param type The view type
     * @param vNodeFactory A function that returns a {@link UiView} VNode for the key
     * @param options The {@link UiView} {@link ViewOptions}
     * @returns UiViewEntry
     */
    registerView(key, type, vNodeFactory, options) {
        if (this.registeredUiViewEntries.has(key)) {
            throw new Error(`View "${key}" is already used`);
        }
        else if (typeof vNodeFactory !== 'function') {
            throw new Error('vNodeFactory has to be a function returning a VNode');
        }
        const viewOptions = Object.assign({}, defaultViewOptions, options);
        const appViewEntry = {
            key,
            render: vNodeFactory,
            vNode: vNodeFactory,
            ref: null,
            containerRef: FSComponent.createRef(),
            isVisible: Subject.create(false),
            layer: Subject.create(-1),
            type: Subject.create(type),
            isInit: false,
            viewOptions,
        };
        if (viewOptions.BootMode === ViewBootMode.HOT) {
            this.initViewEntry(appViewEntry);
        }
        this.registeredUiViewEntries.set(key, appViewEntry);
        return appViewEntry;
    }
    registerPage(key, vNodeFactory, options) {
        return this.registerView(key, 'page', vNodeFactory, options);
    }
    registerPopup(key, vNodeFactory, options) {
        return this.registerView(key, 'popup', vNodeFactory, options);
    }
    async killView(viewEntry) {
        var _a;
        if (viewEntry.isInit === false) {
            return;
        }
        if (this.appViewStack.find((view) => view.key === viewEntry.key)) {
            throw new Error('You must close your view before killing it');
        }
        viewEntry.isInit = false;
        (_a = viewEntry.ref) === null || _a === void 0 ? void 0 : _a.destroy();
        viewEntry.containerRef.instance.destroy();
        viewEntry.ref = null;
        viewEntry.containerRef = FSComponent.createRef();
        viewEntry.vNode = viewEntry.render.bind(viewEntry);
        return new Promise((resolve) => {
            if (viewEntry.viewOptions.BootMode === ViewBootMode.HOT) {
                this.initViewEntry(viewEntry);
            }
            resolve();
        });
    }
    /**
     * @param entry a {@link UiViewEntry}
     * @param shouldOpen opens the view on initialization, defaults to false
     */
    initViewEntry(entry, shouldOpen = false) {
        var _a;
        if (entry.isInit) {
            return;
        }
        entry.isInit = true;
        entry.vNode = value(entry.vNode);
        entry.ref = entry.vNode.instance;
        (_a = this.appViewRef) === null || _a === void 0 ? void 0 : _a.renderView(FSComponent.buildComponent(AppViewWrapper, { viewName: entry.key, isVisible: entry.isVisible, type: entry.type, layer: entry.layer, ref: entry.containerRef }, entry.vNode));
        shouldOpen && entry.ref.onOpen();
    }
    /**
     * Destroys every view in registered view entries and resets the view stack.
     */
    unload() {
        var _a;
        this.registeredUiViewPromises = [];
        for (const viewEntry of this.registeredUiViewEntries.values()) {
            (_a = viewEntry.ref) === null || _a === void 0 ? void 0 : _a.destroy();
        }
        this.registeredUiViewEntries.clear();
        this.appViewStack.splice(0, this.appViewStack.length);
        this.hasInitialized = false;
    }
    /**
     *
     * @param homePageUiViewKey the string key of the {@link UiView}
     * @returns a Promise resolving when all pages are initialized
     */
    async initialize(homePageUiViewKey) {
        if (this.hasInitialized) {
            return Promise.resolve();
        }
        return Promise.all(this.registeredUiViewPromises).then(() => {
            // Populate the view stack with its respective home page
            this.initializeAppViewStack(homePageUiViewKey);
            this.hasInitialized = true;
        });
    }
    /**
     * @returns the current active view entry.
     */
    get activeUiViewEntry() {
        const activeViewEntry = ArrayUtils.peekLast(this.appViewStack);
        /*if (!activeViewEntry) {
            throw new Error('AppViewService: attempting to access the top view of an empty view stack');
        }*/
        return activeViewEntry;
    }
    on(event, viewKey, callback) {
        if (!this.registeredUiViewEntries.has(viewKey)) {
            console.error('Cannot listen for a non registered view.');
            return this;
        }
        this.eventListener[event].push({ key: viewKey, callback });
        return this;
    }
    /**
     * Handles logic associated with changing the open page.
     * @param page Page to close
     */
    handleCloseView(page) {
        var _a;
        if (!page)
            return;
        (_a = page.ref) === null || _a === void 0 ? void 0 : _a.onPause();
        this.eventListener.pause.forEach((e) => e.key === page.key && e.callback(page));
        page.isVisible.set(false);
        this._currentUiView.set(null);
        if (page.viewOptions.SuspendMode === ViewSuspendMode.TERMINATE) {
            setTimeout(() => this.killView(page));
        }
    }
    /**
     * Handles logic associated with changing the open page.
     * @param page Page to open
     */
    handleOpenView(page) {
        var _a;
        if (!page)
            return;
        (_a = page.ref) === null || _a === void 0 ? void 0 : _a.onResume();
        this.eventListener.resume.forEach((e) => e.key === page.key && e.callback(page));
        page.isVisible.set(true);
        this._currentUiView.set(page);
    }
    /**
     * Populate the view stack with its respective home page.
     * @param mainPageUiViewKey the key of the home page
     */
    initializeAppViewStack(mainPageUiViewKey) {
        this.open(mainPageUiViewKey);
    }
    /**
     * @param key the {@link UiView} string key
     * @returns the {@link UiViewEntry} corresponding to the key
     * @throws if the {@link UiViewEntry} doesn't exists
     */
    getUiViewEntry(key) {
        const appViewEntry = this.registeredUiViewEntries.get(key);
        if (!appViewEntry) {
            throw new Error(`${key} wasn't registered as a view`);
        }
        return appViewEntry;
    }
    /**
     * Called by AppContainer to pass in the refs to the view.
     * Should only be called once.
     * @param appViewRef The app view ref.
     */
    onAppContainerRendered(appViewRef) {
        this.appViewRef = appViewRef;
    }
    /**
     * Opens a {@link UiView} as a page and adds it to the stack.
     * @param key The UiView string key
     * @returns the {@link PublicUiViewEntry} entry of the {@link UiView}
     * @deprecated This method has been deprecated in favor of using {@link AppViewService.open}.
     */
    openPage(key) {
        return this.open(key);
    }
    /**
     * Opens a {@link UiView} as a popup.
     * The {@link UiView} will be brought to the top of the current view stack as the active view
     * @param key The UiView string key
     * @returns the {@link PublicUiViewEntry} entry of the {@link UiView}
     * @deprecated This method has been deprecated in favor of using {@link AppViewService.open}.
     */
    openPopup(key) {
        return this.open(key);
    }
    open(key) {
        const viewEntry = this.advanceViewStack(key);
        return viewEntry;
    }
    goBack(steps) {
        steps !== null && steps !== void 0 ? steps : (steps = 1);
        if (steps <= 0) {
            throw new RangeError(`Steps must be superior to 0.`);
        }
        else if (steps >= this.appViewStack.length) {
            throw new RangeError(`Steps can't be superior to ${this.appViewStack.length} when called.`);
        }
        const activeViewEntry = this.activeUiViewEntry;
        if (this.appViewStack.length > steps && activeViewEntry) {
            this.eventListener.pause.forEach((e) => e.key === activeViewEntry.key && e.callback(activeViewEntry));
            const viewsToOpen = [];
            const viewsToClose = [];
            while (steps--) {
                const view = this.appViewStack.pop();
                if (view) {
                    viewsToClose.push(view);
                }
            }
            // Get all views from the end of stack to the first page we found
            let i = this.appViewStack.length;
            do {
                viewsToOpen.push(this.appViewStack[--i]);
            } while (i > 0 && this.appViewStack[i].type.get() !== 'page');
            for (const page of viewsToClose) {
                this.handleCloseView(page);
            }
            // Open views from page to popups
            for (const page of viewsToOpen.reverse()) {
                this.handleOpenView(page);
            }
        }
        return this.activeUiViewEntry;
    }
    /**
     * Handles view stack logic
     * @param key the {@link UiView} string key
     * @returns the current {@link UiViewEntry}
     */
    advanceViewStack(key) {
        const viewEntry = this.getUiViewEntry(key);
        if (this.appViewStack.includes(viewEntry)) {
            if (this.activeUiViewEntry !== viewEntry) {
                throw new Error("Page or popup is already in the viewstack and can't be opened twice");
            }
            return viewEntry;
        }
        switch (viewEntry.type.get()) {
            case 'page':
                for (const page of [...this.appViewStack].reverse()) {
                    this.handleCloseView(page);
                }
                break;
            case 'popup':
                break;
        }
        this.appViewStack.push(viewEntry);
        this.openView(viewEntry);
        this.handleOpenView(viewEntry);
        return viewEntry;
    }
    /**
     * Handle logic associated with opening a view
     * @param view the view to open.
     */
    openView(view) {
        if (!view.isInit) {
            this.initViewEntry(view, true);
        }
        const index = this.appViewStack.indexOf(view);
        if (index < 0) {
            throw new Error(`AppViewService: view not found in view stack: ${view.key}`);
        }
        view.layer.set(index);
    }
    /**
     * Updates all the pages/popups that are initialized and visible
     * @param time timestamp
     */
    update(time) {
        this.registeredUiViewEntries.forEach((UiView) => {
            var _a;
            if (UiView.isInit && UiView.isVisible.get()) {
                (_a = UiView.ref) === null || _a === void 0 ? void 0 : _a.onUpdate(time);
            }
        });
    }
    /**
     * Routes the event to the current {@link UiView}
     * @param gamepadEvent the {@link GamepadEvents}
     */
    routeGamepadInteractionEvent(gamepadEvent) {
        const _currentUiView = this.activeUiViewEntry;
        if ((_currentUiView === null || _currentUiView === void 0 ? void 0 : _currentUiView.ref) instanceof GamepadUiView) {
            _currentUiView.ref.handleGamepadEvent(gamepadEvent);
        }
    }
}
class AppViewWrapper extends DisplayComponent {
    constructor() {
        super(...arguments);
        this.rootRef = FSComponent.createRef();
    }
    render() {
        return (FSComponent.buildComponent("div", { ref: this.rootRef, class: {
                'ui-view': true,
                [this.props.viewName]: true,
                hidden: this.props.isVisible.map(SubscribableMapFunctions.not()),
                page: this.props.type.map(where('page')),
                popup: this.props.type.map(where('popup')),
            }, style: {
                'z-index': this.props.layer.map(toString()),
            } }, this.props.children));
    }
    destroy() {
        var _a;
        const root = this.rootRef.getOrDefault();
        if (root !== null) {
            (_a = root.parentNode) === null || _a === void 0 ? void 0 : _a.removeChild(root);
        }
        super.destroy();
    }
}
