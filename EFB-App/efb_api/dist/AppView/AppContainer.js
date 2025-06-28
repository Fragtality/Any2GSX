import { DisplayComponent, FSComponent } from '@microsoft/msfs-sdk';
import { mergeClassProp } from '../utils';
export class AppContainer extends DisplayComponent {
    constructor() {
        super(...arguments);
        this.appMainRef = FSComponent.createRef();
        this.appStackRef = FSComponent.createRef();
    }
    onAfterRender(node) {
        super.onAfterRender(node);
        this.props.appViewService.onAppContainerRendered(this.appStackRef.instance);
    }
    render() {
        return (FSComponent.buildComponent("div", { ref: this.appMainRef, class: `app-container` },
            FSComponent.buildComponent(DefaultAppViewStackContainer, { ref: this.appStackRef })));
    }
}
class DefaultAppViewStackContainer extends DisplayComponent {
    constructor() {
        super(...arguments);
        this.rootRef = FSComponent.createRef();
    }
    renderView(view) {
        FSComponent.render(view, this.rootRef.instance);
    }
    render() {
        return FSComponent.buildComponent("div", { ref: this.rootRef, class: mergeClassProp('app-view-stack', this.props.class) });
    }
}
