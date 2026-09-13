import { Subscribable } from '@microsoft/msfs-sdk';
import { OrientationSettingMode } from '../Settings';
import { PanelSize } from './PanelSize';

declare global {
    interface Window {
        PanelInfo: PanelInfo;
    }
}
export interface PanelInfo {
    is2D: Subscribable<boolean>;
    orientation: Subscribable<OrientationSettingMode>;
    size: PanelSize;
}
