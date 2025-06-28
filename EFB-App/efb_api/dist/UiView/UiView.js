import { DisplayComponent } from '@microsoft/msfs-sdk';
export class UiView extends DisplayComponent {
    // eslint-disable-next-line @typescript-eslint/no-empty-function
    onOpen() { }
    // eslint-disable-next-line @typescript-eslint/no-empty-function
    onClose() { }
    // eslint-disable-next-line @typescript-eslint/no-empty-function
    onResume() { }
    // eslint-disable-next-line @typescript-eslint/no-empty-function
    onPause() { }
    // eslint-disable-next-line @typescript-eslint/no-empty-function, @typescript-eslint/no-unused-vars
    onUpdate(time) { }
    destroy() {
        super.destroy();
    }
}
