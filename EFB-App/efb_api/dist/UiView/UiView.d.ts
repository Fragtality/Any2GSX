import { ComponentProps, DisplayComponent, EventBus, Subscribable } from '@microsoft/msfs-sdk';
import { AppViewService } from '../AppView';
import { Service } from '../types';

export interface UiViewProps extends ComponentProps {
    /** The AppViewService instance */
    appViewService?: AppViewService;
    /** The event bus */
    bus?: EventBus;
}
export declare abstract class UiView<P extends UiViewProps = UiViewProps> extends DisplayComponent<P> implements Service {
    abstract readonly tabName: string | Subscribable<string>;
    /**
     * Array of Services your view needs.
     * Automatically binds every hooks to thoses services if they are used.
     */
    protected readonly services: Service[];
    onOpen(): void;
    onClose(): void;
    onResume(): void;
    onPause(): void;
    onUpdate(time: number): void;
    destroy(): void;
}
