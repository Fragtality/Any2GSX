import { AppBootMode, AppSuspendMode } from './AppLifecycle';
/**
 * ```
 * Class that all Apps must extends to be registered.
 * It is used to setup how the app is working.
 * ```
 * @template T App options. ie : you need multiple instance of an App with different styles for development.
 */
export class App {
    constructor() {
        this._isInstalled = false;
        this._isReady = false;
        this._favoriteIndex = -1;
        /**
         * Desired AppBootMode
         * @defaultValue > `AppBootMode.COLD`
         */
        this.BootMode = AppBootMode.COLD;
        /**
         * Desired AppSuspendMode
         * @defaultValue > `AppSuspendMode.SLEEP`
         */
        this.SuspendMode = AppSuspendMode.SLEEP;
    }
    /**
     * @param props
     * @internal
     */
    async _install(props) {
        var _a;
        if (this._isInstalled) {
            return Promise.reject('App already installed.');
        }
        this._isInstalled = true;
        this.bus = props.bus;
        this._unitsSettingsManager = props.unitsSettingManager;
        this._efbSettingsManager = props.efbSettingsManager;
        this._notificationManager = props.notificationManager;
        this._onboardingManager = props.onboardingManager;
        this._favoriteIndex = (_a = props.favoriteIndex) !== null && _a !== void 0 ? _a : -1;
        this.options = props.options;
        await this.install(props);
        this._isReady = true;
        return Promise.resolve();
    }
    /**
     * Install hook
     * @param props
     */
    async install(props) {
        return Promise.resolve();
    }
    /** Boolean to check if app is loaded and installed. */
    get isReady() {
        return this._isReady;
    }
    /**
     * Internal app name
     * @defaultValue > Class's name (`this.constructor.name`)
     */
    get internalName() {
        return this.constructor.name;
    }
    /**
     * EFB units settings manager
     * @returns a unique unitsSettingsManager instance
     */
    get unitsSettingsManager() {
        const unitsSettingsManager = this._unitsSettingsManager;
        if (!unitsSettingsManager) {
            throw new Error('Units settings manager is not defined');
        }
        return unitsSettingsManager;
    }
    /**
     * EFB settings manager
     * @returns a unique efbSettingsManager instance
     */
    get efbSettingsManager() {
        const efbSettingsManager = this._efbSettingsManager;
        if (!efbSettingsManager) {
            throw new Error('EFB settings manager is not defined');
        }
        return efbSettingsManager;
    }
    /**
     * EFB notification manager
     * @returns a unique efbNotificationManager instance
     */
    get notificationManager() {
        const notificationManager = this._notificationManager;
        if (!notificationManager) {
            throw new Error('Notification manager is not defined');
        }
        return notificationManager;
    }
    /** Onboarding manager */
    get onboardingManager() {
        const onboardingManager = this._onboardingManager;
        if (!onboardingManager) {
            throw new Error('Onboarding manager is not defined');
        }
        return onboardingManager;
    }
    /** @internal */
    get favoriteIndex() {
        return this._favoriteIndex;
    }
    /** @internal */
    set favoriteIndex(index) {
        this._favoriteIndex = index;
    }
}
