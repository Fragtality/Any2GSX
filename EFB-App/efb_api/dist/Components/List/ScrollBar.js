import { DisplayComponent, FSComponent } from '@microsoft/msfs-sdk';
import './ScrollBar.css';
export class ScrollBar extends DisplayComponent {
    constructor() {
        super(...arguments);
        this.svgRef = FSComponent.createRef();
        this.scrollBarRef = FSComponent.createRef();
        this.scrollThumbRef = FSComponent.createRef();
        this.scrollBarContainerRef = FSComponent.createRef();
        this.scrollableContainer = null;
        this.scrollListener = this.onScroll.bind(this);
        this.sizeChangeTimer = null;
        this.arrowPadding = 6;
        this.margin = 2;
        this.currentScrollHeight = 0;
        this.currentThumbAreaHeight = 0;
        this.scrollHeightRatio = 0;
    }
    onAfterRender(node) {
        super.onAfterRender(node);
        this.scrollableContainer = this.scrollBarContainerRef.instance.previousElementSibling;
        this.scrollableContainer.addEventListener('scroll', this.scrollListener);
        const diffAndAdjust = () => {
            var _a;
            if (this.currentScrollHeight !== ((_a = this.scrollableContainer) === null || _a === void 0 ? void 0 : _a.scrollHeight)) {
                this.adjustScrollbarDimensions();
            }
        };
        this.sizeChangeTimer = setInterval(diffAndAdjust, 150);
        this.onScroll();
    }
    /**
     * Adjusts the dimensions of the scrollbar elements.
     */
    adjustScrollbarDimensions() {
        if (this.scrollableContainer) {
            const parentTop = this.scrollableContainer.offsetTop;
            this.scrollBarContainerRef.instance.style.top = `${parentTop + 4}px`;
            this.currentScrollHeight = this.scrollableContainer.scrollHeight;
            const containerHeight = this.scrollableContainer.clientHeight;
            const totalMarginAndPadding = this.arrowPadding * 2 + this.margin * 2;
            this.currentThumbAreaHeight = containerHeight - totalMarginAndPadding;
            this.scrollHeightRatio = this.currentScrollHeight / containerHeight;
            this.scrollThumbRef.instance.style.height = `${this.currentThumbAreaHeight / this.scrollHeightRatio}`.toString();
            this.scrollBarContainerRef.instance.style.height = `${containerHeight}px`;
            this.svgRef.instance.setAttribute('height', `${containerHeight - this.margin * 2}px`);
            this.scrollBarRef.instance.style.height = `${containerHeight}px`;
            this.scrollBarContainerRef.instance.style.display = this.scrollHeightRatio <= 1.0 ? 'none' : '';
            this.onScroll();
        }
    }
    /**
     * Eventhandler called when a scroll event in the scrollable parent container happens.
     */
    onScroll() {
        if (this.scrollableContainer) {
            const scrollY = (this.scrollableContainer.scrollTop / this.scrollableContainer.scrollHeight) *
                this.currentThumbAreaHeight +
                this.arrowPadding;
            if (!isNaN(scrollY)) {
                this.scrollThumbRef.instance.setAttribute('y', scrollY.toString());
            }
        }
    }
    render() {
        return (FSComponent.buildComponent("div", { class: "scroll-bar", ref: this.scrollBarContainerRef },
            FSComponent.buildComponent("svg", { ref: this.svgRef },
                FSComponent.buildComponent("rect", { ref: this.scrollBarRef, x: "3", y: "6", width: "4", fill: "#707070" }),
                FSComponent.buildComponent("rect", { ref: this.scrollThumbRef, x: "3", y: "6", width: "4", fill: "#1390E3" }))));
    }
    destroy() {
        if (this.scrollableContainer) {
            this.scrollableContainer.removeEventListener('scroll', this.scrollListener);
        }
        if (this.sizeChangeTimer !== null) {
            clearInterval(this.sizeChangeTimer);
        }
    }
}
