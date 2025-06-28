import { DisplayComponent, FSComponent, Subject } from '@microsoft/msfs-sdk';
import { MapSubject } from '../sub';
import { ViewStackContainer } from './ViewContainer';
import { ViewWrapper } from './ViewWrapper';
export class ViewService {
    constructor(viewKey, appViewService) {
        this.registeredViews = MapSubject.create();
        this.hasInitialized = false;
        this.activeViewEntry = Subject.create(null);
        if (viewKey && appViewService) {
            appViewService
                .on('pause', viewKey, () => {
                var _a;
                const activeViewEntry = this.activeViewEntry.get();
                if ((activeViewEntry === null || activeViewEntry === void 0 ? void 0 : activeViewEntry.isActive.get()) === true) {
                    activeViewEntry.isActive.set(false);
                    (_a = activeViewEntry.ref) === null || _a === void 0 ? void 0 : _a.onPause();
                }
            })
                .on('resume', viewKey, () => {
                var _a;
                const activeViewEntry = this.activeViewEntry.get();
                if ((activeViewEntry === null || activeViewEntry === void 0 ? void 0 : activeViewEntry.isActive.get()) === false) {
                    activeViewEntry.isActive.set(true);
                    (_a = activeViewEntry.ref) === null || _a === void 0 ? void 0 : _a.onResume();
                }
            });
        }
    }
    getRegisteredViews() {
        return this.registeredViews;
    }
    getViewEntry(key) {
        const viewEntry = this.registeredViews.get(key);
        if (viewEntry === undefined) {
            throw new Error(`View "${key}" doesn't exists`);
        }
        return viewEntry;
    }
    registerView(key, vNodeFactory) {
        if (this.registeredViews.has(key)) {
            throw new Error(`View "${key}" is already used`);
        }
        const isActive = Subject.create(false);
        const isDisabled = Subject.create(false);
        const isTabVisible = Subject.create(true);
        const isInit = false;
        const viewEntry = {
            key,
            render: vNodeFactory,
            vNode: null,
            containerRef: FSComponent.createRef(),
            ref: null,
            isActive,
            isDisabled,
            isTabVisible,
            isInit,
        };
        this.initViewEntry(viewEntry);
        this.registeredViews.set(key, viewEntry);
        return viewEntry;
    }
    onContainerRendered(viewRef) {
        this.viewRef = viewRef;
    }
    initViewEntry(entry) {
        var _a;
        if (entry.isInit) {
            return;
        }
        entry.isInit = true;
        entry.vNode = entry.render();
        entry.ref = entry.vNode.instance;
        (_a = this.viewRef) === null || _a === void 0 ? void 0 : _a.renderView(FSComponent.buildComponent(ViewWrapper, { viewName: entry.key, isActive: entry.isActive, ref: entry.containerRef }, entry.vNode));
    }
    initialize(key) {
        var _a;
        if (this.hasInitialized) {
            return;
        }
        this.hasInitialized = true;
        if (key) {
            const initViewEntry = this.getViewEntry(key);
            (_a = initViewEntry.ref) === null || _a === void 0 ? void 0 : _a.onOpen();
            initViewEntry.isActive.set(true);
            this.activeViewEntry.set(initViewEntry);
        }
    }
    openPage(key) {
        var _a, _b;
        const activeViewEntry = this.activeViewEntry.get();
        if ((activeViewEntry === null || activeViewEntry === void 0 ? void 0 : activeViewEntry.key) === key) {
            return activeViewEntry;
        }
        activeViewEntry === null || activeViewEntry === void 0 ? void 0 : activeViewEntry.isActive.set(false);
        (_a = activeViewEntry === null || activeViewEntry === void 0 ? void 0 : activeViewEntry.ref) === null || _a === void 0 ? void 0 : _a.onPause();
        const newViewEntry = this.getViewEntry(key);
        newViewEntry.isActive.set(true);
        (_b = newViewEntry.ref) === null || _b === void 0 ? void 0 : _b.onResume();
        this.activeViewEntry.set(newViewEntry);
        return newViewEntry;
    }
    onUpdate(time) {
        var _a, _b;
        (_b = (_a = this.activeViewEntry.get()) === null || _a === void 0 ? void 0 : _a.ref) === null || _b === void 0 ? void 0 : _b.onUpdate(time);
    }
}
export class ViewServiceContainer extends DisplayComponent {
    constructor() {
        super(...arguments);
        this.stackContainerRef = FSComponent.createRef();
    }
    onAfterRender(node) {
        super.onAfterRender(node);
        this.props.viewService.onContainerRendered(this.stackContainerRef.instance);
    }
    render() {
        return FSComponent.buildComponent(ViewStackContainer, { ref: this.stackContainerRef, class: this.props.class, id: this.props.id });
    }
}
