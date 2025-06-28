import { ComponentProps, DisplayComponent, EventBus, Subscribable } from '@microsoft/msfs-sdk';
import { AppViewService } from '../AppView';

export interface UiViewProps extends ComponentProps {
    /** The AppViewService instance */
    appViewService?: AppViewService;
    /** The event bus */
    bus?: EventBus;
}
export declare abstract class UiView<P extends UiViewProps = UiViewProps> extends DisplayComponent<P> {
    abstract readonly tabName: string | Subscribable<string>;
    onOpen(): void;
    onClose(): void;
    onResume(): void;
    onPause(): void;
    onUpdate(time: number): void;
    destroy(): void;
}
//# sourceMappingURL=UiView.d.ts.map