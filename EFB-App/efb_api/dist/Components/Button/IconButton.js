import { DisplayComponent, FSComponent } from '@microsoft/msfs-sdk';
import { mergeClassProp } from '../../utils';
import { IconElement } from '../IconElement';
import { Button } from './Button';
export class IconButton extends DisplayComponent {
    iconRendering() {
        if (this.props.iconPath) {
            return FSComponent.buildComponent(IconElement, { url: this.props.iconPath });
        }
        return null;
    }
    render() {
        return (FSComponent.buildComponent(Button, { class: mergeClassProp('icon-button', this.props.class), style: this.props.style, callback: this.props.callback, hoverable: this.props.hoverable, selected: this.props.selected, state: this.props.state, disabled: this.props.disabled }, this.iconRendering()));
    }
}
