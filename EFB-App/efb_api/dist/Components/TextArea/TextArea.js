import { FSComponent, MappedSubject, Subject, SubscribableUtils, UUID, } from '@microsoft/msfs-sdk';
import { GamepadUiComponent } from '../../Gamepad';
export class TextArea extends GamepadUiComponent {
    constructor() {
        var _a, _b;
        super(...arguments);
        this.uuid = UUID.GenerateUuid();
        this.textAreaRef = this.gamepadUiComponentRef;
        this.model = this.props.model || Subject.create(SubscribableUtils.toSubscribable(this.props.value || '', true).get());
        this.dispatchFocusOutEvent = this._dispatchFocusOutEvent.bind(this);
        this._onKeyPress = this.onKeyPress.bind(this);
        this._onInput = this.onInput.bind(this);
        this.reloadLocalisation = this._reloadLocalisation.bind(this);
        this._isFocused = Subject.create(false);
        this.isFocused = this._isFocused;
        /** Placeholder i18n/visibility */
        this.placeholderKey = SubscribableUtils.toSubscribable((_a = this.props.placeholder) !== null && _a !== void 0 ? _a : '', true);
        this.placeholderShown = Subject.create(true);
        this.placeholderTranslation = Subject.create(this.placeholderKey.get());
        this.hidePlaceholderOnFocus = (_b = this.props.hidePlaceholderOnFocus) !== null && _b !== void 0 ? _b : false;
        this.subs = [];
    }
    _reloadLocalisation() {
        this.placeholderTranslation.notify();
    }
    onKeyPress(event) {
        var _a, _b;
        const keyCode = event.keyCode || event.which;
        (_b = (_a = this.props).onKeyPress) === null || _b === void 0 ? void 0 : _b.call(_a, event);
        if (event.defaultPrevented) {
            return;
        }
        if (this.props.charFilter && !this.props.charFilter(String.fromCharCode(keyCode))) {
            event.preventDefault();
            return;
        }
    }
    onInput() {
        const value = this.textAreaRef.instance.value;
        if (value === this.model.get()) {
            return;
        }
        this.model.set(value);
    }
    onInputUpdated(value) {
        var _a, _b;
        this.textAreaRef.instance.value = value;
        (_b = (_a = this.props).onInput) === null || _b === void 0 ? void 0 : _b.call(_a, this.textAreaRef.instance);
        if (!this.hidePlaceholderOnFocus && value.length === 0) {
            this.placeholderShown.set(true);
        }
    }
    onFocusIn() {
        var _a, _b;
        (_b = (_a = this.props).onFocusIn) === null || _b === void 0 ? void 0 : _b.call(_a);
        if (this.hidePlaceholderOnFocus && this.textAreaRef.instance.value.length === 0) {
            this.placeholderShown.set(false);
        }
    }
    onFocusOut() {
        var _a, _b;
        (_b = (_a = this.props).onFocusOut) === null || _b === void 0 ? void 0 : _b.call(_a);
        if (this.hidePlaceholderOnFocus && this.textAreaRef.instance.value.length === 0) {
            this.placeholderShown.set(true);
        }
    }
    focus() {
        this.textAreaRef.instance.focus();
    }
    blur() {
        this.textAreaRef.instance.blur();
    }
    value() {
        return this.model.get();
    }
    clearInput() {
        this.model.set('');
    }
    _dispatchFocusOutEvent() {
        this.textAreaRef.instance.blur();
    }
    render() {
        var _a;
        return (FSComponent.buildComponent("textarea", { id: this.uuid, ref: this.textAreaRef, placeholder: MappedSubject.create(([placeholderShown, placeholderKey]) => {
                return placeholderShown ? Utils.Translate(placeholderKey) : '';
            }, this.placeholderShown, this.placeholderKey), disabled: this.props.disabled, value: SubscribableUtils.toSubscribable(this.props.model || this.props.value || '', true).get(), rows: (_a = this.props.rows) !== null && _a !== void 0 ? _a : 4 }));
    }
    onAfterRender(node) {
        super.onAfterRender(node);
        this.subs.push(this.model.sub((value) => {
            this.onInputUpdated(value);
        }, true), this.placeholderKey.sub((key) => {
            this.placeholderTranslation.set(key);
        }, true));
        this.textAreaRef.instance.addEventListener('focus', () => {
            if (this._isFocused.get()) {
                return;
            }
            this._isFocused.set(true);
            Coherent.trigger('FOCUS_INPUT_FIELD', this.uuid, '', '', '', true);
            Coherent.on('mousePressOutsideView', this.dispatchFocusOutEvent);
            this.onFocusIn();
        });
        this.textAreaRef.instance.addEventListener('focusout', () => {
            if (!this._isFocused.get()) {
                return;
            }
            this._isFocused.set(false);
            Coherent.trigger('UNFOCUS_INPUT_FIELD', this.uuid);
            Coherent.off('mousePressOutsideView', this.dispatchFocusOutEvent);
            this.onFocusOut();
        });
        this.textAreaRef.instance.addEventListener('input', this._onInput);
        this.textAreaRef.instance.addEventListener('keypress', this._onKeyPress);
        Coherent.on('RELOAD_LOCALISATION', this.reloadLocalisation);
        if (this.props.focusOnInit) {
            this.focus();
        }
    }
    destroy() {
        this.subs.forEach((s) => s.destroy());
        if (this._isFocused.get()) {
            Coherent.trigger('UNFOCUS_INPUT_FIELD', this.uuid);
            Coherent.off('mousePressOutsideView', this.dispatchFocusOutEvent);
        }
        this.textAreaRef.instance.removeEventListener('keypress', this._onKeyPress);
        this.textAreaRef.instance.removeEventListener('input', this._onInput);
        Coherent.off('RELOAD_LOCALISATION', this.reloadLocalisation);
        super.destroy();
    }
}
