import { FSComponent, MappedSubject, Subject, SubscribableUtils, } from '@microsoft/msfs-sdk';
import { GamepadUiComponent } from '../../Gamepad';
/** UI component that renders a slider and changes a given value accordingly */
export class Slider extends GamepadUiComponent {
    constructor(props) {
        var _a, _b, _c, _d, _e, _f, _g;
        super(props);
        this.max = SubscribableUtils.toSubscribable((_a = this.props.max) !== null && _a !== void 0 ? _a : 100, true);
        this.min = SubscribableUtils.toSubscribable((_b = this.props.min) !== null && _b !== void 0 ? _b : 0, true);
        this.isButtonDisabled = SubscribableUtils.toSubscribable((_c = this.props.disabled) !== null && _c !== void 0 ? _c : false, true);
        this.isButtonSelected = Subject.create(false);
        // If this.props.step if undefined, the precision will be equal to 0
        this.precision = MappedSubject.create(([step, min, _max]) => this.convertValueToPercent(step + min), SubscribableUtils.toSubscribable((_d = this.props.step) !== null && _d !== void 0 ? _d : 0, true), this.min, this.max);
        // this.convertValueToPercent((this.props.step ?? 0) + this.min);
        this.isButtonHoverable = MappedSubject.create(([isHoverable, isDisabled, isSelected]) => {
            return isHoverable && !isDisabled && !isSelected;
        }, SubscribableUtils.toSubscribable((_e = this.props.hoverable) !== null && _e !== void 0 ? _e : true, true), this.isButtonDisabled, this.isButtonSelected);
        this.verticalSlider = (_f = this.props.vertical) !== null && _f !== void 0 ? _f : false;
        this.allowWheel = (_g = this.props.allowWheel) !== null && _g !== void 0 ? _g : false;
        this.sliderBarRef = FSComponent.createRef();
        this.sliderBarRect = new DOMRect();
        this.allowMovement = false;
        this.mousePos = 0;
        this.subs = [];
        this.onGlobalMouseUp = this._onGlobalMouseUp.bind(this);
        this.onGlobalMouseMove = this._onGlobalMouseMove.bind(this);
        if (SubscribableUtils.isMutableSubscribable(props.value)) {
            this.value = props.value;
        }
        else if (SubscribableUtils.isSubscribable(props.value)) {
            this.value = Subject.create(props.value.get());
            this.subscribableValueSubscription = props.value.sub((value) => this.value.set(value));
        }
        else {
            this.value = Subject.create(props.value);
        }
        this.valuePercent = Subject.create(this.convertValueToPercent(this.value.get()));
        this.completionRatio = this.valuePercent.map((ratio) => `${ratio.toString()}%`);
    }
    convertValueToPercent(val) {
        // Math.abs in case of a reverse mode (this.min is superior to this.max)
        return Utils.Clamp(Math.abs((val - this.min.get()) / (this.max.get() - this.min.get())) * 100, 0, 100);
    }
    onMouseDown() {
        var _a, _b;
        if (this.disabled.get()) {
            return;
        }
        (_b = (_a = this.props).onFocusIn) === null || _b === void 0 ? void 0 : _b.call(_a);
        this.isButtonSelected.set(true);
        this.allowMovement = true;
        // Mandatory getBoudingClientRect() to handle slider movement with the mouse outside of the slider
        this.sliderBarRect = this.gamepadUiComponentRef.instance.getBoundingClientRect();
        document.addEventListener('mouseup', this.onGlobalMouseUp);
        // Remove the event listeners if the mouse goes out of the component
        document.addEventListener('mouseleave', this.onGlobalMouseUp);
        document.addEventListener('mousemove', this.onGlobalMouseMove);
    }
    onMouseWheel(e) {
        e.preventDefault();
        const precision = this.precision.get() || 1;
        const scrollDirection = this.verticalSlider ? -1 : 1;
        const step = precision * (e.deltaY < 1 ? -1 : 1) * scrollDirection;
        this.valuePercent.set(Utils.Clamp(this.valuePercent.get() + step, 0, 100));
    }
    _onGlobalMouseUp() {
        var _a, _b;
        this.allowMovement = false;
        (_b = (_a = this.props).onFocusOut) === null || _b === void 0 ? void 0 : _b.call(_a);
        this.isButtonSelected.set(false);
        document.removeEventListener('mouseup', this.onGlobalMouseUp);
        document.removeEventListener('mouseleave', this.onGlobalMouseUp);
        document.removeEventListener('mousemove', this.onGlobalMouseMove);
    }
    _onGlobalMouseMove(e) {
        const clientPos = this.verticalSlider ? e.clientY : e.clientX;
        if (!this.allowMovement || clientPos === this.mousePos) {
            return;
        }
        this.mousePos = clientPos;
        let sliderStart = this.sliderBarRect.left;
        let sliderEnd = this.sliderBarRect.right;
        let sliderSize = this.sliderBarRect.width;
        let startRatio = 0;
        if (this.verticalSlider) {
            sliderStart = this.sliderBarRect.top;
            sliderEnd = this.sliderBarRect.bottom;
            sliderSize = this.sliderBarRect.height;
            startRatio = 100;
        }
        if (clientPos < sliderStart) {
            this.valuePercent.set(startRatio);
            return;
        }
        if (clientPos > sliderEnd) {
            this.valuePercent.set(100 - startRatio);
            return;
        }
        const mousePosOnSlider = Utils.Clamp(Math.abs(startRatio - ((clientPos - sliderStart) / sliderSize) * 100), 0, 100);
        let sliderPos = mousePosOnSlider;
        const precision = this.precision.get();
        if (precision !== 0) {
            const quotient = Math.trunc((mousePosOnSlider + precision / 2) / precision);
            sliderPos = Utils.Clamp(quotient * precision, 0, 100);
        }
        this.valuePercent.set(sliderPos);
    }
    render() {
        return (FSComponent.buildComponent("div", { class: {
                slider: true,
                reverse: this.min > this.max,
                vertical: this.verticalSlider,
                hoverable: this.isButtonHoverable,
                selected: this.isButtonSelected,
            }, style: { '--ratio-value': this.completionRatio }, ref: this.gamepadUiComponentRef },
            FSComponent.buildComponent("div", { class: "disabled-layer" }),
            FSComponent.buildComponent("div", { class: "slider-bar", ref: this.sliderBarRef },
                FSComponent.buildComponent("div", { class: "slider-rail" }),
                FSComponent.buildComponent("div", { class: "slider-track" }),
                FSComponent.buildComponent("div", { class: "slider-button" }))));
    }
    onAfterRender(node) {
        super.onAfterRender(node);
        Coherent.on('mouseReleaseOutsideView', this.onGlobalMouseUp);
        this.sliderBarRef.instance.onmousedown = this.onMouseDown.bind(this);
        if (this.allowWheel) {
            this.sliderBarRef.instance.onwheel = this.onMouseWheel.bind(this);
        }
        this.subs.push(this.valuePercent.sub((val) => this.value.set((val * (this.max.get() - this.min.get())) / 100 + this.min.get())), this.value.sub((value) => {
            var _a, _b;
            (_b = (_a = this.props).onValueChange) === null || _b === void 0 ? void 0 : _b.call(_a, value);
            this.valuePercent.set(this.convertValueToPercent(value));
        }));
    }
    destroy() {
        this.isButtonHoverable.destroy();
        this.subs.forEach((sub) => sub.destroy());
        super.destroy();
    }
}
