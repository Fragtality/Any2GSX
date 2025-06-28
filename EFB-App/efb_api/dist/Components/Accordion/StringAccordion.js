import { FSComponent } from '@microsoft/msfs-sdk';
import { TT } from '../../i18n';
import { AbstractAccordion } from './AbstractAccordion';
export class StringAccordion extends AbstractAccordion {
    renderHeader() {
        return FSComponent.buildComponent(TT, { key: this.props.title, format: "ucfirst" });
    }
}
