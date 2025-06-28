import { DisplayComponent, FSComponent, Subject, SubscribableUtils, } from '@microsoft/msfs-sdk';
import { mergeClassProp } from '../utils';
/** @internal */
export class GamepadUiComponent extends DisplayComponent {
    constructor() {
        var _a, _b;
        super(...arguments);
        this._gamepadUiSubs = [];
        this.gamepadUiComponentRef = FSComponent.createRef();
        this._nextHandler = Subject.create(undefined);
        this.nextHandler = this._nextHandler;
        this.disabled = SubscribableUtils.toSubscribable((_a = this.props.disabled) !== null && _a !== void 0 ? _a : false, true);
        this.visible = SubscribableUtils.toSubscribable((_b = this.props.visible) !== null && _b !== void 0 ? _b : true, true);
        this.componentClickListener = this.handleComponentClick.bind(this);
    }
    setNextGamepadEventHandler(ref) {
        this._nextHandler.set(ref);
    }
    deletePreviousGamepadEventHandler() {
        throw new Error('Method not implemented.');
    }
    handleGamepadEvent(gamepadEvent) {
        console.log(`Received ${gamepadEvent} in handleMoveEvent`);
    }
    onAfterRender(node) {
        super.onAfterRender(node);
        const customClasses = mergeClassProp(this.props.customClass, this.props.class);
        Object.keys(customClasses).forEach((className) => {
            const subscribable = SubscribableUtils.toSubscribable(customClasses[className], true);
            this._gamepadUiSubs.push(subscribable.sub((value) => {
                this.gamepadUiComponentRef.instance.classList.toggle(className, value);
            }, true));
        });
        //const customStyles = mergeClassProp(this.props.style);
        //Object.keys(customStyles);
        this._gamepadUiSubs.push(this.visible.sub((isVisible) => {
            this.gamepadUiComponentRef.instance.hidden = !isVisible;
        }, true));
        if (SubscribableUtils.isSubscribable(this.props.disabled)) {
            this._gamepadUiSubs.push(this.props.disabled.sub((disabled) => {
                disabled ? this.disable() : this.enable();
            }, true));
        }
        else {
            this.props.disabled ? this.disable() : this.enable();
        }
        window.addEventListener('click', this.componentClickListener);
        if (this.props.onboardingStepId === undefined) {
            return;
        }
        this.gamepadUiComponentRef.instance.setAttribute('id', this.props.onboardingStepId);
    }
    handleComponentClick(e) {
        if (!this.gamepadUiComponentRef.instance.contains(e.target)) {
            this.onClickOutOfComponent(e);
        }
    }
    onButtonAPressed() {
        if (this.props.onButtonAPressed !== undefined) {
            this.props.onButtonAPressed();
        }
    }
    onButtonBPressed() {
        if (this.props.onButtonBPressed !== undefined) {
            this.props.onButtonBPressed();
        }
    }
    onClickOutOfComponent(_e) {
        return;
    }
    enable() {
        this.gamepadUiComponentRef.instance.removeAttribute('disabled');
    }
    disable() {
        this.gamepadUiComponentRef.instance.setAttribute('disabled', '');
    }
    show() {
        this.gamepadUiComponentRef.instance.style.visibility = 'visible';
    }
    hide() {
        this.gamepadUiComponentRef.instance.style.visibility = 'hidden';
    }
    toggleFocus(value) {
        if (value !== undefined) {
            if (value === true) {
                this.gamepadUiComponentRef.instance.classList.add(GamepadUiComponent.FOCUS_CLASS);
            }
            else {
                this.gamepadUiComponentRef.instance.classList.remove(GamepadUiComponent.FOCUS_CLASS);
            }
            return;
        }
        this.gamepadUiComponentRef.instance.classList.toggle(GamepadUiComponent.FOCUS_CLASS);
    }
    getComponentRect() {
        return this.gamepadUiComponentRef.instance.getBoundingClientRect();
    }
    destroy() {
        this._gamepadUiSubs.forEach((s) => s.destroy());
        window.removeEventListener('click', this.componentClickListener);
        super.destroy();
    }
}
GamepadUiComponent.FOCUS_CLASS = 'focus';
