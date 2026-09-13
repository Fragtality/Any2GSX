import { Subscribable } from '@microsoft/msfs-sdk';

declare global {
    interface Window {
        PanelSize: PanelSize;
    }
}
export interface PanelSize {
    readonly width: Subscribable<number>;
    readonly height: Subscribable<number>;
}
