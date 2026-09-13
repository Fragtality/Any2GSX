import { EventBus, MutableSubscribable, Subject, Subscribable, Subscription } from '@microsoft/msfs-sdk';
import { Nullable } from '../types';

export declare enum GameMode {
    UNKNOWN = 0,
    CAREER = 1,
    CHALLENGE = 2,
    DISCOVERY = 3,
    FREEFLIGHT = 4
}
declare global {
    interface Window {
        GAME_MODE_MANAGER: _GameModeManager | undefined;
    }
}
declare class _GameModeManager {
    private static _instance?;
    protected readonly _gameMode: MutableSubscribable<number, GameMode>;
    /** Public getters */
    readonly gameMode: Subscribable<GameMode>;
    readonly isCareer: import('@microsoft/msfs-sdk').MappedSubscribable<boolean>;
    readonly isChallenge: import('@microsoft/msfs-sdk').MappedSubscribable<boolean>;
    readonly isDiscovery: import('@microsoft/msfs-sdk').MappedSubscribable<boolean>;
    readonly isFreeflight: import('@microsoft/msfs-sdk').MappedSubscribable<boolean>;
    readonly isInMenu: Subject<boolean>;
    protected onGameModeChangedSubscription: Nullable<Subscription>;
    protected onIsInMenuSubscription: Nullable<Subscription>;
    private constructor();
    /**
     * Static singleton instance of the Game mode manager
     * @internal
     */
    static get instance(): _GameModeManager;
    /**
     * The bus is set once at EFB initialization from efb_ui.tsx
     * @internal
     */
    setBus(bus: EventBus): void;
}
export declare const GameModeManager: _GameModeManager;
export {};
