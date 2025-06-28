import { UserSettingSaveManager } from '@microsoft/msfs-sdk';
import { EfbSettings } from './EfbSettingsManager';
import { UnitsSettings } from './UnitsSettings';
/**
 * A manager for EFB user settings that are saved and persistent across flight sessions.
 * @internal
 */
export class EfbSettingsSaveManager extends UserSettingSaveManager {
    constructor(bus) {
        super([...EfbSettings.getManager(bus).getAllSettings(), ...UnitsSettings.getManager(bus).getAllSettings()], bus);
        this.bus = bus;
        this.prefix = 'efb-2024-08-28.';
        this.settings = [
            ...EfbSettings.getManager(this.bus).getAllSettings(),
            ...UnitsSettings.getManager(this.bus).getAllSettings(),
        ];
    }
    load(key) {
        super.load(`${this.prefix}${key}`);
        UnitsSettings.getManager(this.bus).checkLoadedValues();
        EfbSettings.getManager(this.bus).checkLoadedValues();
        this.save(`${this.prefix}${key}`);
    }
    save(key) {
        super.save(`${this.prefix}${key}`);
    }
    startAutoSave(key) {
        super.startAutoSave(`${this.prefix}${key}`);
    }
    stopAutoSave(key) {
        super.stopAutoSave(`${this.prefix}${key}`);
    }
    pruneOldPrefixes() {
        const storage = GetDataStorage().searchData('efb');
        for (const entry of storage) {
            if (entry.key.indexOf(`.${this.prefix}`) === -1) {
                GetDataStorage().deleteData(entry.key);
            }
        }
    }
}
