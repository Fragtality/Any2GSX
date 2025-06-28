import { ArraySubject } from '@microsoft/msfs-sdk';
import { App } from './App';
let uid = 0;
/** EFB Instance */
export class Container {
    // eslint-disable-next-line @typescript-eslint/no-empty-function
    constructor() {
        /** @private */
        this._uid = uid++;
        /** App install promises */
        this._registeredAppsPromises = [];
        /** Installed apps */
        this._installedApps = ArraySubject.create();
    }
    /**
     * Static singleton instance of Efb container
     * @internal
     */
    static get instance() {
        return (window.EFB_API = Container._instance = window.EFB_API || Container._instance || new Container());
    }
    /** @internal */
    apps() {
        return this._installedApps;
    }
    /** @internal */
    allAppsLoaded() {
        return this._registeredAppsPromises.length === this._installedApps.length;
    }
    /**
     * Method used by the OS to share the bus to apps
     * @internal
     */
    setBus(bus) {
        this.bus = bus;
        return this;
    }
    /**
     * Method used by the OS to share the units settings manager to the apps
     * @internal
     */
    setUnitsSettingManager(unitsSettingManager) {
        this.unitsSettingManager = unitsSettingManager;
        return this;
    }
    /**
     * Method used by the OS to share the settings manager to the apps
     * @internal
     */
    setEfbSettingManager(efbSettingsManager) {
        this.efbSettingsManager = efbSettingsManager;
        return this;
    }
    setOnboardingManager(onboardingManager) {
        this.onboardingManager = onboardingManager;
        return this;
    }
    /**
     * Method used by the OS to share the notification manager to the apps
     * @internal
     */
    setNotificationManager(notificationManager) {
        this.notificationManager = notificationManager;
        return this;
    }
    /**
     * Load stylesheet
     * @param uri
     * @returns Promise which is resolved when stylesheet is loaded or rejected if an error occur.
     */
    async loadCss(uri) {
        if (document.querySelector(`link[href*="${uri}"]`)) {
            return Promise.reject(`${uri} already loaded.`);
        }
        const linkTag = document.createElement('link');
        linkTag.rel = 'stylesheet';
        linkTag.href = uri;
        document.head.append(linkTag);
        return new Promise((resolve, reject) => {
            linkTag.onload = () => resolve();
            linkTag.onerror = reject;
        });
    }
    /**
     * Load script file
     * @param uri
     * @returns Promise which is resolved when script is loaded or rejected if an error occur.
     */
    async loadJs(uri) {
        if (document.querySelector(`script[src*="${uri}"]`)) {
            return Promise.reject(`${uri} already loaded.`);
        }
        const scriptTag = document.createElement('script');
        scriptTag.type = 'text/javascript';
        scriptTag.src = uri;
        document.head.append(scriptTag);
        return new Promise((resolve, reject) => {
            scriptTag.onload = () => resolve();
            scriptTag.onerror = reject;
        });
    }
    /**
     * Register an app in EFB
     * @template T - App registration options
     * @param app The app you wan't to register
     * @param options Options you'r app might need when installing
     * @returns EFB instance
     * @throws Throw an error if App install went wrong.
     */
    use(app, ...options) {
        var _a;
        try {
            if (!this.bus) {
                throw new Error(`Bus has not been initialized yet.`);
            }
            const appInstance = app instanceof App ? app : new app();
            const installProps = {
                bus: this.bus,
                unitsSettingManager: this.unitsSettingManager,
                efbSettingsManager: this.efbSettingsManager,
                notificationManager: this.notificationManager,
                onboardingManager: this.onboardingManager,
                options: Object.keys(options)
                    .filter((key) => key !== 'isCoreApp')
                    .reduce((acc, key) => {
                    return Object.assign(acc, options[key]);
                }, {}),
            };
            const appInstaller = appInstance._install.apply(appInstance, [installProps]);
            const name = appInstance.internalName;
            // This check is done after install applied to the app as it allows to have a dynamic name based on options
            if (/\s/.test(name)) {
                throw new Error(`The App name can't have any whitespace character. "${name}"`);
            }
            if (!Object.prototype.hasOwnProperty.call(installProps.options, 'isCoreApp')) {
                this._registeredAppsPromises.push(appInstaller.then(() => {
                    this._installedApps.insert(appInstance);
                }));
            }
        }
        catch (e) {
            (_a = document.currentScript) === null || _a === void 0 ? void 0 : _a.remove();
            console.error(`App can't be installed`, e);
            throw e;
        }
        return this;
    }
}
/** EFB Instance */
export const Efb = Container.instance;
