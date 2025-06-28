import { AbstractButton } from './Button';
/** @deprecated Deprecated component */
export class TypedButton extends AbstractButton {
    onAfterRender(node) {
        var _a;
        super.onAfterRender(node);
        this.gamepadUiComponentRef.instance.classList.add((_a = this.props.type) !== null && _a !== void 0 ? _a : 'primary');
    }
}
