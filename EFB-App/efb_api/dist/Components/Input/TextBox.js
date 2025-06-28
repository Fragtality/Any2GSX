import { DisplayComponent, FSComponent, Subject, SubscribableUtils, } from '@microsoft/msfs-sdk';
import { GamepadUiComponent } from '../../Gamepad';
import { InputsListener } from '../../Listeners/InputsListener';
import { isVNode, mergeClassProp } from '../../utils';
import { IconButton } from '../Button';
import { Input } from './Input';
const addIconPath = 'coui://html_ui/efb_ui/efb_os/Assets/icons/NoMargin/Close.svg';
/**
 * This TextBox component
 * @internal
 */
export class TextBox extends GamepadUiComponent {
    constructor() {
        super(...arguments);
        this.subscriptions = [];
        this.input_id = '';
        this.inputRef = FSComponent.createRef();
        this.model = this.props.model || Subject.create(SubscribableUtils.toSubscribable(this.props.value || '', true).get());
        this.hideDeleteTextButton = Subject.create(true);
        this.onDelete = this._onDelete.bind(this);
        this.onmousedown = (e) => {
            e.preventDefault();
            if (!this.inputRef.instance.isFocused.get()) {
                this.inputRef.instance.focus();
            }
        };
        this.prefix = SubscribableUtils.toSubscribable(this.props.prefix || '', true);
        this.suffix = SubscribableUtils.toSubscribable(this.props.suffix || '', true);
        this.prefixRef = FSComponent.createRef();
        this.suffixRef = FSComponent.createRef();
        this.prefixSuffixEmptyCallback = (value) => {
            return value.toString().length === 0;
        };
    }
    _onDelete(event) {
        var _a, _b;
        event.preventDefault();
        this.hideDeleteTextButton.set(true);
        this.inputRef.instance.clearInput();
        (_b = (_a = this.props).onDelete) === null || _b === void 0 ? void 0 : _b.call(_a);
        if (this.props.focusOnClear) {
            this.inputRef.instance.focus();
        }
    }
    onInput(input) {
        var _a, _b;
        (_b = (_a = this.props).onInput) === null || _b === void 0 ? void 0 : _b.call(_a, input);
        if (this.props.showDeleteIcon) {
            this.hideDeleteTextButton.set(input.value.trim().length === 0);
        }
    }
    onFocusIn() {
        var _a, _b;
        (_b = (_a = this.props).onFocusIn) === null || _b === void 0 ? void 0 : _b.call(_a);
        this.input_id = InputsListener.addInputChangeCallback('MENU_CORE', 'KEY_MENU_SR_BACK', (down) => {
            if (down) {
                this.inputRef.instance.blur();
            }
        });
        this.gamepadUiComponentRef.instance.classList.add('textbox--focused');
    }
    onFocusOut() {
        var _a, _b;
        (_b = (_a = this.props).onFocusOut) === null || _b === void 0 ? void 0 : _b.call(_a);
        InputsListener.removeInputChangeCallback(this.input_id);
        this.gamepadUiComponentRef.instance.classList.remove('textbox--focused');
    }
    render() {
        return (FSComponent.buildComponent("div", { ref: this.gamepadUiComponentRef, class: "textbox" },
            FSComponent.buildComponent("div", { class: "disabled-layer" }),
            FSComponent.buildComponent("div", { ref: this.prefixRef, class: {
                    prefix: true,
                    'prefix--hidden': this.prefix.map(this.prefixSuffixEmptyCallback),
                } }),
            FSComponent.buildComponent(Input, { ref: this.inputRef, type: "text", align: this.props.align, model: this.model, placeholder: this.props.placeholder, disabled: this.props.disabled, hidePlaceholderOnFocus: this.props.hidePlaceholderOnFocus, focusOnInit: this.props.focusOnInit, onFocusIn: this.onFocusIn.bind(this), onFocusOut: this.onFocusOut.bind(this), onInput: this.onInput.bind(this), onKeyPress: this.props.onKeyPress, charFilter: this.props.charFilter }),
            FSComponent.buildComponent("div", { ref: this.suffixRef, class: {
                    suffix: true,
                    'suffix--hidden': this.suffix.map(this.prefixSuffixEmptyCallback),
                } }),
            this.props.showDeleteIcon && (FSComponent.buildComponent(IconButton, { class: mergeClassProp('delete-text-button', { hide: this.hideDeleteTextButton }), iconPath: addIconPath, callback: this.onDelete }))));
    }
    onAfterRender(node) {
        super.onAfterRender(node);
        const prefixSuffixCb = (reference) => {
            let previousNode = null;
            return (value) => {
                if (previousNode) {
                    FSComponent.shallowDestroy(previousNode);
                    const instance = previousNode.instance;
                    if (instance && instance instanceof DisplayComponent) {
                        instance.destroy();
                    }
                    previousNode = null;
                }
                if (typeof value === 'string') {
                    reference.instance.innerText = value;
                }
                else if (typeof value === 'object' && isVNode(value)) {
                    previousNode = value;
                    reference.instance.innerHTML = '';
                    FSComponent.render(value, reference.instance);
                }
                else {
                    reference.instance.innerText = value;
                }
            };
        };
        this.subscriptions.push(this.prefix.sub(prefixSuffixCb(this.prefixRef), true), this.suffix.sub(prefixSuffixCb(this.suffixRef), true));
        this.gamepadUiComponentRef.instance.addEventListener('mousedown', this.onmousedown);
    }
    destroy() {
        this.gamepadUiComponentRef.instance.removeEventListener('mousedown', this.onmousedown);
        this.subscriptions.forEach((s) => s.destroy());
        super.destroy();
    }
}
