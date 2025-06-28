import { DisplayComponent, FSComponent } from '@microsoft/msfs-sdk';
import { mergeClassProp } from '../utils';
export class ViewStackContainer extends DisplayComponent {
    constructor() {
        super(...arguments);
        this.rootRef = FSComponent.createRef();
    }
    renderView(view) {
        FSComponent.render(view, this.rootRef.instance);
    }
    render() {
        var _a;
        return (FSComponent.buildComponent("div", { ref: this.rootRef, class: mergeClassProp('view-stack', this.props.class), id: (_a = this.props.id) !== null && _a !== void 0 ? _a : '' }));
    }
}
