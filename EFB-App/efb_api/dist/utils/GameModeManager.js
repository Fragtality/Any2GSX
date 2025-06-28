import { Subject } from '@microsoft/msfs-sdk';
import { where } from '../sub';
export var GameMode;
(function (GameMode) {
    GameMode[GameMode["UNKNOWN"] = 0] = "UNKNOWN";
    GameMode[GameMode["CAREER"] = 1] = "CAREER";
    GameMode[GameMode["CHALLENGE"] = 2] = "CHALLENGE";
    GameMode[GameMode["DISCOVERY"] = 3] = "DISCOVERY";
    GameMode[GameMode["FREEFLIGHT"] = 4] = "FREEFLIGHT";
})(GameMode || (GameMode = {}));
class _GameModeManager {
    // eslint-disable-next-line @typescript-eslint/no-empty-function
    constructor() {
        this._gameMode = Subject.create(GameMode.UNKNOWN);
        /** Public getters */
        this.gameMode = this._gameMode;
        this.isCareer = this._gameMode.map(where(GameMode.CAREER));
        this.isChallenge = this._gameMode.map(where(GameMode.CHALLENGE));
        this.isDiscovery = this._gameMode.map(where(GameMode.DISCOVERY));
        this.isFreeflight = this._gameMode.map(where(GameMode.FREEFLIGHT));
        this.onGameModeChangedSubscription = null;
    }
    /**
     * Static singleton instance of the Game mode manager
     * @internal
     */
    static get instance() {
        return (window.GAME_MODE_MANAGER = _GameModeManager._instance =
            window.GAME_MODE_MANAGER || _GameModeManager._instance || new _GameModeManager());
    }
    /**
     * The bus is set once at EFB initialization from efb_ui.tsx
     * @internal
     */
    setBus(bus) {
        var _a;
        (_a = this.onGameModeChangedSubscription) === null || _a === void 0 ? void 0 : _a.destroy();
        this.onGameModeChangedSubscription = bus.on('GameModeChanged', (gameMode) => {
            switch (gameMode) {
                case 'CAREER GAMEMODE':
                    this._gameMode.set(GameMode.CAREER);
                    break;
                case 'CHALLENGE GAMEMODE':
                    this._gameMode.set(GameMode.CHALLENGE);
                    break;
                case 'DISCOVERY GAMEMODE':
                    this._gameMode.set(GameMode.DISCOVERY);
                    break;
                case 'FREEFLIGHT GAMEMODE':
                    this._gameMode.set(GameMode.FREEFLIGHT);
                    break;
                default:
                    console.error(`Unknown game mode '${gameMode}'`);
                    this._gameMode.set(GameMode.UNKNOWN);
            }
        });
    }
}
export const GameModeManager = _GameModeManager.instance;
