import { FSComponent, ICAO } from '@microsoft/msfs-sdk';
import { GamepadUiComponent } from '../../Gamepad';
import { getFacilityIconPath } from '../../utils/FacilityUtils';
export class FacilityResultItem extends GamepadUiComponent {
    onAfterRender(node) {
        super.onAfterRender(node);
        this.gamepadUiComponentRef.instance.onmousedown = (ev) => {
            ev.preventDefault();
            if (this.props.callback === undefined) {
                return;
            }
            this.props.callback(this.props.facility);
        };
    }
    render() {
        return (FSComponent.buildComponent("div", { class: `facility-result-item`, ref: this.gamepadUiComponentRef },
            FSComponent.buildComponent("div", { class: "icon-container" },
                FSComponent.buildComponent("icon-element", { "icon-url": getFacilityIconPath(ICAO.getFacilityType(this.props.facility.icao)) })),
            FSComponent.buildComponent("span", { class: "icao" },
                ICAO.getIdent(this.props.facility.icao),
                this.props.separator),
            FSComponent.buildComponent("span", { class: "facility-name" }, Utils.Translate(this.props.facility.name).padEnd(5, '...'))));
    }
}
