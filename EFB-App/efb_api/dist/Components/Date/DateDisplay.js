import { DisplayComponent, FSComponent, Subject, SubscribableUtils, } from '@microsoft/msfs-sdk';
import { TT } from '../../i18n';
import { dayKeys, monthKeys, monthShortKeys, toClassProp } from '../../utils';
const INTERNAL_KEY_SEPARATOR = '__MSFS2024_INTERNAL_DATE_ANA_ACE__';
const FormatKeys = ['YYYY', 'YY', 'MMMM', 'MMM', 'MM', 'DDDD', 'DD', 'hh', 'mm', 'ss'];
const dateToValue = {
    YYYY: (date) => date.getFullYear().toString(),
    YY: (date) => (date.getFullYear() % 100).toString().padStart(2, '0'),
    MMMM: (date) => monthKeys[date.getMonth()],
    MMM: (date) => monthShortKeys[date.getMonth()],
    MM: (date) => (date.getMonth() + 1).toString().padStart(2, '0'),
    DDDD: (date) => dayKeys[date.getDay()],
    DD: (date) => date.getDate().toString().padStart(2, '0'),
    hh: (date) => date.getHours().toString().padStart(2, '0'),
    mm: (date) => date.getMinutes().toString().padStart(2, '0'),
    ss: (date) => date.getSeconds().toString().padStart(2, '0'),
};
const formatToInternalKey = {
    YYYY: 'FULLYEAR',
    YY: 'SHORTYEAR',
    MMMM: 'FULLMONTH',
    MMM: 'SHORTMONTH',
    MM: 'DIGITMONTH',
    DDDD: 'FULLDAY',
    DD: 'DIGITDAY',
    hh: 'HOURS',
    mm: 'MINUTES',
    ss: 'SECONDS',
};
export class DateDisplay extends DisplayComponent {
    constructor() {
        super(...arguments);
        this.ready = Subject.create(false);
        this.date = SubscribableUtils.toSubscribable(this.props.date, true);
        this.format = SubscribableUtils.toSubscribable(this.props.format, true);
        this.dateSubscription = this.date.sub((date) => this.updateDate(date));
        this.formatSubscription = this.format.sub((format) => {
            this.renderFormat(this.date.get(), format);
        });
        this.dateSpanRef = FSComponent.createRef();
        this.dateNodes = []; /* Array of span and TT components */
        this.cachedSubject = {
            YYYY: Subject.create(''),
            YY: Subject.create(''),
            MMMM: Subject.create(''),
            MMM: Subject.create(''),
            MM: Subject.create(''),
            DDDD: Subject.create(''),
            DD: Subject.create(''),
            hh: Subject.create(''),
            mm: Subject.create(''),
            ss: Subject.create(''),
        };
    }
    renderFormat(rawDate, format) {
        if (!this.ready.get()) {
            return;
        }
        this.dateNodes.forEach((node) => FSComponent.shallowDestroy(node));
        this.dateSpanRef.instance.innerHTML = '';
        this.updateDate(rawDate);
        let editedFormat = format;
        for (const key in formatToInternalKey) {
            editedFormat = editedFormat.replace(key, `${INTERNAL_KEY_SEPARATOR}${formatToInternalKey[key]}${INTERNAL_KEY_SEPARATOR}`);
        }
        const splittedFormat = editedFormat.split(INTERNAL_KEY_SEPARATOR);
        for (const part of splittedFormat) {
            const foundKey = FormatKeys.find((key) => formatToInternalKey[key] === part);
            if (foundKey !== undefined) {
                this.dateNodes.push(FSComponent.buildComponent(TT, { key: this.cachedSubject[foundKey] }));
            }
            else {
                this.dateNodes.push(FSComponent.buildComponent("span", null, part));
            }
        }
        FSComponent.render(FSComponent.buildComponent(FSComponent.Fragment, null, this.dateNodes), this.dateSpanRef.instance);
    }
    updateDate(rawDate) {
        if (!this.ready.get()) {
            return;
        }
        const date = new Date(rawDate);
        for (const key in this.cachedSubject) {
            this.cachedSubject[key].set(dateToValue[key](date));
        }
    }
    onAfterRender(node) {
        super.onAfterRender(node);
        this.ready.set(true);
        this.renderFormat(this.date.get(), this.format.get());
    }
    render() {
        return FSComponent.buildComponent("span", { style: this.props.style, class: toClassProp(this.props.class), ref: this.dateSpanRef });
    }
    destroy() {
        this.dateSubscription.destroy();
        this.formatSubscription.destroy();
        super.destroy();
    }
}
