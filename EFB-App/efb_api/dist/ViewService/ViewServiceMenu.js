import { FSComponent, SubscribableMapFunctions } from '@microsoft/msfs-sdk';
import { TabSelector } from '../Components';
import { GamepadUiComponent } from '../Gamepad';
import { UiView } from '../UiView';
import { SubscribableMapEventType } from '../sub';
import { mergeClassProp } from '../utils';
export class ViewServiceMenu extends GamepadUiComponent {
    constructor() {
        super(...arguments);
        this.tabs = [];
    }
    tabRender(tab) {
        return (FSComponent.buildComponent(TabSelector, { ref: tab.tabRef, tabName: tab.view.ref.tabName, callback: () => {
                this.props.viewService.openPage(tab.view.key);
            }, active: tab.view.isActive, disabled: tab.view.isDisabled, hidden: tab.view.isTabVisible.map(SubscribableMapFunctions.not()) }));
    }
    render() {
        return FSComponent.buildComponent("div", { ref: this.gamepadUiComponentRef, class: mergeClassProp('tabs', this.props.class) });
    }
    onAfterRender(node) {
        super.onAfterRender(node);
        this.viewsSub = this.props.viewService.getRegisteredViews().sub((key, type, view) => {
            switch (type) {
                case SubscribableMapEventType.Added:
                    {
                        if (view === undefined || !(view.ref instanceof UiView) || !view.ref.tabName) {
                            return;
                        }
                        const tab = {
                            key,
                            tabRef: FSComponent.createRef(),
                            view,
                        };
                        FSComponent.render(this.tabRender(tab), this.gamepadUiComponentRef.instance);
                        this.tabs.push(tab);
                    }
                    break;
                case SubscribableMapEventType.Removed:
                    {
                        const index = this.tabs.findIndex((el) => el.key === key);
                        if (index === -1) {
                            return;
                        }
                        const removed = this.tabs.splice(index, 1)[0];
                        removed.tabRef.instance.destroy();
                        const child = this.gamepadUiComponentRef.instance.childNodes.item(index);
                        this.gamepadUiComponentRef.instance.removeChild(child);
                    }
                    break;
            }
        }, true);
    }
    destroy() {
        var _a;
        (_a = this.viewsSub) === null || _a === void 0 ? void 0 : _a.destroy();
        super.destroy();
    }
}
