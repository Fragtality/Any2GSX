import { ArraySubject, ComputedSubject, FSComponent, Subject, Wait, } from '@microsoft/msfs-sdk';
import { GamepadUiComponent } from '../../Gamepad';
import { List } from './List';
const arrowIconPath = 'coui://html_ui/efb_ui/efb_os/Assets/icons/NoMargin/Arrow.svg';
export class PagingList extends GamepadUiComponent {
    constructor() {
        var _a;
        super(...arguments);
        this.currentData = ArraySubject.create();
        this.previousData = ArraySubject.create();
        this.nextData = ArraySubject.create();
        this.firstPage = Subject.create(true);
        this.lastPage = Subject.create(false);
        this.numberOfPages = Subject.create(1);
        this.pageSelected = (_a = this.props.pageSelected) !== null && _a !== void 0 ? _a : Subject.create(0);
        this.previousPageSelected = this.pageSelected.get();
        this.transitionWrapperRef = FSComponent.createRef();
        this.ongoingTransition = Subject.create(false);
    }
    onAfterRender(node) {
        super.onAfterRender(node);
        this.props.data.sub(() => this.updateListsData(this.pageSelected.get()));
        this.pageSelected.sub(this.onPageChanged.bind(this));
        // Init list arrays
        this.updateListsData(this.pageSelected.get());
    }
    updateListsData(pageSelected) {
        this.numberOfPages.set(Math.ceil(this.props.data.length / this.props.maxItemsPerPage));
        const start = pageSelected * this.props.maxItemsPerPage;
        const fullData = this.props.data.getArray();
        this.currentData.set(fullData.slice(start, start + this.props.maxItemsPerPage));
        if (start === 0) {
            this.previousData.clear();
            this.firstPage.set(true);
        }
        else {
            this.previousData.set(fullData.slice(start - this.props.maxItemsPerPage, start));
            this.firstPage.set(this.previousData.getArray().length === 0);
        }
        if (start + this.props.maxItemsPerPage > fullData.length) {
            this.nextData.clear();
            this.lastPage.set(true);
        }
        else {
            const followingStart = start + this.props.maxItemsPerPage;
            this.nextData.set(fullData.slice(followingStart, followingStart + this.props.maxItemsPerPage));
            this.lastPage.set(this.nextData.getArray().length === 0);
        }
    }
    async onPageChanged(newPageSelected) {
        if (newPageSelected < 0 || newPageSelected >= this.numberOfPages.get()) {
            this.pageSelected.set(Utils.Clamp(newPageSelected, 0, this.numberOfPages.get() - 1));
            return;
        }
        // Avoid doing several transition at the same time
        await Wait.awaitCondition(() => !this.ongoingTransition.get());
        this.ongoingTransition.set(true);
        const gap = newPageSelected - this.previousPageSelected;
        const side = gap > 0 ? 1 : -1;
        for (let i = 0; i < Math.abs(gap); i++) {
            // the target page of the unique transition
            const targetPage = this.previousPageSelected + (i + 1) * side;
            await this.doTransition(side, targetPage);
        }
        this.previousPageSelected = newPageSelected;
        this.ongoingTransition.set(false);
    }
    async doTransition(side, targetPage) {
        for (let i = 5; i < 101; i += 5) {
            this.transitionWrapperRef.instance.style.right = `${100 + i * side}%`;
            await Wait.awaitDelay(1);
        }
        this.updateListsData(targetPage);
        this.transitionWrapperRef.instance.style.right = '100%';
    }
    render() {
        return (FSComponent.buildComponent("div", { ref: this.gamepadUiComponentRef, class: {
                'paging-list': true,
                'first-page': this.firstPage,
                'last-page': this.lastPage,
                'ongoing-transition': this.ongoingTransition,
            } },
            FSComponent.buildComponent("div", { class: "transition-wrapper", ref: this.transitionWrapperRef },
                FSComponent.buildComponent(List, { class: "previous-list", data: this.previousData, renderItem: this.props.renderItem }),
                FSComponent.buildComponent(List, { class: "main-list", data: this.currentData, renderItem: this.props.renderItem }),
                FSComponent.buildComponent(List, { class: "following-list", data: this.nextData, renderItem: this.props.renderItem })),
            FSComponent.buildComponent(CarrouselButtons, { numberOfPages: this.numberOfPages, pageSelected: this.pageSelected })));
    }
}
class CarrouselButtons extends GamepadUiComponent {
    constructor() {
        super(...arguments);
        this.buttonsRef = [];
    }
    onAfterRender(node) {
        super.onAfterRender(node);
        this.pagesSub = this.props.numberOfPages.sub(this.renderButtons.bind(this));
    }
    renderButtons(numPages) {
        // clear all the buttons (TODO, remove and add only what is needed)
        for (const buttonRef of this.buttonsRef) {
            buttonRef.destroy();
        }
        this.buttonsRef.length = 0;
        this.gamepadUiComponentRef.instance.innerHTML = '';
        for (let i = 0; i < numPages; i++) {
            const node = (FSComponent.buildComponent(UniqueCarrouselButton, { selected: ComputedSubject.create(this.props.pageSelected, (pageSelected) => pageSelected === i), callBack: () => {
                    this.props.pageSelected.set(i);
                } }));
            FSComponent.render(node, this.gamepadUiComponentRef.instance);
            this.buttonsRef.push(node.instance);
        }
    }
    changePage(increment) {
        const newPageIndex = Utils.Clamp(this.props.pageSelected.get() + increment, 0, this.props.numberOfPages.get() - 1);
        this.props.pageSelected.set(newPageIndex);
    }
    destroy() {
        var _a;
        (_a = this.pagesSub) === null || _a === void 0 ? void 0 : _a.destroy();
        super.destroy();
    }
    render() {
        return (FSComponent.buildComponent("div", { class: {
                'carrousel-buttons': true,
                hide: ComputedSubject.create(this.props.numberOfPages, (v) => v <= 1),
            } },
            FSComponent.buildComponent(CarrouselArrow, { class: "left", callBack: () => this.changePage(-1) }),
            FSComponent.buildComponent("div", { ref: this.gamepadUiComponentRef, class: "buttons-container" }),
            FSComponent.buildComponent(CarrouselArrow, { class: "right", callBack: () => this.changePage(1) })));
    }
}
class CarrouselArrow extends GamepadUiComponent {
    onAfterRender(node) {
        super.onAfterRender(node);
        this.gamepadUiComponentRef.instance.onclick = this.props.callBack;
    }
    render() {
        return (FSComponent.buildComponent("div", { class: "carrousel-arrow", ref: this.gamepadUiComponentRef },
            FSComponent.buildComponent("icon-element", { "icon-url": arrowIconPath })));
    }
}
class UniqueCarrouselButton extends GamepadUiComponent {
    onAfterRender(node) {
        super.onAfterRender(node);
        this.gamepadUiComponentRef.instance.onclick = this.props.callBack;
    }
    render() {
        return (FSComponent.buildComponent("div", { ref: this.gamepadUiComponentRef, class: { 'unique-carrousel-button': true, selected: this.props.selected } }));
    }
}
