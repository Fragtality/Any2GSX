import { DisplayComponent, FSComponent, Subject } from '@microsoft/msfs-sdk';
import { mergeClassProp } from '../../utils';
import { Button } from '../Button';
import { IconElement } from '../IconElement';
/** @internal */
export class AccordionButton extends DisplayComponent {
    constructor() {
        super(...arguments);
        this.iconStyle = Subject.create('rotate(0)');
        this.isFoldedSubscription = this.props.isFolded.sub((isFolded) => {
            this.iconStyle.set(`rotate(${isFolded ? 0 : -0.5}turn)`);
        }, true);
    }
    render() {
        return (FSComponent.buildComponent(Button, { class: mergeClassProp('accordion-button', this.props.class), style: this.props.style, callback: this.props.callback, hoverable: this.props.hoverable, selected: this.props.selected, state: this.props.state, disabled: this.props.disabled },
            FSComponent.buildComponent("div", { class: "icon-container", style: { transform: this.iconStyle } },
                FSComponent.buildComponent(IconElement, { class: "accordion-button-icon", url: `coui://html_ui/efb_ui/efb_os/Assets/icons/Arrow.svg` })),
            FSComponent.buildComponent("div", { class: "title" }, this.props.children)));
    }
    destroy() {
        this.isFoldedSubscription.destroy();
        super.destroy();
    }
}
