import { FSComponent, SubscribableArrayEventType, } from '@microsoft/msfs-sdk';
import { GamepadUiComponent } from '../../Gamepad';
export class List extends GamepadUiComponent {
    constructor() {
        super(...arguments);
        this._itemInstanceRefs = [];
        this.dataSubs = [];
    }
    onAfterRender(node) {
        var _a;
        super.onAfterRender(node);
        this.renderList();
        this.dataSubs.push(this.props.data.sub(this.onDataChanged.bind(this)));
        const isListVisibleSub = (_a = this.props.isListVisible) === null || _a === void 0 ? void 0 : _a.sub((isVisible) => {
            if (isVisible && this._itemInstanceRefs.length > 0) {
                this.show();
            }
            else {
                this.hide();
            }
        }, true);
        if (isListVisibleSub) {
            this.dataSubs.push(isListVisibleSub);
        }
    }
    destroy() {
        this.dataSubs.forEach((sub) => sub.destroy());
        super.destroy();
    }
    show() {
        this.gamepadUiComponentRef.instance.classList.remove('hide');
    }
    hide() {
        this.gamepadUiComponentRef.instance.classList.add('hide');
    }
    onDataChanged(index, type, item) {
        switch (type) {
            case SubscribableArrayEventType.Added:
                {
                    if (!item)
                        return;
                    const el = this.gamepadUiComponentRef.instance.children.item(index);
                    if (Array.isArray(item)) {
                        for (let i = 0; i < item.length; i++) {
                            this.addDomNode(item[i], index + i, el);
                        }
                    }
                    else {
                        this.addDomNode(item, index, el);
                    }
                }
                break;
            case SubscribableArrayEventType.Removed:
                {
                    if (!item)
                        return;
                    if (Array.isArray(item)) {
                        item.forEach((_) => {
                            this.removeDomNode(index);
                        });
                    }
                    else {
                        this.removeDomNode(index);
                    }
                }
                break;
            case SubscribableArrayEventType.Cleared:
                {
                    for (const componentRef of this._itemInstanceRefs) {
                        componentRef.destroy();
                    }
                    this._itemInstanceRefs.length = 0;
                    this.gamepadUiComponentRef.instance.innerHTML = '';
                }
                break;
        }
        if (this.props.refreshOnUpdate) {
            this.renderList();
        }
        else {
            this.refreshListVisibility();
        }
    }
    addDomNode(item, index, el) {
        const node = this.renderListItem(item, index);
        if (!node)
            return;
        if (el !== null) {
            FSComponent.renderBefore(node, el);
        }
        else {
            el = this.gamepadUiComponentRef.instance;
            FSComponent.render(node, el);
        }
        if (node.instance !== null) {
            this._itemInstanceRefs.splice(index, 0, node.instance);
        }
    }
    renderListItem(dataItem, index) {
        const renderedNode = this.props.renderItem(dataItem, index);
        if (renderedNode &&
            renderedNode.instance instanceof GamepadUiComponent &&
            renderedNode.instance.gamepadUiComponentRef.instance !== null) {
            return renderedNode;
        }
        else {
            throw new Error('A ListItem must be of type GamepadUiComponent');
        }
    }
    removeDomNode(index) {
        const child = this.gamepadUiComponentRef.instance.childNodes.item(index);
        this.gamepadUiComponentRef.instance.removeChild(child);
        const removed = this._itemInstanceRefs.splice(index, 1)[0];
        removed.destroy();
    }
    renderList() {
        // Clean destroy existing element before rerendering them
        for (const componentRef of this._itemInstanceRefs) {
            componentRef.destroy();
        }
        this.gamepadUiComponentRef.instance.textContent = '';
        this._itemInstanceRefs.length = 0;
        const dataLen = this.props.data.length;
        for (let i = 0; i < dataLen; i++) {
            const vnode = this.renderListItem(this.props.data.get(i), i);
            if (vnode) {
                FSComponent.render(vnode, this.gamepadUiComponentRef.instance);
                this._itemInstanceRefs.push(vnode.instance);
            }
        }
        this.refreshListVisibility();
    }
    refreshListVisibility() {
        var _a;
        // TODO: check usage, it was breaking airport runway list
        const isVisible = ((_a = this.props.isListVisible) === null || _a === void 0 ? void 0 : _a.get()) !== false;
        isVisible && this._itemInstanceRefs.length > 0 ? this.show() : this.hide();
        return;
    }
    render() {
        return (FSComponent.buildComponent("div", { class: `list ${this.props.isScrollable ? 'scroll-container' : ''}`, style: this.props.style, ref: this.gamepadUiComponentRef }));
    }
}
