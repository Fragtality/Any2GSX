import { ArraySubject, FacilityType, FacilityUtils, FSComponent, MappedSubject, RunwayUtils, } from '@microsoft/msfs-sdk';
import { GamepadUiComponent } from '../../../Gamepad';
import { getRunwayName } from '../../../utils';
import { DropdownButton } from '../../Button';
export class RunwaySelector extends GamepadUiComponent {
    constructor() {
        super(...arguments);
        this.runways = ArraySubject.create();
        this.currentRunwayName = MappedSubject.create(([facility, currentRunway]) => {
            /* Update the runways ArraySubject inside of the runway name Subject because MappedSubject cannot map instances of ArraySubscribable */
            const runways = facility && FacilityUtils.isFacilityType(facility, FacilityType.Airport)
                ? RunwayUtils.getOneWayRunwaysFromAirport(facility)
                : [];
            this.runways.set(runways);
            const runwayIndex = typeof currentRunway === 'number'
                ? currentRunway
                : runways.findIndex((runway) => runway.designation === currentRunway.designation);
            return runwayIndex >= 0 && runwayIndex < runways.length ? getRunwayName(runways[runwayIndex]) : '';
        }, this.props.facility, this.props.currentRunway);
    }
    render() {
        return (FSComponent.buildComponent("div", { class: {
                'runway-selector': true,
                hide: this.currentRunwayName.map((name) => !name),
            }, ref: this.gamepadUiComponentRef },
            FSComponent.buildComponent(DropdownButton, { title: this.currentRunwayName, type: "secondary", listDataset: this.runways, getItemLabel: getRunwayName, onItemClick: (runway, index) => {
                    this.props.onRunwaySelected(runway, index);
                }, showArrowIcon: true })));
    }
}
