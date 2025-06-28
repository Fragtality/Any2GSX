var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};
import { DisplayComponent, EventBus, FSComponent } from '@microsoft/msfs-sdk';
import { Memoize } from 'typescript-memoize';
import { AppViewService } from '../AppView/AppViewService';
import { OnboardingManager } from '../Managers/OnboardingManager/OnboardingManager';
import { EfbSettingsManager, UnitsSettingsManager } from '../Settings';
import { toPromise } from '../utils';
import { AppContainer } from './AppContainer';
/**
 * ```
 * Mandatory class to extend for the App's render method
 * It's the entrypoint view of every EFB apps.
 * All events from the EFB are sended to this class when the app is visible.
 * ```
 */
export class AppView extends DisplayComponent {
    /**
     * Get AppViewService instance
     * @throws {Error}
     */
    get appViewService() {
        const _appViewService = this._appViewService;
        if (!_appViewService) {
            throw new Error(`Cannot resolve 'appViewService' because none of 'appViewService' and 'bus' props were given.`);
        }
        return _appViewService;
    }
    /**
     * Get EventBus instance
     * @throws {Error}
     */
    get bus() {
        const _bus = this._bus;
        if (!_bus) {
            throw new Error(`Cannot resolve 'bus' because none of 'appViewService' and 'bus' props were given.`);
        }
        return _bus;
    }
    /**
     * Get units setting manager instance
     * @throws {Error}
     */
    get unitsSettingManager() {
        const _unitsSettingManager = this._unitsSettingManager;
        if (_unitsSettingManager === undefined) {
            throw new Error(`Cannot resolve 'unitsSettingManager'`);
        }
        return _unitsSettingManager;
    }
    /**
     * Get general setting manager instance
     * @throws {Error}
     */
    get efbSettingsManager() {
        const _efbSettingsManager = this._efbSettingsManager;
        if (_efbSettingsManager === undefined) {
            throw new Error(`Cannot resolve 'efbSettingsManager'`);
        }
        return _efbSettingsManager;
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
    /** Get onboarding manager instance
     * @throws {Error}
     */
    get onboardingManager() {
        const _onboardingManager = this._onboardingManager;
        if (!_onboardingManager) {
            throw new Error(`Cannot resolve 'onboardingManager'`);
        }
        return _onboardingManager;
    }
    constructor(props) {
        var _a;
        super(props);
        this.rootRef = FSComponent.createRef();
        this.pageKeyActions = new Map();
        this._appViewService = props.appViewService || (props.bus ? new AppViewService(props.bus) : undefined);
        this._bus = (_a = this._appViewService) === null || _a === void 0 ? void 0 : _a.bus;
        this._unitsSettingManager = props.unitsSettingManager;
        this._efbSettingsManager = props.efbSettingsManager;
        this._notificationManager = props.notificationManager;
        this._onboardingManager = props.onboardingManager;
    }
    /**
     * Called once when the view is opened for the first time.
     */
    onOpen() {
        var _a;
        (_a = this._appViewService) === null || _a === void 0 ? void 0 : _a.onOpen();
    }
    /**
     * Called once when the view is destroyed.
     */
    onClose() {
        var _a;
        (_a = this._appViewService) === null || _a === void 0 ? void 0 : _a.onClose();
    }
    /**
     * Called each time the view is resumed.
     */
    onResume() {
        var _a;
        (_a = this._appViewService) === null || _a === void 0 ? void 0 : _a.onResume();
    }
    /**
     * Called each time the view is closed.
     */
    onPause() {
        var _a;
        (_a = this._appViewService) === null || _a === void 0 ? void 0 : _a.onPause();
    }
    /**
     * On Update loop - It update the `AppViewService` if it is used.
     * @param time in milliseconds
     */
    onUpdate(time) {
        var _a;
        (_a = this._appViewService) === null || _a === void 0 ? void 0 : _a.update(time);
    }
    /**
     * Callback to register all views the app might use.
     */
    registerViews() {
        //
    }
    /**
     * @internal
     * TODO : Need to be documented after Gamepad integration
     */
    routeGamepadInteractionEvent(gamepadEvent) {
        var _a;
        (_a = this._appViewService) === null || _a === void 0 ? void 0 : _a.routeGamepadInteractionEvent(gamepadEvent);
    }
    /**
     * @internal
     * @param key custom page key defined in `pageKeyActions`
     * @param args array of arguments given to the defined callback
     */
    handlePageKeyAction(key, args) {
        if (key === null) {
            return;
        }
        const action = this.pageKeyActions.get(key);
        if (action === undefined) {
            console.error(`Key "${key}" doesn't exists.`);
            return;
        }
        toPromise(action(args)).catch((reason) => {
            console.error(`handlePageKeyAction Error for key "${key}"`, reason);
        });
    }
    /**
     * If using EFB's AppViewService, this method returns an AppContainer binded to AppViewService.
     * Otherwise it can be customized with plain JSX/TSX or custom view service, etc...
     *
     * @example
     * Surrounding AppContainer with a custom class:
     * ```ts
     * public render(): TVNode<HTMLDivElement> {
     * 	return <div class="my-custom-class">{super.render()}</div>;
     * }
     * ```
     * @example
     * Here's an plain JSX/TSX example:
     * ```ts
     * public render(): TVNode<HTMLSpanElement> {
     * 	return <span>Hello World!</span>;
     * }
     * ```
     */
    render() {
        return FSComponent.buildComponent(AppContainer, { appViewService: this.appViewService });
    }
    /** @inheritdoc */
    onAfterRender(node) {
        var _a;
        super.onAfterRender(node);
        this.registerViews();
        if (this.defaultView) {
            (_a = this._appViewService) === null || _a === void 0 ? void 0 : _a.initialize(this.defaultView);
        }
    }
    /** @internal */
    destroy() {
        var _a;
        (_a = this._appViewService) === null || _a === void 0 ? void 0 : _a.unload();
        super.destroy();
    }
}
__decorate([
    Memoize(),
    __metadata("design:type", AppViewService),
    __metadata("design:paramtypes", [])
], AppView.prototype, "appViewService", null);
__decorate([
    Memoize(),
    __metadata("design:type", EventBus),
    __metadata("design:paramtypes", [])
], AppView.prototype, "bus", null);
__decorate([
    Memoize(),
    __metadata("design:type", UnitsSettingsManager),
    __metadata("design:paramtypes", [])
], AppView.prototype, "unitsSettingManager", null);
__decorate([
    Memoize(),
    __metadata("design:type", EfbSettingsManager),
    __metadata("design:paramtypes", [])
], AppView.prototype, "efbSettingsManager", null);
__decorate([
    Memoize(),
    __metadata("design:type", OnboardingManager),
    __metadata("design:paramtypes", [])
], AppView.prototype, "onboardingManager", null);
