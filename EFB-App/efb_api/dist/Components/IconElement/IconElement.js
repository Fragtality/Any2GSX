import { DisplayComponent, FSComponent, SubscribableUtils, } from '@microsoft/msfs-sdk';
export class IconElement extends DisplayComponent {
    constructor(props) {
        super(props);
        this.url = SubscribableUtils.toSubscribable(this.props.url, true);
        this.el = FSComponent.createRef();
        this.classes = {
            'icon-element': true,
        };
        this.onIconLoaded = (found, svgAsString) => {
            this.el.instance.innerText = '';
            if (!found) {
                console.error(`Image ${this.url.get()} was not found`);
                return;
            }
            const template = document.createElement('template');
            template.innerHTML = svgAsString;
            const svgAsHtml = template.content.querySelector('svg');
            if (svgAsHtml) {
                this.el.instance.appendChild(svgAsHtml);
            }
        };
        const propsClass = props.class;
        if (propsClass) {
            Object.assign(this.classes, typeof propsClass === 'string' ? { [propsClass]: true } : propsClass);
        }
    }
    render() {
        return FSComponent.buildComponent("div", { ref: this.el, class: this.classes });
    }
    onAfterRender(node) {
        super.onAfterRender(node);
        this.urlSub = this.url.sub((url) => {
            if (!url.endsWith('.svg')) {
                console.error(`IconElement doesn't support ".${url.split('.').pop()}" filetype.`);
                return;
            }
            // Using IconCacheMgr from html_ui/common.ts - should we copy it into EFB API ?
            getIconCacheMgr().loadURL(url, this.onIconLoaded);
        }, true);
    }
    destroy() {
        var _a;
        (_a = this.urlSub) === null || _a === void 0 ? void 0 : _a.destroy();
        super.destroy();
    }
}
