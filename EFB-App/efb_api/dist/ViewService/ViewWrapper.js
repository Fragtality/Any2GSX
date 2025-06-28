import { DisplayComponent, FSComponent, SetSubject, } from '@microsoft/msfs-sdk';
export class ViewWrapper extends DisplayComponent {
    constructor() {
        super(...arguments);
        this.rootRef = FSComponent.createRef();
        this.classList = SetSubject.create(['view', 'hidden', this.props.viewName]);
        this.subs = [];
    }
    onAfterRender(node) {
        super.onAfterRender(node);
        this.subs.push(this.props.isActive.sub((isActive) => {
            if (isActive) {
                this.classList.delete('hidden');
            }
            else {
                this.classList.add('hidden');
            }
        }));
    }
    render() {
        return (FSComponent.buildComponent("div", { ref: this.rootRef, class: this.classList }, this.props.children));
    }
    destroy() {
        var _a;
        const root = this.rootRef.getOrDefault();
        if (root !== null) {
            (_a = root.parentNode) === null || _a === void 0 ? void 0 : _a.removeChild(root);
        }
        this.subs.forEach((x) => x.destroy());
        super.destroy();
    }
}
