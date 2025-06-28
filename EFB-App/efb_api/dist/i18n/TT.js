var __rest = (this && this.__rest) || function (s, e) {
    var t = {};
    for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p) && e.indexOf(p) < 0)
        t[p] = s[p];
    if (s != null && typeof Object.getOwnPropertySymbols === "function")
        for (var i = 0, p = Object.getOwnPropertySymbols(s); i < p.length; i++) {
            if (e.indexOf(p[i]) < 0 && Object.prototype.propertyIsEnumerable.call(s, p[i]))
                t[p[i]] = s[p[i]];
        }
    return t;
};
import { DisplayComponent, FSComponent, SubscribableUtils } from '@microsoft/msfs-sdk';
import { isFunction } from '../utils';
export class TT extends DisplayComponent {
    constructor() {
        super(...arguments);
        this.ref = FSComponent.createRef();
        this.key = SubscribableUtils.toSubscribable(this.props.key, true);
        this.formatter = this.props.format || ((text) => text);
        this.subs = [];
        this.reloadText = this._reloadText.bind(this);
    }
    _reloadText() {
        var _a;
        const key = this.key.get();
        let translatedKey = key.startsWith('@') || key.startsWith('TT') ? Utils.Translate(key) : key;
        (_a = this.props.arguments) === null || _a === void 0 ? void 0 : _a.forEach((argumentValue, argumentKey) => {
            const argumentValueSub = SubscribableUtils.toSubscribable(argumentValue, true);
            translatedKey = translatedKey.replace(argumentKey, argumentValueSub.get());
        });
        this.updateText(translatedKey);
    }
    getFormattedText(text) {
        if (isFunction(this.formatter)) {
            return this.formatter(text);
        }
        switch (this.formatter) {
            case 'upper':
            case 'uppercase':
                return text.toUpperCase();
            case 'lower':
            case 'lowercase':
                return text.toLowerCase();
            case 'ucfirst':
                return `${text.charAt(0).toUpperCase()}${text.slice(1)}`;
            case 'capitalize':
                return text.replace(/(^\w{1})|(\s+\w{1})/g, (letter) => letter.toUpperCase());
            default:
                console.warn(`Format "${this.props.format}" is not supported.`);
                return text;
        }
    }
    updateText(text) {
        this.ref.instance.innerText = this.getFormattedText(text);
    }
    render() {
        var _a;
        const Tag = (_a = this.props.type) !== null && _a !== void 0 ? _a : 'span';
        const _b = this.props, { key: _key, type: _type, format: _format, children: _children } = _b, props = __rest(_b, ["key", "type", "format", "children"]);
        return FSComponent.buildComponent(Tag, Object.assign({ ref: this.ref }, props));
    }
    onAfterRender(node) {
        var _a;
        super.onAfterRender(node);
        (_a = this.props.arguments) === null || _a === void 0 ? void 0 : _a.forEach((argumentValue, argumentKey) => {
            const subValue = SubscribableUtils.toSubscribable(argumentValue, true);
            this.subs.push(subValue.sub(this.reloadText));
        });
        this.reloadSubscription = this.key.sub(this.reloadText, true);
        Coherent.on('RELOAD_LOCALISATION', this.reloadText);
    }
    destroy() {
        var _a;
        Coherent.off('RELOAD_LOCALISATION', this.reloadText);
        (_a = this.reloadSubscription) === null || _a === void 0 ? void 0 : _a.destroy();
        this.subs.forEach((sub) => sub.destroy());
        super.destroy();
    }
}
