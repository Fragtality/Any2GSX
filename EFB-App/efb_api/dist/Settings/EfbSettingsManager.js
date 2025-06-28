import { DefaultUserSettingManager, } from '@microsoft/msfs-sdk';
import { Efb } from '../EfbApi';
import { checkUserSetting } from '../utils';
export var EfbMode;
(function (EfbMode) {
    EfbMode[EfbMode["2D"] = 0] = "2D";
    EfbMode[EfbMode["3D"] = 1] = "3D";
})(EfbMode || (EfbMode = {}));
export var EfbSizeSettingMode;
(function (EfbSizeSettingMode) {
    EfbSizeSettingMode[EfbSizeSettingMode["Small"] = 0] = "Small";
    EfbSizeSettingMode[EfbSizeSettingMode["Medium"] = 1] = "Medium";
    EfbSizeSettingMode[EfbSizeSettingMode["Large"] = 2] = "Large";
})(EfbSizeSettingMode || (EfbSizeSettingMode = {}));
export var OrientationSettingMode;
(function (OrientationSettingMode) {
    OrientationSettingMode[OrientationSettingMode["Vertical"] = 0] = "Vertical";
    OrientationSettingMode[OrientationSettingMode["Horizontal"] = 1] = "Horizontal";
})(OrientationSettingMode || (OrientationSettingMode = {}));
export class EfbSettingsManager extends DefaultUserSettingManager {
    constructor(bus, settingsDefs) {
        super(bus, settingsDefs);
        // Favorite apps array initialisation
        let stringToArray = [];
        try {
            stringToArray = JSON.parse(this.getSetting('favoriteApps').get());
        }
        catch (error) {
            console.error('JSON failed, impossible to parse : ', this.getSetting('favoriteApps').get());
            stringToArray = ['AtlasApp', 'AircraftApp', 'PilotBookApp', 'SettingsApp'];
        }
        this.favoriteApps = stringToArray;
    }
    get favoriteAppsArray() {
        return this.favoriteApps;
    }
    /**
     * Checks if the values loaded from the datastorage correspond to the settings types.
     */
    checkLoadedValues() {
        checkUserSetting(this.getSetting('efbSize'), EfbSizeSettingMode);
        checkUserSetting(this.getSetting('orientationMode'), OrientationSettingMode);
    }
    /**
     * Add an app to the favorites
     * @param app The app to add
     * @returns the EFB settings manager
     */
    addAppToFavorites(app) {
        this.favoriteApps.push(app.internalName);
        this.onFavoriteAppsUpdated();
        return this;
    }
    /**
     * Remove an app from the favorites
     * @param app The app to remove
     * @returns the EFB settings manager
     */
    removeAppFromFavorites(app) {
        const index = this.favoriteApps.indexOf(app.internalName);
        if (index !== -1) {
            this.favoriteApps.splice(index, 1);
        }
        this.onFavoriteAppsUpdated();
        return this;
    }
    /**
     * Update the favoriteSetting and the apps array in the EFB instance in order to rerender when the favorite apps change
     */
    onFavoriteAppsUpdated() {
        /** Update the manager setting */
        const stringSetting = JSON.stringify(this.favoriteApps);
        this.getSetting('favoriteApps').set(stringSetting);
        /** Update the favorite indexes of the apps stocked in the EFB instance */
        Efb.apps()
            .getArray()
            .forEach((app) => {
            app.favoriteIndex = this.favoriteApps.indexOf(app.internalName);
        });
        this.bus.pub('favs-update', '');
    }
    updateFavoriteAppsArray(value) {
        let arraySetting = [];
        try {
            arraySetting = JSON.parse(value);
        }
        catch (error) {
            console.error('JSON failed, impossible to parse : ', value);
            arraySetting = ['AtlasApp', 'AircraftApp', 'PilotBookApp', 'SettingsApp'];
        }
        this.favoriteApps = arraySetting;
    }
    onSettingValueChanged(entry, value) {
        super.onSettingValueChanged(entry, value);
        const settingName = entry.setting.definition.name;
        switch (settingName) {
            case 'efbSize':
                if (typeof value === 'string') {
                    value = EfbSizeSettingMode[value.toString()];
                }
                Coherent.call('SET_SIZE', value);
                break;
            case 'orientationMode':
                if (typeof value === 'string') {
                    value = OrientationSettingMode[value.toString()];
                }
                Coherent.call('SET_ORIENTATION', value);
                break;
            case 'isBrightnessAuto':
                if (typeof value !== 'boolean') {
                    break;
                }
                if (value === true) {
                    this.getSetting('manualBrightnessPercentage').set(this.getSetting('autoBrightnessPercentage').get());
                }
                Coherent.call('SET_IS_AUTO_BRIGHTNESS', this.getSetting('isBrightnessAuto').get());
                break;
            case 'manualBrightnessPercentage':
                if (this.getSetting('isBrightnessAuto').get()) {
                    break;
                }
                if (typeof value !== 'number') {
                    break;
                }
                Coherent.call('SET_MANUAL_BRIGHTNESS', value / 100);
                break;
            case 'favoriteApps':
                this.updateFavoriteAppsArray(value.toString());
                break;
        }
    }
}
/**
 * Utility class for retrieving EFB setting managers.
 * @internal
 */
export class EfbSettings {
    constructor() {
        // Do nothing
    }
    static getManager(bus) {
        var _a;
        return ((_a = EfbSettings.INSTANCE) !== null && _a !== void 0 ? _a : (EfbSettings.INSTANCE = new EfbSettingsManager(bus, [
            {
                name: 'mode',
                defaultValue: EfbMode['2D'],
            },
            {
                name: 'efbSize',
                defaultValue: EfbSizeSettingMode.Small,
            },
            {
                name: 'orientationMode',
                defaultValue: OrientationSettingMode.Vertical,
            },
            {
                name: 'isBrightnessAuto',
                defaultValue: true,
            },
            {
                name: 'autoBrightnessPercentage',
                defaultValue: 50,
            },
            {
                name: 'manualBrightnessPercentage',
                defaultValue: 50,
            },
            {
                name: 'favoriteApps',
                defaultValue: '["AtlasApp","AircraftApp","PilotBookApp","SettingsApp"]',
            },
            {
                name: 'defaultApp',
                defaultValue: 'AtlasApp',
            },
        ])));
    }
}
