import { EventBus } from '@microsoft/msfs-sdk';
import { UnitsSettingsManager } from './UnitsSettingsManager';

/**
 * Utility class for retrieving display units setting managers.
 * @internal
 */
export declare class UnitsSettings {
    private static INSTANCE;
    /**
     * Retrieves a manager for display units settings.
     * @param bus The event bus.
     * @returns a manager for display units settings.
     */
    static getManager(bus: EventBus): UnitsSettingsManager;
}
