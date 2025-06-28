import { FSComponent } from '@microsoft/msfs-sdk';
import { value } from '../../utils';
import { AbstractAccordion } from './AbstractAccordion';
export class ElementAccordion extends AbstractAccordion {
    renderHeader() {
        return FSComponent.buildComponent(FSComponent.Fragment, null, value(this.props.header));
    }
}
