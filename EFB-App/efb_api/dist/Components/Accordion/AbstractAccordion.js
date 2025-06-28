import { FSComponent, Subject, SubscribableUtils } from '@microsoft/msfs-sdk';
import { GamepadUiComponent } from '../../Gamepad';
import { AccordionButton } from './AccordionButton';
export class AbstractAccordion extends GamepadUiComponent {
    constructor() {
        var _a;
        super(...arguments);
        this.isFolded = SubscribableUtils.isMutableSubscribable(this.props.isFolded)
            ? this.props.isFolded
            : Subject.create((_a = this.props.isFolded) !== null && _a !== void 0 ? _a : true);
    }
    renderBody() {
        return FSComponent.buildComponent(FSComponent.Fragment, null, this.props.children);
    }
    render() {
        return (FSComponent.buildComponent("div", { class: { accordion: true, folded: this.isFolded }, ref: this.gamepadUiComponentRef },
            FSComponent.buildComponent(AccordionButton, { isFolded: this.isFolded, disabled: this.props.disabled, customClass: "accordion-button abstract-button--full-width", callback: () => {
                    this.isFolded.set(!this.isFolded.get());
                } }, this.renderHeader()),
            FSComponent.buildComponent("div", { class: { 'menu-content': true, hide: this.isFolded } }, this.renderBody())));
    }
}
