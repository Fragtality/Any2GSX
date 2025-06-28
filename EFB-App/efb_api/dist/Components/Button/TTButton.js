import { DisplayComponent, FSComponent } from '@microsoft/msfs-sdk';
import { TT } from '../../i18n';
import { mergeClassProp } from '../../utils';
import { Button } from './Button';
export class TTButton extends DisplayComponent {
    render() {
        var _a;
        return (FSComponent.buildComponent(Button, { class: mergeClassProp('tt-button', 'classic-button', this.props.class), style: this.props.style, callback: this.props.callback, hoverable: this.props.hoverable, selected: this.props.selected, state: this.props.state, disabled: this.props.disabled },
            FSComponent.buildComponent(TT, { class: "bold-text", key: this.props.key, type: this.props.type, format: (_a = this.props.format) !== null && _a !== void 0 ? _a : 'uppercase', arguments: this.props.arguments })));
    }
}
/**
 * @deprecated ClassicButton is the old version of TTButton. Use TTButton instead.
 */
export class ClassicButton extends Button {
    buttonRender() {
        var _a;
        return (FSComponent.buildComponent("div", { class: "classic-button" },
            FSComponent.buildComponent(TT, { key: this.props.title, format: (_a = this.props.format) !== null && _a !== void 0 ? _a : 'upper', type: "div", class: "bold-text" })));
    }
}
