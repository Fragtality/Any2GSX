var __defProp = Object.defineProperty;
var __defProps = Object.defineProperties;
var __getOwnPropDescs = Object.getOwnPropertyDescriptors;
var __getOwnPropSymbols = Object.getOwnPropertySymbols;
var __hasOwnProp = Object.prototype.hasOwnProperty;
var __propIsEnum = Object.prototype.propertyIsEnumerable;
var __defNormalProp = (obj, key, value2) => key in obj ? __defProp(obj, key, { enumerable: true, configurable: true, writable: true, value: value2 }) : obj[key] = value2;
var __spreadValues = (a, b) => {
  for (var prop in b || (b = {}))
    if (__hasOwnProp.call(b, prop))
      __defNormalProp(a, prop, b[prop]);
  if (__getOwnPropSymbols)
    for (var prop of __getOwnPropSymbols(b)) {
      if (__propIsEnum.call(b, prop))
        __defNormalProp(a, prop, b[prop]);
    }
  return a;
};
var __spreadProps = (a, b) => __defProps(a, __getOwnPropDescs(b));
var __objRest = (source, exclude) => {
  var target = {};
  for (var prop in source)
    if (__hasOwnProp.call(source, prop) && exclude.indexOf(prop) < 0)
      target[prop] = source[prop];
  if (source != null && __getOwnPropSymbols)
    for (var prop of __getOwnPropSymbols(source)) {
      if (exclude.indexOf(prop) < 0 && __propIsEnum.call(source, prop))
        target[prop] = source[prop];
    }
  return target;
};
import { ArraySubject, Subject, FacilityUtils, FacilityType, AirportUtils, UnitType, ICAO, DmsFormatter, HandlerSubscription, AbstractSubscribableArray, SubscribableArrayEventType, UUID, NumberFormatter, UnitFamily, DisplayComponent, FSComponent, SubscribableUtils, ArrayUtils, SubscribableMapFunctions, MappedSubject, Wait, ObjectSubject, SetSubject, AbstractNumberUnitDisplay, RunwayUtils, DebounceTimer, GeoPoint, FacilityLoader, FacilityRepository, FacilitySearchType, DataStore, DefaultUserSettingManager, BasicNavAngleUnit, UserSettingSaveManager } from "@microsoft/msfs-sdk";
var AppBootMode = /* @__PURE__ */ ((AppBootMode2) => {
  AppBootMode2[AppBootMode2["COLD"] = 0] = "COLD";
  AppBootMode2[AppBootMode2["WARM"] = 1] = "WARM";
  AppBootMode2[AppBootMode2["HOT"] = 2] = "HOT";
  return AppBootMode2;
})(AppBootMode || {});
var AppSuspendMode = /* @__PURE__ */ ((AppSuspendMode2) => {
  AppSuspendMode2[AppSuspendMode2["SLEEP"] = 0] = "SLEEP";
  AppSuspendMode2[AppSuspendMode2["TERMINATE"] = 1] = "TERMINATE";
  return AppSuspendMode2;
})(AppSuspendMode || {});
let uid = 0;
class Container {
  // eslint-disable-next-line @typescript-eslint/no-empty-function
  constructor() {
    this._uid = uid++;
    this._registeredAppsPromises = [];
    this._installedApps = ArraySubject.create();
  }
  /**
   * Static singleton instance of Efb container
   * @internal
   */
  static get instance() {
    return window.EFB_API = Container._instance = window.EFB_API || Container._instance || new Container();
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
    const linkTag = document.createElement("link");
    linkTag.rel = "stylesheet";
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
    const scriptTag = document.createElement("script");
    scriptTag.type = "text/javascript";
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
        options: Object.keys(options).filter((key) => key !== "isCoreApp").reduce((acc, key) => {
          return Object.assign(acc, options[key]);
        }, {})
      };
      const appInstaller = appInstance._install.apply(appInstance, [installProps]);
      const name = appInstance.internalName;
      if (/\s/.test(name)) {
        throw new Error(`The App name can't have any whitespace character. "${name}"`);
      }
      this._registeredAppsPromises.push(
        appInstaller.then(() => {
          this._installedApps.insert(appInstance);
        })
      );
    } catch (e) {
      (_a = document.currentScript) == null ? void 0 : _a.remove();
      console.error(`App can't be installed`, e);
      throw e;
    }
    return this;
  }
}
const Efb = Container.instance;
const EfbApiVersion = "1.0.3";
class App {
  constructor() {
    this._isInstalled = false;
    this._isReady = false;
    this._favoriteIndex = -1;
    this.available = Subject.create(true);
    this.BootMode = AppBootMode.COLD;
    this.SuspendMode = AppSuspendMode.SLEEP;
  }
  /**
   * @param props
   * @internal
   */
  async _install(props) {
    var _a;
    if (this._isInstalled) {
      return Promise.reject("App already installed.");
    }
    this._isInstalled = true;
    this.bus = props.bus;
    this._unitsSettingsManager = props.unitsSettingManager;
    this._efbSettingsManager = props.efbSettingsManager;
    this._notificationManager = props.notificationManager;
    this._onboardingManager = props.onboardingManager;
    this._favoriteIndex = (_a = props.favoriteIndex) != null ? _a : -1;
    this.options = props.options;
    await this.install(props);
    this._isReady = true;
    Coherent.trigger("EFB_APP_INSTALLED", this.name, this.internalName, EfbApiVersion, this.getVersion());
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
      throw new Error("Units settings manager is not defined");
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
      throw new Error("EFB settings manager is not defined");
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
      throw new Error("Notification manager is not defined");
    }
    return notificationManager;
  }
  /** Onboarding manager */
  get onboardingManager() {
    const onboardingManager = this._onboardingManager;
    if (!onboardingManager) {
      throw new Error("Onboarding manager is not defined");
    }
    return onboardingManager;
  }
  /**
   * Aircraft models list compatible with the App. If not defined, the App is compatible with all aircraft models.
   * example: ['Cabri G2', 'H125']
   * @returns a list of aircraft models compatible with the App or undefined
   */
  get compatibleAircraftModels() {
    return void 0;
  }
  /** @internal */
  get favoriteIndex() {
    return this._favoriteIndex;
  }
  /** @internal */
  set favoriteIndex(index2) {
    this._favoriteIndex = index2;
  }
  /**
   * @since 1.0.3
   * @returns True if the app is searchable (ie: app list)
   */
  getIsSearchable() {
    var _a;
    return (_a = this.options.isSearchable) != null ? _a : true;
  }
  /**
   * @since 1.0.3
   * @returns True if the app can be added to favorites
   */
  getIsFavoritable() {
    var _a;
    return (_a = this.options.isFavoritable) != null ? _a : true;
  }
  /**
   * @since 1.0.3
   * @returns Return app version
   */
  getVersion() {
    return "";
  }
}
function unique(collection) {
  return collection.reduce((acc, item) => {
    if (!acc.includes(item)) {
      acc.push(item);
    }
    return acc;
  }, []);
}
function groupBy(collection, iteratee) {
  return collection.reduce((accumulator, value2) => {
    let key = String(typeof iteratee === "function" ? iteratee(value2) : iteratee);
    if (value2 && typeof value2 === "object" && key in value2) {
      key = String(value2[key]);
    }
    if (Object.prototype.hasOwnProperty.call(accumulator, key)) {
      accumulator[key].push(value2);
    } else {
      accumulator[key] = [value2];
    }
    return accumulator;
  }, {});
}
function random(collection) {
  const length = collection.length;
  return length ? collection[Math.floor(Math.random() * length)] : void 0;
}
function chunk(collection, size) {
  size = Math.floor(size);
  if (size <= 0 || isNaN(size)) {
    return [];
  }
  const collectionLength = collection.length;
  const newCollection = new Array(Math.ceil(collectionLength / size));
  let index2 = 0;
  let resIndex = 0;
  while (index2 < collectionLength) {
    newCollection[resIndex++] = collection.slice(index2, index2 += size);
  }
  return newCollection;
}
function wrap(collection) {
  return Array.isArray(collection) ? collection : [collection];
}
function isVNode(object) {
  return "instance" in object && "props" in object && "children" in object;
}
function isIApp(object) {
  return "internalName" in object && "BootMode" in object;
}
function isConstructor(func) {
  return typeof func === "function" && !!func.prototype && func.prototype.constructor === func;
}
function isFunction(fn) {
  return typeof fn === "function";
}
function toArray(list, start = 0) {
  let i = list.length - start;
  const ret = new Array(i);
  while (i--) {
    ret[i] = list[i + start];
  }
  return ret;
}
function toClassProp(classProp) {
  if (classProp === void 0) {
    return classProp;
  }
  if (Array.isArray(classProp)) {
    return toClassProp(classProp.join(" "));
  }
  if (typeof classProp !== "string") {
    return classProp;
  }
  return classProp.split(" ").reduce(function(stack, el) {
    return __spreadProps(__spreadValues({}, stack), { [el]: true });
  }, {});
}
function mergeClassProp(baseProp, ...args) {
  const mergedClassProp = Object.assign({}, toClassProp(baseProp));
  for (const arg of args) {
    Object.assign(mergedClassProp, toClassProp(arg));
  }
  return mergedClassProp;
}
const dayKeys = [
  "@fs-base,TT:TIME.SUNDAY",
  "@fs-base,TT:TIME.MONDAY",
  "@fs-base,TT:TIME.TUESDAY",
  "@fs-base,TT:TIME.WEDNESDAY",
  "@fs-base,TT:TIME.THURSDAY",
  "@fs-base,TT:TIME.FRIDAY",
  "@fs-base,TT:TIME.SATURDAY"
];
const monthKeys = [
  "@fs-base,TT:TIME.JANUARY",
  "@fs-base,TT:TIME.FEBRUARY",
  "@fs-base,TT:TIME.MARCH",
  "@fs-base,TT:TIME.APRIL",
  "@fs-base,TT:TIME.MAY",
  "@fs-base,TT:TIME.JUNE",
  "@fs-base,TT:TIME.JULY",
  "@fs-base,TT:TIME.AUGUST",
  "@fs-base,TT:TIME.SEPTEMBER",
  "@fs-base,TT:TIME.OCTOBER",
  "@fs-base,TT:TIME.NOVEMBER",
  "@fs-base,TT:TIME.DECEMBER"
];
const monthShortKeys = [
  "@fs-base,TT:TIME.monthShort1",
  "@fs-base,TT:TIME.monthShort2",
  "@fs-base,TT:TIME.monthShort3",
  "@fs-base,TT:TIME.monthShort4",
  "@fs-base,TT:TIME.monthShort5",
  "@fs-base,TT:TIME.monthShort6",
  "@fs-base,TT:TIME.monthShort7",
  "@fs-base,TT:TIME.monthShort8",
  "@fs-base,TT:TIME.monthShort9",
  "@fs-base,TT:TIME.monthShort10",
  "@fs-base,TT:TIME.monthShort11",
  "@fs-base,TT:TIME.monthShort12"
];
function getWeeksInMonth(year, month) {
  const weeks = [];
  const firstDate = new Date(year, month, 1);
  const lastDate = new Date(year, month + 1, 0);
  const numDays = lastDate.getDate();
  let dayOfWeekCounter = firstDate.getDay();
  for (let date = 1; date <= numDays; date++) {
    if (dayOfWeekCounter === 0 || weeks.length === 0) {
      weeks.push([]);
    }
    weeks[weeks.length - 1].push(date);
    dayOfWeekCounter = (dayOfWeekCounter + 1) % 7;
  }
  return weeks.filter((w) => w.length > 0).map((w) => ({
    startDay: w[0],
    endDay: w[w.length - 1]
  }));
}
function getStartOfWeek(year, month, day) {
  const date = new Date(year, month, day);
  date.setDate(date.getDate() - date.getDay());
  return date;
}
function formatDay(day) {
  if (day === 31) return "st";
  switch (day % 20) {
    case 1:
      return "st";
    case 2:
      return "nd";
    case 3:
      return "rd";
    default:
      return "th";
  }
}
function isSelectedAirportFacility(facility) {
  return "currentRunway" in facility;
}
function isAirportFacility(facility) {
  return FacilityUtils.isFacilityType(facility, FacilityType.Airport) && "altitude" in facility;
}
var AirportSize = /* @__PURE__ */ ((AirportSize2) => {
  AirportSize2["Large"] = "Large";
  AirportSize2["Medium"] = "Medium";
  AirportSize2["Small"] = "Small";
  return AirportSize2;
})(AirportSize || {});
const LargeAirportThresholdFt = 8100;
const MediumAirportThresholdFt = 5e3;
function getAirportSize(airport) {
  const longestRunway = AirportUtils.getLongestRunway(airport);
  if (!longestRunway) {
    return "Small";
  }
  const longestRwyLengthFeet = UnitType.METER.convertTo(longestRunway.length, UnitType.FOOT);
  return longestRwyLengthFeet >= LargeAirportThresholdFt ? "Large" : longestRwyLengthFeet >= MediumAirportThresholdFt || airport.towered ? "Medium" : "Small";
}
function getFacilityStrField(facility, propertyName, getter) {
  return facility ? getter(facility[propertyName]) : "";
}
function getICAOIdent(facility) {
  return getFacilityStrField(facility, "icao", ICAO.getIdent);
}
function getFacilityName(facility) {
  return getFacilityStrField(facility, "name", Utils.Translate);
}
function getRunwayName(runway, shortened = false) {
  return `${shortened ? "rwy" : "runway"} ${runway.designation}`;
}
function getCurrentRunwayName(facility) {
  return facility && isSelectedAirportFacility(facility) ? getRunwayName(facility.currentRunway) : "";
}
function getFacilityIconPath(facilityType) {
  let svgName;
  switch (facilityType) {
    case FacilityType.Airport:
    case FacilityType.RWY:
      svgName = "Airport";
      break;
    case FacilityType.VOR:
      svgName = "VOR";
      break;
    case FacilityType.NDB:
      svgName = "NDB";
      break;
    case FacilityType.USR:
      svgName = "Intersection";
      break;
    case FacilityType.Intersection:
    default:
      svgName = "Waypoint";
  }
  return `coui://html_ui/efb_ui/efb_os/Assets/icons/facilities/${svgName}.svg`;
}
function createCustomFacility(repository, lat, lon) {
  const customIdent = getLatLonStr(lat, lon);
  const icao = `U      ${customIdent}`;
  const customFac = {
    icao,
    icaoStruct: ICAO.stringV1ToValue(icao),
    city: "none",
    lat,
    lon,
    name: "Custom point",
    region: "none"
  };
  repository.add(customFac);
  return customFac;
}
function getLatLonStr(lat, lon) {
  const dmsFormatter = new DmsFormatter();
  const partsLat = dmsFormatter.parseLat(lat);
  let customName = partsLat.degrees.toFixed(0) + partsLat.minutes.toFixed(0) + partsLat.direction;
  const partsLon = dmsFormatter.parseLon(lon);
  customName += partsLon.degrees.toFixed(0) + partsLon.minutes.toFixed(0) + partsLon.direction;
  return customName;
}
var SubscribableMapEventType = /* @__PURE__ */ ((SubscribableMapEventType2) => {
  SubscribableMapEventType2[SubscribableMapEventType2["Added"] = 0] = "Added";
  SubscribableMapEventType2[SubscribableMapEventType2["Updated"] = 1] = "Updated";
  SubscribableMapEventType2[SubscribableMapEventType2["Removed"] = 2] = "Removed";
  return SubscribableMapEventType2;
})(SubscribableMapEventType || {});
class MapSubject {
  constructor(entries) {
    this.isSubscribable = true;
    this.isMapSubscribable = true;
    this.obj = /* @__PURE__ */ new Map();
    this.subs = [];
    this.notifyDepth = 0;
    this.initialNotifyFunc = this.initialNotify.bind(this);
    this.onSubDestroyedFunc = this.onSubDestroyed.bind(this);
    if (entries) {
      for (const [k, v] of entries) {
        this.obj.set(k, v);
      }
    }
  }
  get length() {
    return this.obj.size;
  }
  static create(entries) {
    return new MapSubject(entries);
  }
  get(key) {
    return this.obj.get(key);
  }
  set(key, value2) {
    const oldValue = this.obj.get(key);
    const exists = this.has(key);
    this.obj.set(key, value2);
    this.notify(key, exists ? 1 : 0, value2, oldValue);
  }
  has(key) {
    return this.obj.has(key);
  }
  delete(key) {
    const deleted = this.obj.delete(key);
    if (deleted) {
      this.notify(
        key,
        2
        /* Removed */
      );
    }
    return deleted;
  }
  sub(handler, initialNotify = false, paused = false) {
    const sub = new HandlerSubscription(handler, this.initialNotifyFunc, this.onSubDestroyedFunc);
    this.subs.push(sub);
    if (paused) {
      sub.pause();
    } else if (initialNotify) {
      sub.initialNotify();
    }
    return sub;
  }
  unsub(handler) {
    const toDestroy = this.subs.find((sub) => sub.handler === handler);
    toDestroy == null ? void 0 : toDestroy.destroy();
  }
  map(_fn, _equalityFunc, _mutateFunc, _initialVal) {
    throw new Error("Method not implemented.");
  }
  pipe(_to, _map, _paused) {
    throw new Error("Method not implemented.");
  }
  notify(key, type, modifiedItem, previousValue) {
    let needCleanUpSubs = false;
    this.notifyDepth++;
    for (const sub of this.subs) {
      try {
        if (sub.isAlive && !sub.isPaused) {
          sub.handler(key, type, modifiedItem, previousValue);
        }
        needCleanUpSubs || (needCleanUpSubs = !sub.isAlive);
      } catch (error) {
        console.error(`MapSubject: error in handler: ${error}`);
      }
    }
    this.notifyDepth--;
    if (needCleanUpSubs && this.notifyDepth === 0) {
      this.subs = this.subs.filter((sub) => sub.isAlive);
    }
  }
  initialNotify(sub) {
    for (const key of this.obj.keys()) {
      const v = this.obj.get(key);
      try {
        sub.handler(key, 0, v, void 0);
      } catch (error) {
        console.error(`MapSubject: error in handker: ${error}`);
      }
    }
  }
  onSubDestroyed(sub) {
    if (this.notifyDepth === 0) {
      this.subs.splice(this.subs.indexOf(sub), 1);
    }
  }
}
function where(value2) {
  return (input) => value2 === input;
}
function toString() {
  return (input) => input.toString();
}
class ArrayFilterSubject extends AbstractSubscribableArray {
  constructor(arr, predicate) {
    super();
    this.array = [];
    this.sourceSub = arr.sub((_index, _type, _item, _array) => {
      const arrayBefore = [...this.array];
      this.array = _array.filter(predicate);
      if (arrayBefore !== this.array) {
        const index2 = arrayBefore.length;
        this.notify(0, SubscribableArrayEventType.Cleared);
        this.notify(
          index2,
          this.array.length >= index2 ? SubscribableArrayEventType.Added : SubscribableArrayEventType.Removed,
          this.array
        );
      }
    }, true);
  }
  get length() {
    return this.array.length;
  }
  static create(arr, predicate) {
    return new ArrayFilterSubject(arr, predicate);
  }
  getArray() {
    return this.array;
  }
  destroy() {
    this.sourceSub.destroy();
  }
}
var FlightPhaseState = /* @__PURE__ */ ((FlightPhaseState2) => {
  FlightPhaseState2[FlightPhaseState2["PREFLIGHT"] = 0] = "PREFLIGHT";
  FlightPhaseState2[FlightPhaseState2["STARTUP"] = 1] = "STARTUP";
  FlightPhaseState2[FlightPhaseState2["BEFORE_TAXI"] = 2] = "BEFORE_TAXI";
  FlightPhaseState2[FlightPhaseState2["TAXI"] = 3] = "TAXI";
  FlightPhaseState2[FlightPhaseState2["TAKEOFF"] = 4] = "TAKEOFF";
  FlightPhaseState2[FlightPhaseState2["CLIMB"] = 5] = "CLIMB";
  FlightPhaseState2[FlightPhaseState2["CRUISE"] = 6] = "CRUISE";
  FlightPhaseState2[FlightPhaseState2["DESCENT"] = 7] = "DESCENT";
  FlightPhaseState2[FlightPhaseState2["LANDING"] = 8] = "LANDING";
  FlightPhaseState2[FlightPhaseState2["TAXITOGATE"] = 9] = "TAXITOGATE";
  FlightPhaseState2[FlightPhaseState2["SHUTDOWN"] = 10] = "SHUTDOWN";
  FlightPhaseState2[FlightPhaseState2["FLIGHT_OVER"] = 11] = "FLIGHT_OVER";
  FlightPhaseState2[FlightPhaseState2["UNKNOWN"] = 100] = "UNKNOWN";
  return FlightPhaseState2;
})(FlightPhaseState || {});
class _FlightPhaseManager {
  // eslint-disable-next-line @typescript-eslint/no-empty-function
  constructor() {
    this._flightPhase = Subject.create(
      100
      /* UNKNOWN */
    );
    this.flightPhase = this._flightPhase;
    this.isFlightOver = this._flightPhase.map(where(
      11
      /* FLIGHT_OVER */
    ));
    this.onFlightPhaseStateChangedSubscription = null;
  }
  /**
   * Static singleton instance of the Flight Phase Manager
   * @internal
   */
  static get instance() {
    return window.FLIGHT_PHASE_MANAGER = _FlightPhaseManager._instance = window.FLIGHT_PHASE_MANAGER || _FlightPhaseManager._instance || new _FlightPhaseManager();
  }
  /**
   * The bus is set once at EFB initialization from efb_ui.tsx
   * @internal
   */
  setBus(bus) {
    var _a;
    (_a = this.onFlightPhaseStateChangedSubscription) == null ? void 0 : _a.destroy();
    this.onFlightPhaseStateChangedSubscription = bus.on("FlightPhaseChanged", (flightPhase) => {
      switch (flightPhase) {
        case "PREFLIGHT_RTC":
        case "PREFLIGHT":
        case "SKIP_TRANSITION_PREFLIGHT":
          this._flightPhase.set(
            0
            /* PREFLIGHT */
          );
          break;
        case "STARTUP":
          this._flightPhase.set(
            1
            /* STARTUP */
          );
          break;
        case "BEFORE_TAXI":
        case "TAXI":
        case "SKIP_TRANSITION_TAXI":
          this._flightPhase.set(
            3
            /* TAXI */
          );
          break;
        case "TAKEOFF":
          this._flightPhase.set(
            4
            /* TAKEOFF */
          );
          break;
        case "CLIMB":
          this._flightPhase.set(
            5
            /* CLIMB */
          );
          break;
        case "CRUISE":
        case "SKIP_TRANSITION_CRUISE":
          this._flightPhase.set(
            6
            /* CRUISE */
          );
          break;
        case "DESCENT":
          this._flightPhase.set(
            7
            /* DESCENT */
          );
          break;
        case "LANDING":
          this._flightPhase.set(
            8
            /* LANDING */
          );
          break;
        case "TAXITOGATE":
        case "SKIP_TRANSITION_TAXITOGATE":
          this._flightPhase.set(
            9
            /* TAXITOGATE */
          );
          break;
        case "SHUTDOWN":
          this._flightPhase.set(
            10
            /* SHUTDOWN */
          );
          break;
        case "RTC":
          break;
        case "MISSIONSUCCESS":
        case "MISSIONABORTED":
          this._flightPhase.set(
            11
            /* FLIGHT_OVER */
          );
          break;
        case "REACH BANNER APPROACH":
        case "HOOKBANNER":
        case "REACH PASS":
        case "PASS":
        case "CRUISEBACK":
        case "REACH DROP":
        case "DROP BANNER":
          break;
        case "WATERDROP":
          break;
        default:
          console.warn(`Received unknown flight phase '${flightPhase}'`);
          this._flightPhase.set(
            100
            /* UNKNOWN */
          );
      }
    });
  }
  /**
   * @description This function is used in order to verify if a given flight phase has been reached.
   * Always return true when the flight phase is unknown.
   * @param flightPhase The flight phase that has been reached or not
   * @returns A subscribable that returns whether the given flight phase has been reached
   */
  hasReachedFlightPhaseState(flightPhase) {
    return this.flightPhase.map(
      (currentFlightPhaseState) => currentFlightPhaseState >= flightPhase
    );
  }
}
const FlightPhaseManager = _FlightPhaseManager.instance;
var GameMode = /* @__PURE__ */ ((GameMode2) => {
  GameMode2[GameMode2["UNKNOWN"] = 0] = "UNKNOWN";
  GameMode2[GameMode2["CAREER"] = 1] = "CAREER";
  GameMode2[GameMode2["CHALLENGE"] = 2] = "CHALLENGE";
  GameMode2[GameMode2["DISCOVERY"] = 3] = "DISCOVERY";
  GameMode2[GameMode2["FREEFLIGHT"] = 4] = "FREEFLIGHT";
  return GameMode2;
})(GameMode || {});
class _GameModeManager {
  // eslint-disable-next-line @typescript-eslint/no-empty-function
  constructor() {
    this._gameMode = Subject.create(
      0
      /* UNKNOWN */
    );
    this.gameMode = this._gameMode;
    this.isCareer = this._gameMode.map(where(
      1
      /* CAREER */
    ));
    this.isChallenge = this._gameMode.map(where(
      2
      /* CHALLENGE */
    ));
    this.isDiscovery = this._gameMode.map(where(
      3
      /* DISCOVERY */
    ));
    this.isFreeflight = this._gameMode.map(where(
      4
      /* FREEFLIGHT */
    ));
    this.isInMenu = Subject.create(true);
    this.onGameModeChangedSubscription = null;
    this.onIsInMenuSubscription = null;
  }
  /**
   * Static singleton instance of the Game mode manager
   * @internal
   */
  static get instance() {
    return window.GAME_MODE_MANAGER = _GameModeManager._instance = window.GAME_MODE_MANAGER || _GameModeManager._instance || new _GameModeManager();
  }
  /**
   * The bus is set once at EFB initialization from efb_ui.tsx
   * @internal
   */
  setBus(bus) {
    var _a, _b;
    (_a = this.onGameModeChangedSubscription) == null ? void 0 : _a.destroy();
    (_b = this.onIsInMenuSubscription) == null ? void 0 : _b.destroy();
    this.onGameModeChangedSubscription = bus.on("GameModeChanged", (gameMode) => {
      switch (gameMode) {
        case "":
          this._gameMode.set(
            0
            /* UNKNOWN */
          );
          break;
        case "CAREER GAMEMODE":
          this._gameMode.set(
            1
            /* CAREER */
          );
          break;
        case "CHALLENGE GAMEMODE":
          this._gameMode.set(
            2
            /* CHALLENGE */
          );
          break;
        case "DISCOVERY GAMEMODE":
          this._gameMode.set(
            3
            /* DISCOVERY */
          );
          break;
        case "FREEFLIGHT GAMEMODE":
          this._gameMode.set(
            4
            /* FREEFLIGHT */
          );
          break;
        default:
          console.error(`Unknown game mode '${gameMode}'`);
          this._gameMode.set(
            0
            /* UNKNOWN */
          );
      }
    });
    this.onIsInMenuSubscription = bus.on("IsInMenuUpdate", (isInMenu) => this.isInMenu.set(isInMenu));
  }
}
const GameModeManager = _GameModeManager.instance;
function loadFileAsBlob(url) {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    xhr.open("GET", url, true);
    xhr.responseType = "blob";
    xhr.onload = function(e) {
      if (this.status === 200) {
        resolve(this.response);
      } else {
        reject(this.response);
      }
    };
    xhr.send();
  });
}
Blob.prototype.arrayBuffer = function() {
  return new Promise((resolve, reject) => {
    const fileReader = new FileReader();
    fileReader.onload = function(event) {
      var _a;
      const arrayBuffer = (_a = event.target) == null ? void 0 : _a.result;
      if (typeof arrayBuffer === "string" || arrayBuffer === null || arrayBuffer === void 0) {
        reject("ArrayBuffer is null");
        return;
      }
      resolve(arrayBuffer);
    };
    fileReader.readAsArrayBuffer(this);
    fileReader.result;
  });
};
const uint8 = new Uint8Array(4);
const uint32 = new Uint32Array(uint8.buffer);
function extractChunks(data) {
  if (data[0] !== 137) throw new Error("Invalid .png file header");
  if (data[1] !== 80) throw new Error("Invalid .png file header");
  if (data[2] !== 78) throw new Error("Invalid .png file header");
  if (data[3] !== 71) throw new Error("Invalid .png file header");
  if (data[4] !== 13)
    throw new Error("Invalid .png file header: possibly caused by DOS-Unix line ending conversion?");
  if (data[5] !== 10)
    throw new Error("Invalid .png file header: possibly caused by DOS-Unix line ending conversion?");
  if (data[6] !== 26) throw new Error("Invalid .png file header");
  if (data[7] !== 10)
    throw new Error("Invalid .png file header: possibly caused by DOS-Unix line ending conversion?");
  let ended = false;
  const chunks = [];
  let idx = 8;
  while (idx < data.length) {
    uint8[3] = data[idx++];
    uint8[2] = data[idx++];
    uint8[1] = data[idx++];
    uint8[0] = data[idx++];
    const length = uint32[0] + 4;
    const chunk2 = new Uint8Array(length);
    chunk2[0] = data[idx++];
    chunk2[1] = data[idx++];
    chunk2[2] = data[idx++];
    chunk2[3] = data[idx++];
    const name = String.fromCharCode(chunk2[0]) + String.fromCharCode(chunk2[1]) + String.fromCharCode(chunk2[2]) + String.fromCharCode(chunk2[3]);
    if (!chunks.length && name !== "IHDR") {
      throw new Error("IHDR header missing");
    }
    if (name === "IEND") {
      ended = true;
      chunks.push({
        name,
        data: new Uint8Array(0)
      });
      break;
    }
    for (let i = 4; i < length; i++) {
      chunk2[i] = data[idx++];
    }
    uint8[3] = data[idx++];
    uint8[2] = data[idx++];
    uint8[1] = data[idx++];
    uint8[0] = data[idx++];
    const chunkData = new Uint8Array(chunk2.buffer.slice(4));
    chunks.push({
      name,
      data: chunkData
    });
  }
  if (!ended) {
    throw new Error(".png file ended prematurely: no IEND header was found");
  }
  return chunks;
}
function textDecode(data) {
  let naming = true;
  let text = "";
  let name = "";
  for (const code of data) {
    if (naming) {
      if (code) {
        name += String.fromCharCode(code);
      } else {
        naming = false;
      }
    } else {
      if (code) {
        text += String.fromCharCode(code);
      } else {
        throw new Error("Invalid NULL character found. 0x00 character is not permitted in tEXt content");
      }
    }
  }
  return {
    keyword: name,
    text
  };
}
function readUint32(uint8array, offset) {
  const byte1 = uint8array[offset++];
  const byte2 = uint8array[offset++];
  const byte3 = uint8array[offset++];
  const byte4 = uint8array[offset];
  return 0 | byte1 << 24 | byte2 << 16 | byte3 << 8 | byte4;
}
function readMetadata(buffer) {
  const result = {
    tEXt: {
      keyword: ""
    },
    pHYs: { x: 0, y: 0 }
  };
  const chunks = extractChunks(new Uint8Array(buffer));
  chunks.forEach((chunk2) => {
    if (chunk2.name === "tExt") {
      const textChunk = textDecode(chunk2.data);
      result.tEXt[textChunk.keyword] = textChunk.text;
    } else if (chunk2.name === "pHYs") {
      result.pHYs = {
        x: readUint32(chunk2.data, 0),
        y: readUint32(chunk2.data, 4)
      };
    } else {
      result[chunk2.name] = true;
    }
  });
  return result;
}
function isNotificationPermanent(notif) {
  return notif.type === "permanent";
}
function isNotifPermanent(notif) {
  return isNotificationPermanent(notif);
}
const DEFAULT_DISPLAY_TIME = 3e3;
function createTemporaryNotification(description, displayTimeMs, style, descriptionArguments, icon) {
  const notifStyle = style != null ? style : "info";
  return {
    uuid: UUID.GenerateUuid(),
    type: "temporary",
    createdAt: /* @__PURE__ */ new Date(),
    hide: Subject.create(false),
    delayMs: Utils.Clamp(displayTimeMs != null ? displayTimeMs : DEFAULT_DISPLAY_TIME, 0, 6e4),
    description,
    style: notifStyle,
    descriptionArguments,
    icon: icon != null ? icon : getNotifIconFromStyle(notifStyle)
  };
}
function createTemporaryNotif(displayTimeMs, description, style, descriptionArguments, icon) {
  return createTemporaryNotification(description, displayTimeMs, style, descriptionArguments, icon);
}
function createPermanentNotification(title, description, displayTimeMs, style, descriptionArguments, icon, color, action) {
  const notifStyle = style != null ? style : "info";
  return {
    uuid: UUID.GenerateUuid(),
    type: "permanent",
    createdAt: /* @__PURE__ */ new Date(),
    hide: Subject.create(false),
    delayMs: Utils.Clamp(displayTimeMs != null ? displayTimeMs : DEFAULT_DISPLAY_TIME, 0, 6e4),
    description,
    style: notifStyle,
    descriptionArguments,
    icon: icon != null ? icon : getNotifIconFromStyle(notifStyle),
    title,
    color,
    action,
    viewed: Subject.create(false)
  };
}
function createPermanentNotif(displayTimeMs, title, description, style, descriptionArguments, icon, color, action) {
  return createPermanentNotification(
    title,
    description,
    displayTimeMs,
    style,
    descriptionArguments,
    icon,
    color,
    action
  );
}
function getNotifIconFromStyle(style) {
  switch (style) {
    case "warning":
      return "coui://html_ui/efb_ui/efb_os/Assets/icons/NoMargin/Warning.svg";
    case "success":
      return "coui://html_ui/efb_ui/efb_os/Assets/icons/NoMargin/Check_Full.svg";
    case "error":
      return "coui://html_ui/efb_ui/efb_os/Assets/icons/NoMargin/Failure_Full.svg";
    default:
      return "coui://html_ui/efb_ui/efb_os/Assets/icons/NoMargin/Info_Full.svg";
  }
}
function isPromise(value2) {
  return value2 instanceof Promise;
}
function toPromise(value2) {
  return isPromise(value2) ? value2 : Promise.resolve(value2);
}
function checkUserSetting(setting, type) {
  if (!Object.values(type).includes(setting.get())) {
    setting.resetToDefault();
  }
}
const basicFormatter = NumberFormatter.create({
  maxDigits: 0,
  forceDecimalZeroes: false,
  nanString: "-"
});
var StopwatchState = /* @__PURE__ */ ((StopwatchState2) => {
  StopwatchState2[StopwatchState2["READY"] = 0] = "READY";
  StopwatchState2[StopwatchState2["RUNNING"] = 1] = "RUNNING";
  StopwatchState2[StopwatchState2["PAUSED"] = 2] = "PAUSED";
  return StopwatchState2;
})(StopwatchState || {});
class Stopwatch {
  constructor() {
    this._timerSeconds = Subject.create(0);
    this.timerSeconds = this._timerSeconds;
    this._state = Subject.create(
      0
      /* READY */
    );
    this.state = this._state;
    this.initialTime = 0;
  }
  start() {
    const offset = this._state.get() === 2 ? this._timerSeconds.get() : 0;
    this.initialTime = SimVar.GetSimVarValue("E:SIMULATION TIME", "seconds") - offset;
    this.intervalObj = setInterval(this.callback.bind(this), 500);
    this._state.set(
      1
      /* RUNNING */
    );
  }
  pause() {
    clearInterval(this.intervalObj);
    this._state.set(
      2
      /* PAUSED */
    );
  }
  reset() {
    clearInterval(this.intervalObj);
    this.initialTime = 0;
    this._timerSeconds.set(0);
    this._state.set(
      0
      /* READY */
    );
  }
  callback() {
    this._timerSeconds.set(SimVar.GetSimVarValue("E:SIMULATION TIME", "seconds") - this.initialTime);
  }
}
const _UnitFormatter = class _UnitFormatter {
  /**
   * Creates a function which formats measurement units to strings representing their abbreviated names.
   * @param defaultString The string to output when the input unit cannot be formatted. Defaults to the empty string.
   * @param charCase The case to enforce on the output string. Defaults to `'normal'`.
   * @returns A function which formats measurement units to strings representing their abbreviated names.
   */
  static create(defaultString = "", charCase = "normal") {
    var _a, _b;
    switch (charCase) {
      case "upper":
        (_a = _UnitFormatter.UNIT_TEXT_UPPER) != null ? _a : _UnitFormatter.UNIT_TEXT_UPPER = _UnitFormatter.createUpperCase();
        return (unit) => {
          var _a2, _b2;
          return (
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            (_b2 = (_a2 = _UnitFormatter.UNIT_TEXT_UPPER[unit.family]) == null ? void 0 : _a2[unit.name]) != null ? _b2 : defaultString
          );
        };
      case "lower":
        (_b = _UnitFormatter.UNIT_TEXT_LOWER) != null ? _b : _UnitFormatter.UNIT_TEXT_LOWER = _UnitFormatter.createLowerCase();
        return (unit) => {
          var _a2, _b2;
          return (
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            (_b2 = (_a2 = _UnitFormatter.UNIT_TEXT_LOWER[unit.family]) == null ? void 0 : _a2[unit.name]) != null ? _b2 : defaultString
          );
        };
      default:
        return (unit) => {
          var _a2, _b2;
          return (_b2 = (_a2 = _UnitFormatter.UNIT_TEXT[unit.family]) == null ? void 0 : _a2[unit.name]) != null ? _b2 : defaultString;
        };
    }
  }
  /**
   * Creates a record of lowercase unit abbreviated names.
   * @returns A record of lowercase unit abbreviated names.
   */
  static createLowerCase() {
    const lower = {};
    for (const family in _UnitFormatter.UNIT_TEXT) {
      const familyText = _UnitFormatter.UNIT_TEXT[family];
      lower[family] = {};
      for (const unit in familyText) {
        lower[family][unit] = familyText[unit].toLowerCase();
      }
    }
    return lower;
  }
  /**
   * Creates a record of uppercase unit abbreviated names.
   * @returns A record of uppercase unit abbreviated names.
   */
  static createUpperCase() {
    const upper = {};
    for (const family in _UnitFormatter.UNIT_TEXT) {
      const familyText = _UnitFormatter.UNIT_TEXT[family];
      upper[family] = {};
      for (const unit in familyText) {
        upper[family][unit] = familyText[unit].toUpperCase();
      }
    }
    return upper;
  }
  /**
   * Gets a mapping of unit family and name to text used by UnitFormatter to format units. The returned object maps
   * unit families to objects that map unit names within each family to formatted text.
   * @returns A mapping of unit family and name to text used by UnitFormatter to format units.
   */
  static getUnitTextMap() {
    return _UnitFormatter.UNIT_TEXT;
  }
};
_UnitFormatter.UNIT_TEXT = {
  [UnitFamily.Distance]: {
    [UnitType.METER.name]: "M",
    [UnitType.FOOT.name]: "FT",
    [UnitType.KILOMETER.name]: "KM",
    [UnitType.NMILE.name]: "NM",
    [UnitType.MILE.name]: "SM"
  },
  [UnitFamily.Angle]: {
    [UnitType.DEGREE.name]: "°",
    [UnitType.RADIAN.name]: "rad"
  },
  [UnitFamily.Duration]: {
    [UnitType.SECOND.name]: "SEC",
    [UnitType.MINUTE.name]: "MIN",
    [UnitType.HOUR.name]: "HR"
  },
  [UnitFamily.Weight]: {
    [UnitType.KILOGRAM.name]: "KG",
    [UnitType.POUND.name]: "LBS",
    [UnitType.LITER_FUEL.name]: "LT",
    [UnitType.GALLON_FUEL.name]: "GAL",
    [UnitType.IMP_GALLON_FUEL.name]: "IG"
  },
  [UnitFamily.Volume]: {
    [UnitType.LITER.name]: "L",
    [UnitType.GALLON.name]: "GAL"
  },
  [UnitFamily.Pressure]: {
    [UnitType.HPA.name]: "HPA",
    [UnitType.IN_HG.name]: "IN"
  },
  [UnitFamily.Temperature]: {
    [UnitType.CELSIUS.name]: "°C",
    [UnitType.FAHRENHEIT.name]: "°F"
  },
  [UnitFamily.TemperatureDelta]: {
    [UnitType.DELTA_CELSIUS.name]: "°C",
    [UnitType.DELTA_FAHRENHEIT.name]: "°F"
  },
  [UnitFamily.Speed]: {
    [UnitType.KNOT.name]: "KT",
    [UnitType.KPH.name]: "KM/H",
    [UnitType.MPM.name]: "MPM",
    [UnitType.FPM.name]: "FPM"
  },
  [UnitFamily.WeightFlux]: {
    [UnitType.KGH.name]: "KG/HR",
    [UnitType.PPH.name]: "LB/HR",
    [UnitType.LPH_FUEL.name]: "LT/HR",
    [UnitType.GPH_FUEL.name]: "GAL/HR",
    [UnitType.IGPH_FUEL.name]: "IG/HR"
  }
};
let UnitFormatter = _UnitFormatter;
function value(arg) {
  return typeof arg === "function" ? arg() : arg;
}
function offsetMousePosition(e, vec, element = null) {
  const mousePosition = elementOffset(e.target, element);
  vec[0] = mousePosition[0] + e.offsetX;
  vec[1] = mousePosition[1] + e.offsetY;
}
function elementOffset(from, limit = null) {
  let leftOffset = 0;
  let topOffset = 0;
  let currentElement = from;
  do {
    leftOffset += currentElement.offsetLeft;
    topOffset += currentElement.offsetTop;
    currentElement = currentElement.parentNode;
  } while (currentElement !== null && currentElement !== limit);
  return new Float64Array([leftOffset, topOffset]);
}
const measure = (_target, propertyKey, descriptor) => {
  const originalMethod = descriptor.value;
  descriptor.value = function(...args) {
    const start = performance.now();
    const result = originalMethod.apply(this, args);
    const finish = performance.now();
    console.log(`Execution time of ${_target.constructor.name}.${propertyKey} took ${finish - start} ms`);
    return result;
  };
  return descriptor;
};
class AppContainer extends DisplayComponent {
  constructor() {
    super(...arguments);
    this.appMainRef = FSComponent.createRef();
    this.appStackRef = FSComponent.createRef();
  }
  onAfterRender(node) {
    super.onAfterRender(node);
    this.props.appViewService.onAppContainerRendered(this.appStackRef.instance);
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent("div", { ref: this.appMainRef, class: `app-container` }, /* @__PURE__ */ FSComponent.buildComponent(DefaultAppViewStackContainer, { ref: this.appStackRef }));
  }
}
class DefaultAppViewStackContainer extends DisplayComponent {
  constructor() {
    super(...arguments);
    this.rootRef = FSComponent.createRef();
  }
  renderView(view) {
    FSComponent.render(view, this.rootRef.instance);
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent("div", { ref: this.rootRef, class: mergeClassProp("app-view-stack", this.props.class) });
  }
}
function Memoize(args) {
  let hashFunction;
  let duration;
  let tags;
  {
    hashFunction = args;
  }
  return (target, propertyKey, descriptor) => {
    if (descriptor.value != null) {
      descriptor.value = getNewFunction(descriptor.value, hashFunction, duration, tags);
    } else if (descriptor.get != null) {
      descriptor.get = getNewFunction(descriptor.get, hashFunction, duration, tags);
    } else {
      throw "Only put a Memoize() decorator on a method or get accessor.";
    }
  };
}
const clearCacheTagsMap = /* @__PURE__ */ new Map();
function getNewFunction(originalMethod, hashFunction, duration = 0, tags) {
  const propMapName = Symbol(`__memoized_map__`);
  return function(...args) {
    let returnedValue;
    if (!this.hasOwnProperty(propMapName)) {
      Object.defineProperty(this, propMapName, {
        configurable: false,
        enumerable: false,
        writable: false,
        value: /* @__PURE__ */ new Map()
      });
    }
    let myMap = this[propMapName];
    if (Array.isArray(tags)) {
      for (const tag of tags) {
        if (clearCacheTagsMap.has(tag)) {
          clearCacheTagsMap.get(tag).push(myMap);
        } else {
          clearCacheTagsMap.set(tag, [myMap]);
        }
      }
    }
    if (hashFunction || args.length > 0 || duration > 0) {
      let hashKey;
      if (hashFunction === true) {
        hashKey = args.map((a) => a.toString()).join("!");
      } else if (hashFunction) {
        hashKey = hashFunction.apply(this, args);
      } else {
        hashKey = args[0];
      }
      const timestampKey = `${hashKey}__timestamp`;
      let isExpired = false;
      if (duration > 0) {
        if (!myMap.has(timestampKey)) {
          isExpired = true;
        } else {
          let timestamp = myMap.get(timestampKey);
          isExpired = Date.now() - timestamp > duration;
        }
      }
      if (myMap.has(hashKey) && !isExpired) {
        returnedValue = myMap.get(hashKey);
      } else {
        returnedValue = originalMethod.apply(this, args);
        myMap.set(hashKey, returnedValue);
        if (duration > 0) {
          myMap.set(timestampKey, Date.now());
        }
      }
    } else {
      const hashKey = this;
      if (myMap.has(hashKey)) {
        returnedValue = myMap.get(hashKey);
      } else {
        returnedValue = originalMethod.apply(this, args);
        myMap.set(hashKey, returnedValue);
      }
    }
    return returnedValue;
  };
}
var GamepadEvents = /* @__PURE__ */ ((GamepadEvents2) => {
  GamepadEvents2["JOYSTICK_LEFT_X_AXIS"] = "JOYSTICK_LEFT_X_AXIS";
  GamepadEvents2["JOYSTICK_LEFT_Y_AXIS"] = "JOYSTICK_LEFT_Y_AXIS";
  GamepadEvents2["JOYSTICK_RIGHT_X_AXIS"] = "JOYSTICK_RIGHT_X_AXIS";
  GamepadEvents2["JOYSTICK_RIGHT_Y_AXIS"] = "JOYSTICK_RIGH_Y_AXIS";
  GamepadEvents2["BUTTON_A"] = "BUTTON_A";
  GamepadEvents2["BUTTON_B"] = "BUTTON_B";
  GamepadEvents2["BUTTON_Y"] = "BUTTON_Y";
  GamepadEvents2["BUTTON_X"] = "BUTTON_X";
  GamepadEvents2["JOYDIR_LEFT"] = "JOYDIR_LEFT";
  GamepadEvents2["JOYDIR_RIGHT"] = "JOYDIR_RIGHT";
  GamepadEvents2["JOYDIR_UP"] = "JOYDIR_UP";
  GamepadEvents2["JOYDIR_DOWN"] = "JOYDIR_DOWN";
  return GamepadEvents2;
})(GamepadEvents || {});
class GamepadInputManager {
  /**
   * @deprecated
   * @param element The HTML element used for scroll inputs.
   */
  constructor(element) {
    this.element = element;
    this.defaultScrollDirection = "all";
    this.defaultScrollSpeed = 1e3;
  }
  /**
   * @deprecated
   * @param inputContext The input context.
   * @param inputAction The input action.
   * @param callback The callback that will be called when the input is triggered.
   * @returns The input watcher ID.
   */
  addCustomEvent(inputContext, inputAction, callback) {
    return "";
  }
  /**
   * @deprecated
   * @param eventId The event ID.
   */
  removeCustonEvent(eventId) {
    return;
  }
  /**
   * @deprecated
   * Allows an HTML element to be scrolled through using a gamepad.
   * @param direction The direction where it is allowed to scroll. All directions are allowed by default.
   * @param scrollSpeed The scroll speed in px/s. 1000 by default.
   */
  enableScroll(direction = this.defaultScrollDirection, scrollSpeed = this.defaultScrollSpeed) {
    return;
  }
  /**
   * @deprecated
   * Prevents an HTML element to be scrolled through using a gamepad.
   */
  disableScroll() {
    return;
  }
}
var InputAction = /* @__PURE__ */ ((InputAction2) => {
  InputAction2[InputAction2["SECONDARY"] = 0] = "SECONDARY";
  InputAction2[InputAction2["TERTIARY"] = 1] = "TERTIARY";
  InputAction2[InputAction2["PREV"] = 2] = "PREV";
  InputAction2[InputAction2["NEXT"] = 3] = "NEXT";
  InputAction2[InputAction2["PREV_SUB"] = 4] = "PREV_SUB";
  InputAction2[InputAction2["NEXT_SUB"] = 5] = "NEXT_SUB";
  InputAction2[InputAction2["ZOOM_OUT"] = 6] = "ZOOM_OUT";
  InputAction2[InputAction2["ZOOM_IN"] = 7] = "ZOOM_IN";
  InputAction2[InputAction2["SCROLL_UP"] = 8] = "SCROLL_UP";
  InputAction2[InputAction2["SCROLL_RIGHT"] = 9] = "SCROLL_RIGHT";
  InputAction2[InputAction2["SCROLL_DOWN"] = 10] = "SCROLL_DOWN";
  InputAction2[InputAction2["SCROLL_LEFT"] = 11] = "SCROLL_LEFT";
  return InputAction2;
})(InputAction || {});
const InputActions = {
  [
    0
    /* SECONDARY */
  ]: "KEY_EFB_SECONDARY",
  [
    1
    /* TERTIARY */
  ]: "KEY_EFB_TERTIARY",
  [
    2
    /* PREV */
  ]: "KEY_EFB_NAV_TABS_PREV",
  [
    3
    /* NEXT */
  ]: "KEY_EFB_NAV_TABS_NEXT",
  [
    4
    /* PREV_SUB */
  ]: "KEY_EFB_ZOOM_OUT",
  [
    5
    /* NEXT_SUB */
  ]: "KEY_EFB_ZOOM_IN",
  [
    6
    /* ZOOM_OUT */
  ]: "KEY_EFB_ZOOM_OUT",
  [
    7
    /* ZOOM_IN */
  ]: "KEY_EFB_ZOOM_IN",
  [
    8
    /* SCROLL_UP */
  ]: "KEY_EFB_SCROLL_UP",
  [
    9
    /* SCROLL_RIGHT */
  ]: "KEY_EFB_SCROLL_RIGHT",
  [
    10
    /* SCROLL_DOWN */
  ]: "KEY_EFB_SCROLL_DOWN",
  [
    11
    /* SCROLL_LEFT */
  ]: "KEY_EFB_SCROLL_LEFT"
};
var InternalInputAction = /* @__PURE__ */ ((InternalInputAction2) => {
  InternalInputAction2[InternalInputAction2["VALID"] = 0] = "VALID";
  InternalInputAction2[InternalInputAction2["BACK"] = 1] = "BACK";
  InternalInputAction2[InternalInputAction2["SECONDARY"] = 2] = "SECONDARY";
  InternalInputAction2[InternalInputAction2["TERTIARY"] = 3] = "TERTIARY";
  InternalInputAction2[InternalInputAction2["PREV_TAB"] = 4] = "PREV_TAB";
  InternalInputAction2[InternalInputAction2["NEXT_TAB"] = 5] = "NEXT_TAB";
  InternalInputAction2[InternalInputAction2["ZOOM_OUT"] = 6] = "ZOOM_OUT";
  InternalInputAction2[InternalInputAction2["ZOOM_IN"] = 7] = "ZOOM_IN";
  InternalInputAction2[InternalInputAction2["UP"] = 8] = "UP";
  InternalInputAction2[InternalInputAction2["RIGHT"] = 9] = "RIGHT";
  InternalInputAction2[InternalInputAction2["DOWN"] = 10] = "DOWN";
  InternalInputAction2[InternalInputAction2["LEFT"] = 11] = "LEFT";
  InternalInputAction2[InternalInputAction2["PREV_APP"] = 12] = "PREV_APP";
  InternalInputAction2[InternalInputAction2["NEXT_APP"] = 13] = "NEXT_APP";
  InternalInputAction2[InternalInputAction2["OPEN_NOTIFICATION_MENU"] = 14] = "OPEN_NOTIFICATION_MENU";
  InternalInputAction2[InternalInputAction2["SCROLL_UP"] = 15] = "SCROLL_UP";
  InternalInputAction2[InternalInputAction2["SCROLL_RIGHT"] = 16] = "SCROLL_RIGHT";
  InternalInputAction2[InternalInputAction2["SCROLL_DOWN"] = 17] = "SCROLL_DOWN";
  InternalInputAction2[InternalInputAction2["SCROLL_LEFT"] = 18] = "SCROLL_LEFT";
  InternalInputAction2[InternalInputAction2["TEXTBOX_VALID"] = 19] = "TEXTBOX_VALID";
  InternalInputAction2[InternalInputAction2["TEXTBOX_UP"] = 20] = "TEXTBOX_UP";
  InternalInputAction2[InternalInputAction2["TEXTBOX_RIGHT"] = 21] = "TEXTBOX_RIGHT";
  InternalInputAction2[InternalInputAction2["TEXTBOX_DOWN"] = 22] = "TEXTBOX_DOWN";
  InternalInputAction2[InternalInputAction2["TEXTBOX_LEFT"] = 23] = "TEXTBOX_LEFT";
  return InternalInputAction2;
})(InternalInputAction || {});
const InternalInputActions = {
  [
    0
    /* VALID */
  ]: "KEY_EFB_VALID",
  [
    1
    /* BACK */
  ]: "KEY_EFB_BACK",
  [
    2
    /* SECONDARY */
  ]: "KEY_EFB_SECONDARY",
  [
    3
    /* TERTIARY */
  ]: "KEY_EFB_TERTIARY",
  [
    4
    /* PREV_TAB */
  ]: "KEY_EFB_NAV_TABS_PREV",
  [
    5
    /* NEXT_TAB */
  ]: "KEY_EFB_NAV_TABS_NEXT",
  [
    6
    /* ZOOM_OUT */
  ]: "KEY_EFB_ZOOM_OUT",
  [
    7
    /* ZOOM_IN */
  ]: "KEY_EFB_ZOOM_IN",
  [
    8
    /* UP */
  ]: "KEY_EFB_UP",
  [
    9
    /* RIGHT */
  ]: "KEY_EFB_NAV_APP_NEXT",
  [
    10
    /* DOWN */
  ]: "KEY_EFB_DOWN",
  [
    11
    /* LEFT */
  ]: "KEY_EFB_NAV_APP_PREV",
  [
    12
    /* PREV_APP */
  ]: "KEY_EFB_NAV_APP_PREV",
  [
    13
    /* NEXT_APP */
  ]: "KEY_EFB_NAV_APP_NEXT",
  [
    14
    /* OPEN_NOTIFICATION_MENU */
  ]: "KEY_EFB_NOTIFICATIONS_MENU",
  [
    15
    /* SCROLL_UP */
  ]: "KEY_EFB_SCROLL_UP",
  [
    16
    /* SCROLL_RIGHT */
  ]: "KEY_EFB_SCROLL_RIGHT",
  [
    17
    /* SCROLL_DOWN */
  ]: "KEY_EFB_SCROLL_DOWN",
  [
    18
    /* SCROLL_LEFT */
  ]: "KEY_EFB_SCROLL_LEFT",
  [
    19
    /* TEXTBOX_VALID */
  ]: "KEY_EFB_VALID_TEXT",
  [
    20
    /* TEXTBOX_UP */
  ]: "KEY_EFB_TEXTBOX_UP",
  [
    21
    /* TEXTBOX_RIGHT */
  ]: "KEY_EFB_TEXTBOX_RIGHT",
  [
    22
    /* TEXTBOX_DOWN */
  ]: "KEY_EFB_TEXTBOX_DOWN",
  [
    23
    /* TEXTBOX_LEFT */
  ]: "KEY_EFB_TEXTBOX_LEFT"
};
const _InputsListener = class _InputsListener extends ViewListener.ViewListener {
  /**
   * @deprecated
   * @param context The input context.
   * @param action The input action.
   * @param callback The callback that will be called when the input is triggered.
   * @returns The input watcher ID.
   */
  static addInputChangeCallback(context, action, callback) {
    const idWatcher = "InputWatcher_" + context + "_" + action + "_" + UUID.GenerateUuid();
    _InputsListener.inputsListener.trigger("ADD_INPUT_WATCHER", idWatcher, context, action);
    _InputsListener.inputsListener.on("InputListener.InputChange", (id, down) => {
      if (id === idWatcher) {
        callback(down);
      }
    });
    return idWatcher;
  }
  /**
   * @deprecated
   * @param id The event ID.
   */
  static removeInputChangeCallback(id) {
    _InputsListener.inputsListener.trigger("REMOVE_INPUT_WATCHER", id);
  }
};
_InputsListener.isLoaded = Subject.create(false);
_InputsListener.inputsListener = RegisterViewListener(
  "JS_LISTENER_INPUTS",
  () => _InputsListener.isLoaded.set(true)
);
let InputsListener = _InputsListener;
class InputStackListener {
  constructor() {
    this._isReady = Subject.create(false);
    this.isReady = this._isReady;
    this.inputActionMap = /* @__PURE__ */ new Map();
    this.inputStackListener = RegisterViewListener(
      "JS_LISTENER_INPUT_STACK",
      () => this._isReady.set(true)
    );
    this.onInputTriggeredCallback = this._onInputTriggeredCallback.bind(this);
    this.inputStackListener.on("ON_INPUT_TRIGGERED", this.onInputTriggeredCallback);
  }
  _onInputTriggeredCallback(task, value2, _activeId, uuid) {
    var _a;
    (_a = this.inputActionMap.get(uuid)) == null ? void 0 : _a(task, value2);
  }
  /**
   * Add an action that will be executed when the given input is triggered.
   * @param input The input that, when triggered, will execute the action.
   * @param callback The action callback that will be called when the input is triggered.
   * @param inputType The trigger type of the input between 'pressed', 'released' and 'axis'. Default to 'released'.
   * @param context The context which the input will be applied to. Default to 'DEFAULT'.
   * @returns A callback that has to be called when the input action is not needed anymore.
   */
  addInputAction(input, callback, inputType = "released", context = "DEFAULT") {
    const uuid = UUID.GenerateUuid().toUpperCase();
    const formattedUuid = `{${uuid}}`;
    this.inputActionMap.set(uuid, (task, value2) => {
      const isConsumed = callback(value2);
      this.inputStackListener.call(`RESOLVE_${task}`, isConsumed);
    });
    this.inputStackListener.trigger("ADD_INPUT_ACTION", input, formattedUuid, "", inputType, context);
    return () => {
      this.inputStackListener.trigger("REMOVE_INPUT_ACTION", input, formattedUuid);
      this.inputActionMap.delete(uuid);
    };
  }
}
class InternalInputManager {
  constructor(inputActionRecord) {
    this.inputActionRecord = inputActionRecord;
    this.defaultInputType = "released";
    this.defaultScrollAxis = "all";
    this.defaultScrollSpeedFactor = 10;
    this.inputStackListener = new InputStackListener();
  }
  //#region Add input action
  /**
   * Add an action that will be executed when the given input is triggered.
   * @param inputAction The input that, when triggered, will execute the action.
   * @param callback The action callback that will be called when the input is triggered.
   * @param options The optional input action options.
   * @returns A callback that has to be called when the input action is not needed anymore.
   */
  addInputAction(inputAction, callback, options) {
    var _a;
    return this.inputStackListener.addInputAction(
      this.inputActionRecord[inputAction],
      callback,
      (_a = options == null ? void 0 : options.inputType) != null ? _a : this.defaultInputType
    );
  }
  //#endregion
  //#region On hover
  /**
   * Add an action that will be executed when the given input is triggered on hover of the given HTML element.
   * @param element The HTML element that will allow the action to be executed when hovered on.
   * @param inputAction The input that, when triggered, will execute the action.
   * @param callback The action callback that will be called when the input is triggered.
   * @param options The optional input action options.
   * @returns A callback that has to be called when the input action is not needed anymore.
   */
  addInputActionOnHover(element, inputAction, callback, options) {
    let inputActionDestructor = null;
    const mouseEnterCallback = () => {
      var _a;
      if (inputActionDestructor) {
        inputActionDestructor();
      }
      inputActionDestructor = this.inputStackListener.addInputAction(
        this.inputActionRecord[inputAction],
        callback,
        (_a = options == null ? void 0 : options.inputType) != null ? _a : this.defaultInputType
      );
    };
    const mouseLeaveCallback = () => {
      inputActionDestructor == null ? void 0 : inputActionDestructor();
    };
    element.addEventListener("mouseenter", mouseEnterCallback);
    element.addEventListener("mouseleave", mouseLeaveCallback);
    return () => {
      element.removeEventListener("mouseenter", mouseEnterCallback);
      element.removeEventListener("mouseleave", mouseLeaveCallback);
      inputActionDestructor == null ? void 0 : inputActionDestructor();
    };
  }
  //#endregion
}
class InputManager extends InternalInputManager {
  constructor() {
    super(InputActions);
  }
  //#region Scroll
  /**
   * Add scroll input actions using the gamepad joystick for a given HTML element.
   * @param element The scrollable HTML element.
   * @param options The optional scroll input action options.
   * @returns A callback that has to be called when the scroll input action is not needed anymore.
   */
  addScrollInputAction(element, options) {
    var _a;
    const scrollSpeed = options == null ? void 0 : options.scrollSpeed;
    let predicate;
    if (scrollSpeed === void 0) {
      predicate = (axis) => this.computeScrollSpeedFactor(axis, this.defaultScrollSpeedFactor);
    } else if (typeof scrollSpeed === "number") {
      predicate = (axis) => this.computeScrollSpeedFactor(axis, scrollSpeed);
    } else {
      predicate = scrollSpeed;
    }
    switch ((_a = options == null ? void 0 : options.scrollAxis) != null ? _a : this.defaultScrollAxis) {
      case "vertical":
      case "y":
        return this.setupVerticalScroll(element, predicate);
      case "horizontal":
      case "x":
        return this.setupHorizontalScroll(element, predicate);
      default: {
        const verticalScrollDestructor = this.setupVerticalScroll(element, predicate);
        const horizontalScrollDestructor = this.setupHorizontalScroll(element, predicate);
        return () => {
          verticalScrollDestructor();
          horizontalScrollDestructor();
        };
      }
    }
  }
  setupVerticalScroll(element, predicate) {
    const scrollUpInputActionDestructor = this.addInputActionOnHover(
      element,
      InputAction.SCROLL_UP,
      (axisValue) => {
        if (axisValue < 0) {
          return false;
        }
        element.scroll({
          top: element.scrollTop - predicate(axisValue)
        });
        return true;
      },
      { inputType: "axis" }
    );
    const scrollDownInputActionDestructor = this.addInputActionOnHover(
      element,
      InputAction.SCROLL_DOWN,
      (axisValue) => {
        if (axisValue < 0) {
          return false;
        }
        element.scroll({
          top: element.scrollTop + predicate(axisValue)
        });
        return true;
      },
      { inputType: "axis" }
    );
    return () => {
      scrollUpInputActionDestructor();
      scrollDownInputActionDestructor();
    };
  }
  setupHorizontalScroll(element, predicate) {
    const scrollLeftInputActionDestructor = this.addInputActionOnHover(
      element,
      InputAction.SCROLL_LEFT,
      (axisValue) => {
        if (axisValue < 0) {
          return false;
        }
        element.scroll({
          left: element.scrollLeft - predicate(axisValue)
        });
        return true;
      },
      { inputType: "axis" }
    );
    const scrollRightInputActionDestructor = this.addInputActionOnHover(
      element,
      InputAction.SCROLL_RIGHT,
      (axisValue) => {
        if (axisValue < 0) {
          return false;
        }
        element.scroll({
          left: element.scrollLeft + predicate(axisValue)
        });
        return true;
      },
      { inputType: "axis" }
    );
    return () => {
      scrollLeftInputActionDestructor();
      scrollRightInputActionDestructor();
    };
  }
  computeScrollSpeedFactor(axisValue, factor) {
    return axisValue * axisValue * factor;
  }
  //#endregion
}
const _GamepadUiComponent = class _GamepadUiComponent extends DisplayComponent {
  constructor() {
    var _a, _b;
    super(...arguments);
    this.classSubscriptions = [];
    this.gamepadUiComponentRef = FSComponent.createRef();
    this.inputManager = new InputManager();
    this.areGamepadInputsEnabled = Subject.create(false);
    this._nextHandler = Subject.create(void 0);
    this.nextHandler = this._nextHandler;
    this.disabled = SubscribableUtils.toSubscribable((_a = this.props.disabled) != null ? _a : false, true);
    this.visible = SubscribableUtils.toSubscribable((_b = this.props.visible) != null ? _b : true, true);
    this.componentClickListener = this.handleComponentClick.bind(this);
  }
  setNextGamepadEventHandler(ref) {
    this._nextHandler.set(ref);
  }
  deletePreviousGamepadEventHandler() {
    throw new Error("Method not implemented.");
  }
  handleGamepadEvent(gamepadEvent) {
    console.log(`Received ${gamepadEvent} in handleMoveEvent`);
  }
  onAfterRender(node) {
    super.onAfterRender(node);
    const gamepadUiComponentInstance = this.gamepadUiComponentRef.getOrDefault();
    if (gamepadUiComponentInstance === null) {
      return;
    }
    const classes = mergeClassProp(this.props.class);
    Object.keys(classes).forEach((className) => {
      const subscribable = SubscribableUtils.toSubscribable(classes[className], true);
      this.classSubscriptions.push(
        subscribable.sub((value2) => {
          gamepadUiComponentInstance.classList.toggle(className, value2);
        }, true)
      );
    });
    this.isVisibleSubscription = this.visible.sub((isVisible) => {
      gamepadUiComponentInstance.hidden = !isVisible;
    }, true);
    if (SubscribableUtils.isSubscribable(this.props.disabled)) {
      this.isDisabledSubscription = this.props.disabled.sub((disabled) => {
        disabled ? this.disable() : this.enable();
      }, true);
    } else {
      this.props.disabled ? this.disable() : this.enable();
    }
    window.addEventListener("click", this.componentClickListener);
    if (this.props.onboardingStepId !== void 0) {
      gamepadUiComponentInstance.setAttribute("id", this.props.onboardingStepId);
    }
  }
  handleComponentClick(e) {
    if (!this.gamepadUiComponentRef.instance.contains(e.target)) {
      this.onClickOutOfComponent(e);
    }
  }
  onButtonAPressed() {
    if (this.props.onButtonAPressed !== void 0) {
      this.props.onButtonAPressed();
    }
  }
  onButtonBPressed() {
    if (this.props.onButtonBPressed !== void 0) {
      this.props.onButtonBPressed();
    }
  }
  onClickOutOfComponent(_e) {
    return;
  }
  enableGamepadInputs() {
    if (this.areGamepadInputsEnabled.get()) {
      console.warn("Trying to enable gamepad inputs that are already enabled. Operation cancelled.");
      return;
    }
    this.areGamepadInputsEnabled.set(true);
  }
  disableGamepadInputs() {
    this.areGamepadInputsEnabled.set(false);
  }
  enable() {
    this.gamepadUiComponentRef.instance.removeAttribute("disabled");
  }
  disable() {
    this.gamepadUiComponentRef.instance.setAttribute("disabled", "");
  }
  show() {
    this.gamepadUiComponentRef.instance.style.visibility = "visible";
  }
  hide() {
    this.gamepadUiComponentRef.instance.style.visibility = "hidden";
  }
  toggleFocus(value2) {
    if (value2 !== void 0) {
      if (value2 === true) {
        this.gamepadUiComponentRef.instance.classList.add(_GamepadUiComponent.FOCUS_CLASS);
      } else {
        this.gamepadUiComponentRef.instance.classList.remove(_GamepadUiComponent.FOCUS_CLASS);
      }
      return;
    }
    this.gamepadUiComponentRef.instance.classList.toggle(_GamepadUiComponent.FOCUS_CLASS);
  }
  getComponentRect() {
    return this.gamepadUiComponentRef.instance.getBoundingClientRect();
  }
  destroy() {
    var _a, _b;
    this.classSubscriptions.forEach((subscription) => subscription.destroy());
    (_a = this.isVisibleSubscription) == null ? void 0 : _a.destroy();
    (_b = this.isDisabledSubscription) == null ? void 0 : _b.destroy();
    window.removeEventListener("click", this.componentClickListener);
    super.destroy();
  }
};
_GamepadUiComponent.FOCUS_CLASS = "focus";
let GamepadUiComponent = _GamepadUiComponent;
class GamepadUiParser {
  constructor() {
    this.gamepadUiViewVNode = null;
  }
  /** On veut focus le premier élément */
  bindVNodeReference(gamepadUiViewVNode) {
    this.gamepadUiViewVNode = gamepadUiViewVNode;
    this.focusFirstElement();
  }
  focusFirstElement() {
    if (this.currentElement = this.findFirstElement()) {
      this.currentElement.instance.toggleFocus(true);
    }
  }
  goUp() {
    this.goDir("UP");
  }
  goRight() {
    this.goDir("RIGHT");
  }
  goDown() {
    this.goDir("DOWN");
  }
  goLeft() {
    this.goDir("LEFT");
  }
  pushButtonA() {
    var _a;
    (_a = this.currentElement) == null ? void 0 : _a.instance.onButtonAPressed();
  }
  pushButtonB() {
    var _a;
    (_a = this.currentElement) == null ? void 0 : _a.instance.onButtonBPressed();
  }
  goDir(dir) {
    const HTMLElementList = this.parseDOM();
    if (!this.currentElement || HTMLElementList === null) {
      return;
    }
    const nextElement = this.findClosestNode(this.currentElement, HTMLElementList, dir);
    if (nextElement !== null) {
      this.currentElement.instance.toggleFocus(false);
      this.currentElement = nextElement;
      this.currentElement.instance.toggleFocus(true);
    }
  }
  parseDOM() {
    if (this.gamepadUiViewVNode === null) {
      throw new Error(`Can't parse DOM, VNode is null`);
    }
    const VNodeList = [];
    FSComponent.visitNodes(this.gamepadUiViewVNode, (node) => {
      if (node.instance instanceof GamepadUiComponent) {
        const ref = FSComponent.createRef();
        ref.instance = node.instance;
        VNodeList.push(ref);
      }
      return false;
    });
    return VNodeList.length ? VNodeList : null;
  }
  findFirstElement() {
    const HTMLElementList = this.parseDOM();
    if (HTMLElementList === null) {
      return null;
    }
    return HTMLElementList[0];
  }
  /** On réduit la liste dont le top est en dessous du bottom du current node. */
  findClosestNode(currentGamepadUiComponent, gamepadUiComponentList, direction) {
    let candidateIndex = -1;
    switch (direction) {
      case "UP":
        candidateIndex = this.findBestCandidate(
          currentGamepadUiComponent,
          gamepadUiComponentList,
          "bottom",
          "top"
        );
        break;
      case "RIGHT":
        candidateIndex = this.findBestCandidate(
          currentGamepadUiComponent,
          gamepadUiComponentList,
          "left",
          "right"
        );
        break;
      case "DOWN":
        candidateIndex = this.findBestCandidate(
          currentGamepadUiComponent,
          gamepadUiComponentList,
          "top",
          "bottom"
        );
        break;
      case "LEFT":
        candidateIndex = this.findBestCandidate(
          currentGamepadUiComponent,
          gamepadUiComponentList,
          "right",
          "left"
        );
        break;
    }
    if (candidateIndex !== -1 && candidateIndex < gamepadUiComponentList.length) {
      return gamepadUiComponentList[candidateIndex];
    }
    return null;
  }
  findBestCandidate(currentGamepadUiComponent, gamepadUiComponentList, side1, side2) {
    const htmlElementCandidates = [];
    let closestDistance = Infinity;
    let candidateIndex = -1;
    const currentHTMLElementRect = currentGamepadUiComponent.instance.getComponentRect();
    gamepadUiComponentList.forEach((htmlElement, index2) => {
      const htmlElementRect = htmlElement.instance.getComponentRect();
      if (this.isRectPosValid(htmlElementRect, currentHTMLElementRect, side1, side2, false)) {
        if (!htmlElementCandidates.some(
          (htmlElementCandidate) => this.isRectPosValid(
            htmlElementRect,
            htmlElementCandidate.instance.getComponentRect(),
            side1,
            side1,
            true
          )
        )) {
          htmlElementCandidates.push(htmlElement);
          const corner = side1 === "bottom" || side1 === "top" ? "left" : "top";
          const distance = this.distances1D(htmlElementRect, currentHTMLElementRect, corner);
          if (distance <= closestDistance) {
            closestDistance = distance;
            candidateIndex = index2;
          }
        }
      }
    });
    return candidateIndex;
  }
  isRectPosValid(rect1, rect2, side1, side2, strict) {
    if (side1 === "bottom" || side1 === "right") {
      if (strict) {
        return rect1[side1] < rect2[side2];
      } else {
        return rect1[side1] <= rect2[side2];
      }
    } else {
      if (strict) {
        return rect1[side1] > rect2[side2];
      } else {
        return rect1[side1] >= rect2[side2];
      }
    }
  }
  distances1D(rect1, rect2, side) {
    return Math.abs(rect2[side] - rect1[side]);
  }
}
class UiView extends DisplayComponent {
  constructor() {
    super(...arguments);
    this.services = [];
  }
  onOpen() {
    var _a;
    for (const service of this.services) {
      (_a = service.onOpen) == null ? void 0 : _a.call(service);
    }
  }
  onClose() {
    var _a;
    for (const service of this.services) {
      (_a = service.onClose) == null ? void 0 : _a.call(service);
    }
  }
  onResume() {
    var _a;
    for (const service of this.services) {
      (_a = service.onResume) == null ? void 0 : _a.call(service);
    }
  }
  onPause() {
    var _a;
    for (const service of this.services) {
      (_a = service.onPause) == null ? void 0 : _a.call(service);
    }
  }
  onUpdate(time) {
    var _a;
    for (const service of this.services) {
      (_a = service.onUpdate) == null ? void 0 : _a.call(service, time);
    }
  }
  destroy() {
    var _a;
    for (const service of this.services) {
      (_a = service.destroy) == null ? void 0 : _a.call(service);
    }
    super.destroy();
  }
}
class GamepadUiView extends UiView {
  constructor() {
    super(...arguments);
    this.gamepadUiViewRef = FSComponent.createRef();
    this.gamepadUiParser = new GamepadUiParser();
    this._nextHandler = Subject.create(void 0);
    this.nextHandler = this._nextHandler;
    this.inputManager = new InputManager();
    this.gamepadComponentChildren = [];
    this.areGamepadInputsEnabled = Subject.create(false);
    this.viewCoreInputManager = new InternalInputManager(InternalInputActions);
  }
  /**
   * Enable the gamepad inputs for this UI view.
   * Gamepad inputs of any UI view shall be added in its own enableGamepadInputs function via inputManager.
   */
  enableGamepadInputs() {
    if (this.areGamepadInputsEnabled.get()) {
      console.warn("Trying to enable gamepad inputs that are already enabled. Operation cancelled.");
      return;
    }
    this.goBackActionDestructor = this.viewCoreInputManager.addInputAction(
      InternalInputAction.BACK,
      () => {
        if (this.props.appViewService === void 0) {
          return false;
        }
        try {
          this.props.appViewService.goBack();
        } catch (e) {
          return false;
        }
        return true;
      }
    );
    this.gamepadComponentChildren.forEach((child) => child.enableGamepadInputs());
    this.areGamepadInputsEnabled.set(true);
  }
  /**
   * Disable the gamepad inputs for this UI view.
   * Gamepad input destructors of any UI view shall be called in its own disableGamepadInputs function.
   */
  disableGamepadInputs() {
    var _a;
    (_a = this.goBackActionDestructor) == null ? void 0 : _a.call(this);
    this.gamepadComponentChildren.forEach((child) => child.disableGamepadInputs());
    this.areGamepadInputsEnabled.set(false);
  }
  onClose() {
    super.onClose();
    this.disableGamepadInputs();
  }
  onResume() {
    super.onResume();
    this.enableGamepadInputs();
  }
  onPause() {
    super.onPause();
    this.disableGamepadInputs();
  }
  onAfterRender(node) {
    super.onAfterRender(node);
    this.gamepadUiParser.bindVNodeReference(node);
    FSComponent.visitNodes(node, (childNode) => {
      if (childNode.instance instanceof GamepadUiComponent) {
        this.gamepadComponentChildren.push(childNode.instance);
      }
      return false;
    });
  }
  destroy() {
    super.destroy();
    this.disableGamepadInputs();
  }
  setNextGamepadEventHandler(ref) {
    this._nextHandler.set(ref);
  }
  deletePreviousGamepadEventHandler() {
    this._nextHandler.set(void 0);
  }
  handleGamepadEvent(_gamepadEvent) {
    if (_gamepadEvent === GamepadEvents.BUTTON_B) {
      this.gamepadUiParser.pushButtonB();
    }
    const nextHandler = this._nextHandler.get();
    if (nextHandler !== void 0) {
      return nextHandler.handleGamepadEvent(_gamepadEvent);
    }
    switch (_gamepadEvent) {
      case GamepadEvents.JOYDIR_UP:
        this.gamepadUiParser.goUp();
        break;
      case GamepadEvents.JOYDIR_RIGHT:
        this.gamepadUiParser.goRight();
        break;
      case GamepadEvents.JOYDIR_DOWN:
        this.gamepadUiParser.goDown();
        break;
      case GamepadEvents.JOYDIR_LEFT:
        this.gamepadUiParser.goLeft();
        break;
      case GamepadEvents.BUTTON_A:
        this.gamepadUiParser.pushButtonA();
        break;
    }
  }
}
var ViewBootMode = /* @__PURE__ */ ((ViewBootMode2) => {
  ViewBootMode2[ViewBootMode2["COLD"] = 0] = "COLD";
  ViewBootMode2[ViewBootMode2["HOT"] = 1] = "HOT";
  return ViewBootMode2;
})(ViewBootMode || {});
var ViewSuspendMode = /* @__PURE__ */ ((ViewSuspendMode2) => {
  ViewSuspendMode2[ViewSuspendMode2["SLEEP"] = 0] = "SLEEP";
  ViewSuspendMode2[ViewSuspendMode2["TERMINATE"] = 1] = "TERMINATE";
  return ViewSuspendMode2;
})(ViewSuspendMode || {});
const defaultViewOptions = {
  BootMode: ViewBootMode.COLD,
  SuspendMode: ViewSuspendMode.SLEEP
};
class AppViewService {
  constructor(bus) {
    this.registeredUiViewEntries = /* @__PURE__ */ new Map();
    this.appViewStack = [];
    this.registeredUiViewPromises = [];
    this.hasInitialized = false;
    this._currentUiView = Subject.create(null);
    this.currentUiView = this._currentUiView;
    this.bus = bus;
    this.goHomeSub = this.bus.on(
      "_efb_appviewservice_go_home",
      () => {
        const steps = this.appViewStack.length - 1;
        if (steps >= 1) {
          this.goBack(steps);
        }
      },
      true
    );
  }
  onOpen() {
    var _a, _b;
    this.goHomeSub.resume();
    (_b = (_a = this.getActiveUiViewEntry()) == null ? void 0 : _a.ref) == null ? void 0 : _b.onOpen();
  }
  onClose() {
    var _a, _b;
    this.goHomeSub.pause();
    (_b = (_a = this.getActiveUiViewEntry()) == null ? void 0 : _a.ref) == null ? void 0 : _b.onClose();
  }
  onResume() {
    var _a, _b;
    this.goHomeSub.resume();
    (_b = (_a = this.getActiveUiViewEntry()) == null ? void 0 : _a.ref) == null ? void 0 : _b.onResume();
  }
  onPause() {
    var _a, _b;
    this.goHomeSub.pause();
    (_b = (_a = this.getActiveUiViewEntry()) == null ? void 0 : _a.ref) == null ? void 0 : _b.onPause();
  }
  /**
   * Registers and renders a view (page or popup) to be opened by the service.
   * @param key The UiView string key.
   * @param type The view type
   * @param vNodeFactory A function that returns a {@link UiView} VNode for the key
   * @param options The {@link UiView} {@link ViewOptions}
   * @returns UiViewEntry
   */
  registerView(key, type, vNodeFactory, options) {
    if (this.registeredUiViewEntries.has(key)) {
      throw new Error(`View "${key}" is already used`);
    } else if (typeof vNodeFactory !== "function") {
      throw new Error("vNodeFactory has to be a function returning a VNode");
    }
    const viewOptions = Object.assign({}, defaultViewOptions, options);
    const appViewEntry = {
      key,
      render: vNodeFactory,
      vNode: vNodeFactory,
      ref: null,
      containerRef: FSComponent.createRef(),
      isVisible: Subject.create(false),
      layer: Subject.create(-1),
      type: Subject.create(type),
      isInit: false,
      viewOptions
    };
    if (viewOptions.BootMode === ViewBootMode.HOT) {
      this.initViewEntry(appViewEntry);
    }
    this.registeredUiViewEntries.set(key, appViewEntry);
    return appViewEntry;
  }
  registerPage(key, vNodeFactory, options) {
    return this.registerView(key, "page", vNodeFactory, options);
  }
  registerPopup(key, vNodeFactory, options) {
    return this.registerView(key, "popup", vNodeFactory, options);
  }
  async killView(viewEntry) {
    var _a;
    if (viewEntry.isInit === false) {
      return;
    }
    if (this.appViewStack.find((view) => view.key === viewEntry.key)) {
      throw new Error("You must close your view before killing it");
    }
    viewEntry.isInit = false;
    (_a = viewEntry.ref) == null ? void 0 : _a.destroy();
    viewEntry.containerRef.instance.destroy();
    viewEntry.ref = null;
    viewEntry.containerRef = FSComponent.createRef();
    viewEntry.vNode = viewEntry.render.bind(viewEntry);
    return new Promise((resolve) => {
      if (viewEntry.viewOptions.BootMode === ViewBootMode.HOT) {
        this.initViewEntry(viewEntry);
      }
      resolve();
    });
  }
  /**
   * @param entry a {@link UiViewEntry}
   * @param shouldOpen opens the view on initialization, defaults to false
   */
  initViewEntry(entry, shouldOpen = false) {
    var _a;
    if (entry.isInit) {
      return;
    }
    entry.isInit = true;
    entry.vNode = value(entry.vNode);
    entry.ref = entry.vNode.instance;
    (_a = this.appViewRef) == null ? void 0 : _a.renderView(
      /* @__PURE__ */ FSComponent.buildComponent(
        AppViewWrapper,
        {
          viewName: entry.key,
          isVisible: entry.isVisible,
          type: entry.type,
          layer: entry.layer,
          ref: entry.containerRef
        },
        entry.vNode
      )
    );
    shouldOpen && entry.ref.onOpen();
  }
  /**
   * Destroys every view in registered view entries and resets the view stack.
   */
  unload() {
    var _a;
    this.registeredUiViewPromises = [];
    for (const viewEntry of this.registeredUiViewEntries.values()) {
      (_a = viewEntry.ref) == null ? void 0 : _a.destroy();
    }
    this.registeredUiViewEntries.clear();
    this.appViewStack.splice(0, this.appViewStack.length);
    this.hasInitialized = false;
  }
  /**
   *
   * @param homePageUiViewKey the string key of the {@link UiView}
   * @returns a Promise resolving when all pages are initialized
   */
  async initialize(homePageUiViewKey) {
    if (this.hasInitialized) {
      return Promise.resolve();
    }
    return Promise.all(this.registeredUiViewPromises).then(() => {
      this.initializeAppViewStack(homePageUiViewKey);
      this.hasInitialized = true;
    });
  }
  /**
   * @returns the current active view entry.
   */
  getActiveUiViewEntry() {
    const activeViewEntry = ArrayUtils.peekLast(this.appViewStack);
    return activeViewEntry;
  }
  /** @deprecated */
  on(_event, _viewKey, _callback) {
    return this;
  }
  /**
   * Handles logic associated with changing the open page.
   * @param page Page to close
   */
  handleCloseView(page) {
    var _a;
    if (!page || page.isVisible.get() === false) return;
    (_a = page.ref) == null ? void 0 : _a.onPause();
    page.isVisible.set(false);
    this._currentUiView.set(null);
    if (page.viewOptions.SuspendMode === ViewSuspendMode.TERMINATE) {
      setTimeout(() => this.killView(page));
    }
  }
  /**
   * Handles logic associated with changing the open page.
   * @param page Page to open
   */
  handleOpenView(page) {
    var _a;
    if (!page || page.isVisible.get() === true) return;
    (_a = page.ref) == null ? void 0 : _a.onResume();
    page.isVisible.set(true);
    this._currentUiView.set(page);
  }
  /**
   * Populate the view stack with its respective home page.
   * @param mainPageUiViewKey the key of the home page
   */
  initializeAppViewStack(mainPageUiViewKey) {
    this.open(mainPageUiViewKey);
  }
  /**
   * @param key the {@link UiView} string key
   * @returns the {@link UiViewEntry} corresponding to the key
   * @throws if the {@link UiViewEntry} doesn't exists
   */
  getUiViewEntry(key) {
    const appViewEntry = this.registeredUiViewEntries.get(key);
    if (!appViewEntry) {
      throw new Error(`${key} wasn't registered as a view`);
    }
    return appViewEntry;
  }
  /**
   * Called by AppContainer to pass in the refs to the view.
   * Should only be called once.
   * @param appViewRef The app view ref.
   */
  onAppContainerRendered(appViewRef) {
    this.appViewRef = appViewRef;
  }
  isInViewStack(view) {
    const key = typeof view === "string" ? view : view.key;
    return this.appViewStack.findIndex((stackViewEntry) => key === stackViewEntry.key) !== -1;
  }
  isActive(view) {
    const key = typeof view === "string" ? view : view.key;
    const activeViewEntry = this.getActiveUiViewEntry();
    if (activeViewEntry === void 0) {
      return false;
    }
    return key === activeViewEntry.key;
  }
  open(key) {
    const viewEntry = this.advanceViewStack(key);
    return viewEntry;
  }
  goBack(steps) {
    steps != null ? steps : steps = 1;
    if (steps <= 0) {
      throw new RangeError(`Steps must be superior to 0.`);
    } else if (steps >= this.appViewStack.length) {
      throw new RangeError(`Steps can't be superior to ${this.appViewStack.length} when called.`);
    }
    const activeViewEntry = this.getActiveUiViewEntry();
    if (this.appViewStack.length > steps && activeViewEntry) {
      const viewsToOpen = [];
      const viewsToClose = [];
      while (steps--) {
        const view = this.appViewStack.pop();
        if (view) {
          viewsToClose.push(view);
        }
      }
      let i = this.appViewStack.length;
      do {
        viewsToOpen.push(this.appViewStack[--i]);
      } while (i > 0 && this.appViewStack[i].type.get() !== "page");
      for (const page of viewsToClose) {
        this.handleCloseView(page);
      }
      for (const page of viewsToOpen.reverse()) {
        this.handleOpenView(page);
      }
    }
    return this.getActiveUiViewEntry();
  }
  /**
   * Handles view stack logic
   * @param key the {@link UiView} string key
   * @returns the current {@link UiViewEntry}
   */
  advanceViewStack(key) {
    const viewEntry = this.getUiViewEntry(key);
    if (this.appViewStack.includes(viewEntry)) {
      if (this.getActiveUiViewEntry() !== viewEntry) {
        throw new Error("Page or popup is already in the viewstack and can't be opened twice");
      }
      return viewEntry;
    }
    switch (viewEntry.type.get()) {
      case "page":
        for (const page of [...this.appViewStack].reverse()) {
          this.handleCloseView(page);
        }
        break;
    }
    this.appViewStack.push(viewEntry);
    this.openView(viewEntry);
    this.handleOpenView(viewEntry);
    return viewEntry;
  }
  /**
   * Handle logic associated with opening a view
   * @param view the view to open.
   */
  openView(view) {
    if (!view.isInit) {
      this.initViewEntry(view, true);
    }
    const index2 = this.appViewStack.indexOf(view);
    if (index2 < 0) {
      throw new Error(`AppViewService: view not found in view stack: ${view.key}`);
    }
    view.layer.set(index2);
  }
  /**
   * Updates all the pages/popups that are initialized and visible
   * @param time timestamp
   */
  update(time) {
    this.registeredUiViewEntries.forEach((UiView2) => {
      var _a;
      if (UiView2.isInit && UiView2.isVisible.get()) {
        (_a = UiView2.ref) == null ? void 0 : _a.onUpdate(time);
      }
    });
  }
  /**
   * Routes the event to the current {@link UiView}
   * @param gamepadEvent the {@link GamepadEvents}
   */
  routeGamepadInteractionEvent(gamepadEvent) {
    const _currentUiView = this.getActiveUiViewEntry();
    if ((_currentUiView == null ? void 0 : _currentUiView.ref) instanceof GamepadUiView) {
      _currentUiView.ref.handleGamepadEvent(gamepadEvent);
    }
  }
}
class AppViewWrapper extends DisplayComponent {
  constructor() {
    super(...arguments);
    this.rootRef = FSComponent.createRef();
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent(
      "div",
      {
        ref: this.rootRef,
        class: {
          "ui-view": true,
          [this.props.viewName]: true,
          hidden: this.props.isVisible.map(SubscribableMapFunctions.not()),
          page: this.props.type.map(where("page")),
          popup: this.props.type.map(where("popup"))
        },
        style: {
          "z-index": this.props.layer.map(toString())
        }
      },
      this.props.children
    );
  }
  destroy() {
    var _a;
    const root = this.rootRef.getOrDefault();
    if (root !== null) {
      (_a = root.parentNode) == null ? void 0 : _a.removeChild(root);
    }
    super.destroy();
  }
}
var __defProp2 = Object.defineProperty;
var __getOwnPropDesc = Object.getOwnPropertyDescriptor;
var __decorateClass = (decorators, target, key, kind) => {
  var result = __getOwnPropDesc(target, key);
  for (var i = decorators.length - 1, decorator; i >= 0; i--)
    if (decorator = decorators[i])
      result = decorator(target, key, result) || result;
  if (result) __defProp2(target, key, result);
  return result;
};
class AppView extends DisplayComponent {
  constructor(props) {
    var _a;
    super(props);
    this.rootRef = FSComponent.createRef();
    this.pageKeyActions = /* @__PURE__ */ new Map();
    this._appViewService = props.appViewService || (props.bus ? new AppViewService(props.bus) : void 0);
    this._bus = (_a = this._appViewService) == null ? void 0 : _a.bus;
    this._unitsSettingManager = props.unitsSettingManager;
    this._efbSettingsManager = props.efbSettingsManager;
    this._notificationManager = props.notificationManager;
    this._onboardingManager = props.onboardingManager;
  }
  get appViewService() {
    const _appViewService = this._appViewService;
    if (!_appViewService) {
      throw new Error(
        `Cannot resolve 'appViewService' because none of 'appViewService' and 'bus' props were given.`
      );
    }
    return _appViewService;
  }
  get bus() {
    const _bus = this._bus;
    if (!_bus) {
      throw new Error(`Cannot resolve 'bus' because none of 'appViewService' and 'bus' props were given.`);
    }
    return _bus;
  }
  get unitsSettingManager() {
    const _unitsSettingManager = this._unitsSettingManager;
    if (_unitsSettingManager === void 0) {
      throw new Error(`Cannot resolve 'unitsSettingManager'`);
    }
    return _unitsSettingManager;
  }
  get efbSettingsManager() {
    const _efbSettingsManager = this._efbSettingsManager;
    if (_efbSettingsManager === void 0) {
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
      throw new Error("Notification manager is not defined");
    }
    return notificationManager;
  }
  get onboardingManager() {
    const _onboardingManager = this._onboardingManager;
    if (!_onboardingManager) {
      throw new Error(`Cannot resolve 'onboardingManager'`);
    }
    return _onboardingManager;
  }
  /**
   * Called once when the view is opened for the first time.
   */
  onOpen() {
    var _a;
    (_a = this._appViewService) == null ? void 0 : _a.onOpen();
  }
  /**
   * Called once when the view is destroyed.
   */
  onClose() {
    var _a;
    (_a = this._appViewService) == null ? void 0 : _a.onClose();
  }
  /**
   * Called each time the view is resumed.
   */
  onResume() {
    var _a;
    (_a = this._appViewService) == null ? void 0 : _a.onResume();
  }
  /**
   * Called each time the view is closed.
   */
  onPause() {
    var _a;
    (_a = this._appViewService) == null ? void 0 : _a.onPause();
  }
  /**
   * On Update loop - It update the `AppViewService` if it is used.
   * @param time in milliseconds
   */
  onUpdate(time) {
    var _a;
    (_a = this._appViewService) == null ? void 0 : _a.update(time);
  }
  /**
   * Callback to register all views the app might use.
   */
  registerViews() {
  }
  /**
   * @internal
   * TODO : Need to be documented after Gamepad integration
   */
  routeGamepadInteractionEvent(gamepadEvent) {
    var _a;
    (_a = this._appViewService) == null ? void 0 : _a.routeGamepadInteractionEvent(gamepadEvent);
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
    if (action === void 0) {
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
    return /* @__PURE__ */ FSComponent.buildComponent(AppContainer, { appViewService: this.appViewService });
  }
  /** @inheritdoc */
  onAfterRender(node) {
    var _a;
    super.onAfterRender(node);
    this.registerViews();
    if (this.defaultView) {
      (_a = this._appViewService) == null ? void 0 : _a.initialize(this.defaultView);
    }
  }
  /** @internal */
  destroy() {
    var _a;
    (_a = this._appViewService) == null ? void 0 : _a.unload();
    super.destroy();
  }
}
__decorateClass([
  Memoize()
], AppView.prototype, "appViewService");
__decorateClass([
  Memoize()
], AppView.prototype, "bus");
__decorateClass([
  Memoize()
], AppView.prototype, "unitsSettingManager");
__decorateClass([
  Memoize()
], AppView.prototype, "efbSettingsManager");
__decorateClass([
  Memoize()
], AppView.prototype, "onboardingManager");
class Button extends GamepadUiComponent {
  constructor() {
    var _a, _b, _c, _d;
    super(...arguments);
    this.isButtonDisabled = SubscribableUtils.toSubscribable((_a = this.props.disabled) != null ? _a : false, true);
    this.isButtonHoverable = MappedSubject.create(
      ([isHoverable, isDisabled]) => {
        return isHoverable && !isDisabled;
      },
      SubscribableUtils.toSubscribable((_b = this.props.hoverable) != null ? _b : true, true),
      this.isButtonDisabled
    );
    this.isButtonSelected = MappedSubject.create(
      ([isSelected, state]) => isSelected || state,
      SubscribableUtils.toSubscribable((_c = this.props.selected) != null ? _c : false, true),
      SubscribableUtils.toSubscribable((_d = this.props.state) != null ? _d : false, true)
    );
  }
  /**
   * @deprecated Old way to render the button. Instead of extending the `Button` class, render your content as a children of `<Button>...</Button>`.
   */
  buttonRender() {
    return null;
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent(
      "button",
      {
        ref: this.gamepadUiComponentRef,
        class: mergeClassProp(
          "button",
          "abstract-button",
          // deprecated
          {
            hoverable: this.isButtonHoverable,
            selected: this.isButtonSelected
          },
          this.props.class
        ),
        style: this.props.style
      },
      /* @__PURE__ */ FSComponent.buildComponent("div", { class: "disabled-layer" }),
      this.props.children,
      this.buttonRender()
    );
  }
  onAfterRender(node) {
    super.onAfterRender(node);
    if (this.props.callback) {
      this.gamepadUiComponentRef.instance.onclick = (event) => {
        var _a;
        if (this.isButtonDisabled.get() === true) {
          return;
        }
        (_a = this.props.callback) == null ? void 0 : _a.call(this, event);
      };
    }
  }
  destroy() {
    this.isButtonHoverable.destroy();
    super.destroy();
  }
}
class AbstractButton extends Button {
}
class TT extends DisplayComponent {
  constructor() {
    super(...arguments);
    this.ref = FSComponent.createRef();
    this.key = SubscribableUtils.toSubscribable(this.props.key, true);
    this.formatter = this.props.format || ((text) => text);
    this.subs = [];
    this.reloadText = this._reloadText.bind(this);
  }
  _reloadText() {
    var _a;
    const key = this.key.get();
    let translatedKey = key.startsWith("@") || key.startsWith("TT") ? Utils.Translate(key) : key;
    (_a = this.props.arguments) == null ? void 0 : _a.forEach((argumentValue, argumentKey) => {
      const argumentValueSub = SubscribableUtils.toSubscribable(argumentValue, true);
      translatedKey = translatedKey.replace(argumentKey, argumentValueSub.get());
    });
    this.updateText(translatedKey);
  }
  getTextTransformed(text) {
    if (isFunction(this.formatter)) {
      return this.formatter(text);
    }
    switch (this.formatter) {
      case "upper":
      case "uppercase":
        return text.toUpperCase();
      case "lower":
      case "lowercase":
        return text.toLowerCase();
      case "ucfirst":
        return `${text.charAt(0).toUpperCase()}${text.slice(1)}`;
      case "capitalize":
        return text.replace(/(^\w{1})|(\s+\w{1})/g, (letter) => letter.toUpperCase());
      default:
        console.warn(`Format "${this.props.format}" is not supported.`);
        return text;
    }
  }
  getEmphasisText(text) {
    let emphasisText;
    const boldRegex = /\*\*(.*?)\*\*/g;
    const italicRegex = /\*(.*?)\*/g;
    emphasisText = text.replace(boldRegex, '<span class="bold-text">$1</span>');
    emphasisText = emphasisText.replace(italicRegex, '<span class="italic-text">$1</span>');
    return emphasisText;
  }
  updateText(text) {
    const formattedText = this.getTextTransformed(text);
    const htmlText = this.getEmphasisText(formattedText);
    this.ref.instance.innerHTML = htmlText;
  }
  render() {
    var _a;
    const Tag2 = (_a = this.props.type) != null ? _a : "span";
    const _b = this.props, { key: _key, type: _type, format: _format, children: _children } = _b, props = __objRest(_b, ["key", "type", "format", "children"]);
    return /* @__PURE__ */ FSComponent.buildComponent(Tag2, __spreadValues({ ref: this.ref }, props));
  }
  onAfterRender(node) {
    var _a;
    super.onAfterRender(node);
    (_a = this.props.arguments) == null ? void 0 : _a.forEach((argumentValue, argumentKey) => {
      const subValue = SubscribableUtils.toSubscribable(argumentValue, true);
      this.subs.push(subValue.sub(this.reloadText));
    });
    this.reloadSubscription = this.key.sub(this.reloadText, true);
    Coherent.on("RELOAD_LOCALISATION", this.reloadText);
  }
  destroy() {
    var _a;
    Coherent.off("RELOAD_LOCALISATION", this.reloadText);
    (_a = this.reloadSubscription) == null ? void 0 : _a.destroy();
    this.subs.forEach((sub) => sub.destroy());
    super.destroy();
  }
}
class GhostListItem extends GamepadUiComponent {
  constructor(props) {
    props.disabled = true;
    props.visible = false;
    super(props);
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent("div", { class: "hidden", ref: this.gamepadUiComponentRef });
  }
}
const _GamepadListNavigationManager = class _GamepadListNavigationManager {
  constructor(data, toolbox) {
    var _a, _b;
    this.data = data;
    this.toolbox = toolbox;
    this.allowedUpcomingNavigationMs = 0;
    this.currentItemIndex = Subject.create(-1);
    this.isListFocusedSubscriptions = [];
    this.currentItemIndexSubscription = this.currentItemIndex.sub((index2) => {
      var _a2, _b2;
      const item = index2 < 0 || index2 >= this.data.length ? null : this.data.get(index2);
      (_b2 = (_a2 = this.toolbox).onItemHovered) == null ? void 0 : _b2.call(_a2, item, index2);
    });
    this.internalInputManager = new InternalInputManager(InternalInputActions);
    this.navigationBetweenItemsDelayMs = (_a = toolbox.navigationBetweenItemsDelayMs) != null ? _a : _GamepadListNavigationManager.defaultNavigationBetweenItemsDelayMs;
    if (toolbox.wrap) {
      this.computeUpcomingItemIndex = (previousItemIndex, step) => (this.data.length + previousItemIndex + step) % this.data.length;
    } else {
      this.computeUpcomingItemIndex = (previousItemIndex, step) => {
        const upcomingItemIndex = previousItemIndex + step;
        if (upcomingItemIndex >= 0 && upcomingItemIndex < this.data.length) {
          return upcomingItemIndex;
        } else {
          return previousItemIndex;
        }
      };
    }
    if (SubscribableUtils.isMutableSubscribable(toolbox.isListFocused)) {
      this.isListFocused = toolbox.isListFocused;
    } else if (SubscribableUtils.isSubscribable(toolbox.isListFocused)) {
      this.isListFocused = Subject.create(toolbox.isListFocused.get());
      this.isListFocusedSubscriptions.push(
        toolbox.isListFocused.sub((value2) => this.isListFocused.set(value2))
      );
    } else {
      this.isListFocused = Subject.create((_b = toolbox.isListFocused) != null ? _b : false);
    }
    this.isListFocusedSubscriptions.push(
      MappedSubject.create(
        ([is2D, isFocused]) => {
          if (is2D && isFocused) {
            this.enableGamepadNavigation();
          } else {
            this.disableGamepadNavigation();
          }
        },
        window.PanelInfo.is2D,
        this.isListFocused
      )
    );
  }
  navigateThroughList(step) {
    const currentNavigationTryMs = Date.now();
    if (currentNavigationTryMs < this.allowedUpcomingNavigationMs) {
      return false;
    }
    this.allowedUpcomingNavigationMs = currentNavigationTryMs + this.navigationBetweenItemsDelayMs;
    const currentItemIndex = this.currentItemIndex.get();
    if (currentItemIndex === -1) {
      return false;
    }
    const upcomingItemIndex = this.computeUpcomingItemIndex(currentItemIndex, step);
    this.currentItemIndex.set(upcomingItemIndex);
    return true;
  }
  enableGamepadNavigation() {
    Coherent.trigger("SET_EFB_GAMEPAD_NAVIGATION", true);
    this.currentItemIndex.set(0);
    this.unfocusListActionDestructor = this.internalInputManager.addInputAction(
      InternalInputAction.BACK,
      () => {
        this.isListFocused.set(false);
        return true;
      }
    );
    this.selectItemActionDestructor = this.internalInputManager.addInputAction(
      InternalInputAction.VALID,
      () => {
        var _a, _b;
        const currentItemIndex = this.currentItemIndex.get();
        try {
          (_b = (_a = this.toolbox).onItemSelected) == null ? void 0 : _b.call(_a, this.data.get(currentItemIndex), currentItemIndex);
        } catch (e) {
          return false;
        }
        return true;
      }
    );
    this.allowedUpcomingNavigationMs = Date.now();
    this.prevItemActionDestructor = this.internalInputManager.addInputAction(
      InternalInputAction.UP,
      () => this.navigateThroughList(
        -1
        /* UP */
      ),
      { inputType: "axis" }
    );
    this.nextItemActionDestructor = this.internalInputManager.addInputAction(
      InternalInputAction.DOWN,
      () => this.navigateThroughList(
        1
        /* DOWN */
      ),
      { inputType: "axis" }
    );
  }
  disableGamepadNavigation() {
    var _a, _b, _c, _d;
    (_a = this.unfocusListActionDestructor) == null ? void 0 : _a.call(this);
    this.unfocusListActionDestructor = void 0;
    (_b = this.selectItemActionDestructor) == null ? void 0 : _b.call(this);
    this.selectItemActionDestructor = void 0;
    (_c = this.prevItemActionDestructor) == null ? void 0 : _c.call(this);
    this.prevItemActionDestructor = void 0;
    (_d = this.nextItemActionDestructor) == null ? void 0 : _d.call(this);
    this.nextItemActionDestructor = void 0;
    this.currentItemIndex.set(-1);
    Coherent.trigger("SET_EFB_GAMEPAD_NAVIGATION", false);
  }
  pause() {
    this.disableGamepadNavigation();
    this.currentItemIndexSubscription.pause();
    this.isListFocusedSubscriptions.forEach((subscription) => {
      subscription.pause();
    });
  }
  resume() {
    this.currentItemIndexSubscription.resume(this.isListFocused.get());
    this.isListFocusedSubscriptions.forEach((subscription) => {
      subscription.resume(true);
    });
  }
  destroy() {
    this.currentItemIndexSubscription.destroy();
    this.isListFocusedSubscriptions.forEach((subscription) => {
      subscription.destroy();
    });
    this.disableGamepadNavigation();
  }
};
_GamepadListNavigationManager.defaultNavigationBetweenItemsDelayMs = 100;
let GamepadListNavigationManager = _GamepadListNavigationManager;
class List extends GamepadUiComponent {
  constructor(props) {
    var _a;
    super(props);
    this.renderedItems = ArraySubject.create();
    this.gamepadComponentChildren = ArraySubject.create();
    this.nbRenderedItems = Subject.create(0);
    this.renderedItemsSubscription = this.renderedItems.sub(
      (_index, _type, _item, items) => {
        this.nbRenderedItems.set(items.length);
        this.gamepadComponentChildren.set(items.filter((item) => item instanceof GamepadUiComponent));
      }
    );
    this.isListVisible = (_a = this.props.isListVisible) != null ? _a : Subject.create(true);
    this.isListVisibleSubscription = this.isListVisible.sub((isVisible) => {
      if (isVisible) {
        this.enableGamepadInputs();
      } else {
        this.disableGamepadInputs();
      }
    }, true);
    this.onDataChangedSubscription = this.props.data.sub(this.onDataChanged.bind(this));
    if (props.gamepadNavigationOptions) {
      this.gamepadNavigationManager = new GamepadListNavigationManager(
        props.data,
        props.gamepadNavigationOptions
      );
    }
  }
  /**
   * Scroll the list to a specific item given its index.
   * @param index The index of the item to scroll to.
   * @throws Error if the index is out of bounds or if the item is not an Element.
   */
  scrollToItem(index2) {
    const listRef = this.gamepadUiComponentRef.getOrDefault();
    if (listRef === null) {
      return;
    }
    let node = this.renderedItems.get(index2);
    if (node instanceof GamepadUiComponent) {
      node = node.gamepadUiComponentRef.getOrDefault();
    }
    if (node instanceof Element) {
      const listRect = listRef.getBoundingClientRect();
      const nodeRect = node.getBoundingClientRect();
      const nodeRelativeTop = nodeRect.top - listRect.top;
      const nodeRelativeBottom = nodeRect.bottom - listRect.bottom;
      if (nodeRelativeTop < 0) {
        listRef.scrollTo({ top: listRef.scrollTop + nodeRelativeTop });
      } else if (nodeRelativeBottom > 0) {
        listRef.scrollTo({ top: listRef.scrollTop + nodeRelativeBottom });
      }
    } else {
      throw new Error("The item to scroll to is not an Element.");
    }
  }
  cleanRenderedContent() {
    for (const item of this.renderedItems.getArray()) {
      if (item instanceof DisplayComponent) {
        item.destroy();
      }
    }
    this.gamepadUiComponentRef.instance.textContent = "";
    this.renderedItems.clear();
  }
  onDataChanged(index2, type, item) {
    const instance = this.gamepadUiComponentRef.getOrDefault();
    if (instance === null) {
      return;
    }
    if (this.props.refreshOnUpdate) {
      this.renderList();
      return;
    }
    switch (type) {
      case SubscribableArrayEventType.Added:
        {
          if (!item) return;
          const el = instance.children.item(index2);
          if (Array.isArray(item)) {
            for (let i = 0; i < item.length; i++) {
              this.addDomNode(item[i], index2 + i, el);
            }
          } else {
            this.addDomNode(item, index2, el);
          }
        }
        break;
      case SubscribableArrayEventType.Removed:
        {
          if (!item) return;
          if (Array.isArray(item)) {
            item.forEach((_) => {
              this.removeDomNode(index2);
            });
          } else {
            this.removeDomNode(index2);
          }
        }
        break;
      case SubscribableArrayEventType.Cleared:
        this.cleanRenderedContent();
        break;
    }
  }
  addDomNode(item, index2, el) {
    const node = this.props.renderItem(item, index2);
    if (!node) return;
    if (el !== null) {
      FSComponent.renderBefore(node, el);
    } else {
      el = this.gamepadUiComponentRef.instance;
      FSComponent.render(node, el);
    }
    if (node.instance !== null) {
      this.renderedItems.insert(node.instance, index2);
    }
  }
  removeDomNode(index2) {
    const child = this.gamepadUiComponentRef.instance.childNodes.item(index2);
    this.gamepadUiComponentRef.instance.removeChild(child);
    const removed = this.renderedItems.get(index2);
    this.renderedItems.removeItem(removed);
    if (removed instanceof DisplayComponent) {
      removed.destroy();
    }
  }
  enableGamepadInputsOnHover(element) {
    if (this.props.isScrollable) {
      this.scrollActionDestructor = this.inputManager.addScrollInputAction(element, {
        scrollAxis: "vertical"
      });
    }
  }
  enableGamepadInputs() {
    var _a;
    super.enableGamepadInputs();
    const scrollContainerRef = this.gamepadUiComponentRef.getOrDefault();
    if (scrollContainerRef !== null) {
      this.enableGamepadInputsOnHover(scrollContainerRef);
    }
    (_a = this.gamepadNavigationManager) == null ? void 0 : _a.resume();
    this.gamepadComponentChildren.getArray().forEach((child) => child.enableGamepadInputs());
  }
  disableGamepadInputs() {
    var _a, _b;
    super.disableGamepadInputs();
    (_a = this.scrollActionDestructor) == null ? void 0 : _a.call(this);
    this.scrollActionDestructor = void 0;
    (_b = this.gamepadNavigationManager) == null ? void 0 : _b.pause();
    this.gamepadComponentChildren.getArray().forEach((child) => child.disableGamepadInputs());
  }
  renderList() {
    const instance = this.gamepadUiComponentRef.getOrDefault();
    if (instance === null) {
      return;
    }
    this.cleanRenderedContent();
    FSComponent.render(
      /* @__PURE__ */ FSComponent.buildComponent(FSComponent.Fragment, null, this.props.data.getArray().map((item, index2) => {
        const vnode = this.props.renderItem(item, index2);
        if (vnode !== null) {
          this.renderedItems.insert(vnode.instance);
        }
        return vnode;
      })),
      instance
    );
  }
  render() {
    var _a;
    return /* @__PURE__ */ FSComponent.buildComponent(
      "div",
      {
        class: mergeClassProp("list", {
          "scroll-container": (_a = this.props.isScrollable) != null ? _a : false,
          hide: MappedSubject.create(
            ([isVisible, nbItems]) => !isVisible || nbItems === 0,
            this.isListVisible,
            this.nbRenderedItems
          )
        }),
        style: this.props.style,
        ref: this.gamepadUiComponentRef
      }
    );
  }
  onAfterRender(node) {
    super.onAfterRender(node);
    this.renderList();
  }
  destroy() {
    var _a;
    this.renderedItemsSubscription.destroy();
    this.onDataChangedSubscription.destroy();
    (_a = this.gamepadNavigationManager) == null ? void 0 : _a.destroy();
    this.isListVisibleSubscription.destroy();
    super.destroy();
  }
}
const arrowIconPath$1 = "coui://html_ui/efb_ui/efb_os/Assets/icons/NoMargin/Arrow.svg";
class PagingList extends GamepadUiComponent {
  constructor() {
    var _a;
    super(...arguments);
    this.currentData = ArraySubject.create();
    this.previousData = ArraySubject.create();
    this.nextData = ArraySubject.create();
    this.firstPage = Subject.create(true);
    this.lastPage = Subject.create(false);
    this.maxItemsPerPage = SubscribableUtils.toSubscribable(this.props.maxItemsPerPage, true);
    this.numberOfPages = Subject.create(1);
    this.pageSelected = (_a = this.props.pageSelected) != null ? _a : Subject.create(0);
    this.previousPageSelected = this.pageSelected.get();
    this.transitionWrapperRef = FSComponent.createRef();
    this.ongoingTransition = Subject.create(false);
    this.allowTransition = true;
    this.subs = [];
  }
  onAfterRender(node) {
    super.onAfterRender(node);
    this.subs.push(
      this.props.data.sub(() => this.updateListsData(this.pageSelected.get())),
      this.maxItemsPerPage.sub(() => this.updateListsData(this.pageSelected.get())),
      this.pageSelected.sub(this.onPageChanged.bind(this))
    );
    this.updateListsData(this.pageSelected.get());
  }
  clearData() {
    this.previousData.clear();
    this.currentData.clear();
    this.nextData.clear();
  }
  updateListsData(pageSelected) {
    const maxItemsPerPage = this.maxItemsPerPage.get();
    if (maxItemsPerPage <= 0) {
      console.error(
        `The maximum items per page of a paging list cannot be negative or null. Current value: ${maxItemsPerPage}`
      );
      return;
    }
    const numberOfPages = Math.ceil(this.props.data.length / maxItemsPerPage);
    this.numberOfPages.set(numberOfPages);
    if (numberOfPages === 0) {
      this.clearData();
      return;
    }
    const rawPageSelected = pageSelected;
    pageSelected = Utils.Clamp(pageSelected, 0, numberOfPages - 1);
    const start = pageSelected * maxItemsPerPage;
    const followingStart = start + maxItemsPerPage;
    const fullData = this.props.data.getArray();
    this.currentData.set(fullData.slice(start, followingStart));
    if (start === 0) {
      this.previousData.clear();
      this.firstPage.set(true);
    } else {
      this.previousData.set(fullData.slice(start - maxItemsPerPage, start));
      this.firstPage.set(this.previousData.getArray().length === 0);
    }
    if (followingStart > fullData.length) {
      this.nextData.clear();
      this.lastPage.set(true);
    } else {
      this.nextData.set(fullData.slice(followingStart, followingStart + maxItemsPerPage));
      this.lastPage.set(this.nextData.getArray().length === 0);
    }
    if (rawPageSelected !== pageSelected) {
      this.allowTransition = false;
      this.pageSelected.set(pageSelected);
    }
  }
  async onPageChanged(newPageSelected) {
    const numberOfPages = this.numberOfPages.get();
    if (newPageSelected < 0 || newPageSelected >= numberOfPages) {
      this.pageSelected.set(Utils.Clamp(newPageSelected, 0, numberOfPages - 1));
      return;
    }
    if (!this.allowTransition) {
      this.allowTransition = true;
      this.previousPageSelected = newPageSelected;
      this.updateListsData(newPageSelected);
      return;
    }
    await Wait.awaitCondition(() => !this.ongoingTransition.get());
    this.ongoingTransition.set(true);
    const gap = newPageSelected - this.previousPageSelected;
    const side = gap > 0 ? 1 : -1;
    for (let i = 0; i < Math.abs(gap); i++) {
      const targetPage = this.previousPageSelected + (i + 1) * side;
      await this.doTransition(side, targetPage);
    }
    this.previousPageSelected = newPageSelected;
    this.ongoingTransition.set(false);
  }
  async doTransition(side, targetPage) {
    for (let i = 5; i < 101; i += 5) {
      this.transitionWrapperRef.instance.style.right = `${100 + i * side}%`;
      await Wait.awaitDelay(1);
    }
    this.updateListsData(targetPage);
    this.transitionWrapperRef.instance.style.right = "100%";
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent(
      "div",
      {
        ref: this.gamepadUiComponentRef,
        class: {
          "paging-list": true,
          "first-page": this.firstPage,
          "last-page": this.lastPage,
          "ongoing-transition": this.ongoingTransition
        }
      },
      /* @__PURE__ */ FSComponent.buildComponent("div", { class: "transition-wrapper", ref: this.transitionWrapperRef }, /* @__PURE__ */ FSComponent.buildComponent(List, { class: "previous-list", data: this.previousData, renderItem: this.props.renderItem }), /* @__PURE__ */ FSComponent.buildComponent(List, { class: "main-list", data: this.currentData, renderItem: this.props.renderItem }), /* @__PURE__ */ FSComponent.buildComponent(List, { class: "following-list", data: this.nextData, renderItem: this.props.renderItem })),
      /* @__PURE__ */ FSComponent.buildComponent(CarrouselButtons, { numberOfPages: this.numberOfPages, pageSelected: this.pageSelected })
    );
  }
  destroy() {
    this.subs.forEach((s) => s.destroy());
    super.destroy();
  }
}
class CarrouselButtons extends GamepadUiComponent {
  constructor() {
    super(...arguments);
    this.buttonsRef = [];
  }
  onAfterRender(node) {
    super.onAfterRender(node);
    this.pagesSub = this.props.numberOfPages.sub(this.renderButtons.bind(this));
  }
  renderButtons(numPages) {
    for (const buttonRef of this.buttonsRef) {
      buttonRef.destroy();
    }
    this.buttonsRef.length = 0;
    this.gamepadUiComponentRef.instance.innerHTML = "";
    for (let i = 0; i < numPages; i++) {
      const node = /* @__PURE__ */ FSComponent.buildComponent(
        UniqueCarrouselButton,
        {
          selected: this.props.pageSelected.map((pageSelected) => pageSelected === i),
          callback: () => {
            this.props.pageSelected.set(i);
          }
        }
      );
      FSComponent.render(node, this.gamepadUiComponentRef.instance);
      this.buttonsRef.push(node.instance);
    }
  }
  changePage(increment) {
    const newPageIndex = Utils.Clamp(
      this.props.pageSelected.get() + increment,
      0,
      this.props.numberOfPages.get() - 1
    );
    this.props.pageSelected.set(newPageIndex);
  }
  destroy() {
    var _a;
    (_a = this.pagesSub) == null ? void 0 : _a.destroy();
    super.destroy();
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent(
      "div",
      {
        class: {
          "carrousel-buttons": true,
          hide: this.props.numberOfPages.map((v) => v <= 1)
        }
      },
      /* @__PURE__ */ FSComponent.buildComponent(
        IconButton,
        {
          class: "left carrousel-arrow",
          callback: () => this.changePage(-1),
          iconPath: arrowIconPath$1
        }
      ),
      /* @__PURE__ */ FSComponent.buildComponent("div", { ref: this.gamepadUiComponentRef, class: "buttons-container" }),
      /* @__PURE__ */ FSComponent.buildComponent(
        IconButton,
        {
          class: "right carrousel-arrow",
          callback: () => this.changePage(1),
          iconPath: arrowIconPath$1
        }
      )
    );
  }
}
class UniqueCarrouselButton extends GamepadUiComponent {
  onAfterRender(node) {
    super.onAfterRender(node);
    this.gamepadUiComponentRef.instance.onclick = this.props.callback;
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent(
      "div",
      {
        ref: this.gamepadUiComponentRef,
        class: { "unique-carrousel-button": true, selected: this.props.selected }
      }
    );
  }
}
class ScrollBar extends DisplayComponent {
  constructor() {
    super(...arguments);
    this.svgRef = FSComponent.createRef();
    this.scrollBarRef = FSComponent.createRef();
    this.scrollThumbRef = FSComponent.createRef();
    this.scrollBarContainerRef = FSComponent.createRef();
    this.scrollableContainer = null;
    this.scrollListener = this.onScroll.bind(this);
    this.sizeChangeTimer = null;
    this.arrowPadding = 6;
    this.margin = 2;
    this.currentScrollHeight = 0;
    this.currentThumbAreaHeight = 0;
    this.scrollHeightRatio = 0;
  }
  onAfterRender(node) {
    super.onAfterRender(node);
    this.scrollableContainer = this.scrollBarContainerRef.instance.previousElementSibling;
    this.scrollableContainer.addEventListener("scroll", this.scrollListener);
    const diffAndAdjust = () => {
      var _a;
      if (this.currentScrollHeight !== ((_a = this.scrollableContainer) == null ? void 0 : _a.scrollHeight)) {
        this.adjustScrollbarDimensions();
      }
    };
    this.sizeChangeTimer = setInterval(diffAndAdjust, 150);
    this.onScroll();
  }
  /**
   * Adjusts the dimensions of the scrollbar elements.
   */
  adjustScrollbarDimensions() {
    if (this.scrollableContainer) {
      const parentTop = this.scrollableContainer.offsetTop;
      this.scrollBarContainerRef.instance.style.top = `${parentTop + 4}px`;
      this.currentScrollHeight = this.scrollableContainer.scrollHeight;
      const containerHeight = this.scrollableContainer.clientHeight;
      const totalMarginAndPadding = this.arrowPadding * 2 + this.margin * 2;
      this.currentThumbAreaHeight = containerHeight - totalMarginAndPadding;
      this.scrollHeightRatio = this.currentScrollHeight / containerHeight;
      this.scrollThumbRef.instance.style.height = `${this.currentThumbAreaHeight / this.scrollHeightRatio}`.toString();
      this.scrollBarContainerRef.instance.style.height = `${containerHeight}px`;
      this.svgRef.instance.setAttribute("height", `${containerHeight - this.margin * 2}px`);
      this.scrollBarRef.instance.style.height = `${containerHeight}px`;
      this.scrollBarContainerRef.instance.style.display = this.scrollHeightRatio <= 1 ? "none" : "";
      this.onScroll();
    }
  }
  /**
   * Eventhandler called when a scroll event in the scrollable parent container happens.
   */
  onScroll() {
    if (this.scrollableContainer) {
      const scrollY = this.scrollableContainer.scrollTop / this.scrollableContainer.scrollHeight * this.currentThumbAreaHeight + this.arrowPadding;
      if (!isNaN(scrollY)) {
        this.scrollThumbRef.instance.setAttribute("y", scrollY.toString());
      }
    }
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent("div", { class: "scroll-bar", ref: this.scrollBarContainerRef }, /* @__PURE__ */ FSComponent.buildComponent("svg", { ref: this.svgRef }, /* @__PURE__ */ FSComponent.buildComponent("rect", { ref: this.scrollBarRef, x: "3", y: "6", width: "4", fill: "#707070" }), /* @__PURE__ */ FSComponent.buildComponent("rect", { ref: this.scrollThumbRef, x: "3", y: "6", width: "4", fill: "#1390E3" })));
  }
  destroy() {
    if (this.scrollableContainer) {
      this.scrollableContainer.removeEventListener("scroll", this.scrollListener);
    }
    if (this.sizeChangeTimer !== null) {
      clearInterval(this.sizeChangeTimer);
    }
  }
}
const arrowIconPath = "coui://html_ui/efb_ui/efb_os/Assets/icons/NoMargin/Arrow.svg";
class DropdownButton extends GamepadUiComponent {
  constructor() {
    super(...arguments);
    this.listDataset = Array.isArray(this.props.listDataset) ? ArraySubject.create(this.props.listDataset) : this.props.listDataset;
    this.isListVisible = Subject.create(false);
  }
  /* Close the list when clicking out of it */
  onClickOutOfComponent() {
    this.isListVisible.set(false);
  }
  onAfterRender(node) {
    var _a;
    super.onAfterRender(node);
    this.isListVisibleSub = (_a = this.props.isListVisible) == null ? void 0 : _a.sub(this.isListVisible.set);
  }
  destroy() {
    var _a;
    (_a = this.isListVisibleSub) == null ? void 0 : _a.destroy();
    super.destroy();
  }
  render() {
    var _a;
    return /* @__PURE__ */ FSComponent.buildComponent("div", { class: "dropdown-button", ref: this.gamepadUiComponentRef }, /* @__PURE__ */ FSComponent.buildComponent(
      DropdownButtonSelector,
      {
        class: "button--full-width",
        key: this.props.title,
        isOpen: this.isListVisible,
        type: this.props.type,
        callback: () => {
          if (this.disabled.get()) {
            return;
          }
          this.isListVisible.set(!this.isListVisible.get());
        },
        customIcon: this.props.customIcon,
        showArrowIcon: this.props.showArrowIcon,
        disabled: this.disabled
      }
    ), /* @__PURE__ */ FSComponent.buildComponent(
      List,
      {
        class: "dropdown-button-list",
        data: this.listDataset,
        isListVisible: this.isListVisible,
        isScrollable: (_a = this.props.isScrollable) != null ? _a : true,
        renderItem: (item, index2) => {
          const disabled = item && typeof item === "object" && "disabled" in item && SubscribableUtils.isSubscribable(item.disabled) ? item.disabled.map((v) => !!v) : void 0;
          return /* @__PURE__ */ FSComponent.buildComponent(
            DropdownButtonListItem,
            {
              disabled,
              name: this.props.getItemLabel(item),
              onClick: () => {
                this.isListVisible.set(false);
                this.props.onItemClick(item, index2);
              }
            }
          );
        }
      }
    ));
  }
}
class DropdownButtonSelector extends DisplayComponent {
  constructor(props) {
    super(props);
    this.iconScale = 1;
    this.iconStyle = ObjectSubject.create({
      transform: `scale(${this.iconScale}, ${this.iconScale})`
    });
    if (this.props.showArrowIcon) {
      this.isOpenSub = this.props.isOpen.sub((isOpen) => {
        this.iconStyle.set(
          "transform",
          `scale(${this.iconScale}, ${isOpen ? -1 * this.iconScale : this.iconScale})`
        );
      }, true);
    }
  }
  destroy() {
    var _a;
    (_a = this.isOpenSub) == null ? void 0 : _a.destroy();
    super.destroy();
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent(
      Button,
      {
        class: mergeClassProp("dropdown-button-selector", this.props.class),
        style: this.props.style,
        callback: this.props.callback,
        hoverable: this.props.hoverable,
        selected: this.props.selected,
        disabled: this.props.disabled
      },
      /* @__PURE__ */ FSComponent.buildComponent(TT, { class: "text bold-text", key: this.props.key }),
      this.props.showArrowIcon && /* @__PURE__ */ FSComponent.buildComponent(
        "icon-element",
        {
          class: "dropdown-button-selector-icon",
          "icon-url": arrowIconPath,
          style: this.iconStyle
        }
      ),
      this.props.customIcon
    );
  }
}
class DropdownButtonListItem extends GamepadUiComponent {
  constructor() {
    var _a;
    super(...arguments);
    this.isButtonDisabled = SubscribableUtils.toSubscribable((_a = this.props.disabled) != null ? _a : false, true);
    this.cssClassSet = SetSubject.create(["text", "regular-text"]);
    this.onClick = () => {
      if (!this.isButtonDisabled.get()) {
        this.props.onClick();
      }
    };
  }
  onAfterRender(node) {
    super.onAfterRender(node);
    this.isButtonDisabled.sub((x) => {
      return x ? this.cssClassSet.add("disabled") : this.cssClassSet.delete("disabled");
    }, true);
    this.gamepadUiComponentRef.instance.onclick = this.onClick;
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent("div", { class: "dropdown-button-list-item", ref: this.gamepadUiComponentRef }, /* @__PURE__ */ FSComponent.buildComponent(TT, { key: this.props.name, type: "div", class: this.cssClassSet }));
  }
}
class IconElement extends DisplayComponent {
  constructor() {
    super(...arguments);
    this.url = SubscribableUtils.toSubscribable(this.props.url, true);
    this.el = FSComponent.createRef();
    this.onSVGIconLoaded = (found, svgAsString) => {
      this.el.instance.innerText = "";
      if (!found) {
        console.error(`Image ${this.url.get()} was not found`);
        return;
      }
      const template = document.createElement("template");
      template.innerHTML = svgAsString;
      const svgAsHtml = template.content.querySelector("svg");
      if (svgAsHtml) {
        this.el.instance.appendChild(svgAsHtml);
      }
    };
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent("div", { ref: this.el, class: mergeClassProp("icon-element", this.props.class) });
  }
  onAfterRender(node) {
    super.onAfterRender(node);
    this.urlSub = this.url.sub((url) => {
      if (url.endsWith(".svg")) {
        getIconCacheMgr().loadURL(url, this.onSVGIconLoaded);
      } else {
        this.el.instance.innerHTML = `<img src="${url}" onerror="console.error('Image ${url} was not found')" />`;
      }
    }, true);
  }
  destroy() {
    var _a;
    (_a = this.urlSub) == null ? void 0 : _a.destroy();
    super.destroy();
  }
}
class IconButton extends DisplayComponent {
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent(
      Button,
      {
        class: mergeClassProp("icon-button", this.props.class),
        style: this.props.style,
        callback: this.props.callback,
        hoverable: this.props.hoverable,
        selected: this.props.selected,
        state: this.props.state,
        disabled: this.props.disabled,
        onboardingStepId: this.props.onboardingStepId
      },
      /* @__PURE__ */ FSComponent.buildComponent(IconElement, { url: this.props.iconPath })
    );
  }
}
class MultipleButtons extends GamepadUiComponent {
  constructor() {
    var _a;
    super(...arguments);
    this.buttonSelected = (_a = this.props.buttonSelected) != null ? _a : 0;
    this.buttonRefs = [...Array(this.props.titles.length)].map(
      () => FSComponent.createRef()
    );
  }
  onAfterRender(node) {
    super.onAfterRender(node);
    if (this.buttonRefs.length) {
      this.buttonRefs[this.buttonSelected].instance.selectButton();
    }
  }
  buttonCallback(buttonIndex) {
    var _a, _b;
    if (this.disabled.get()) {
      return;
    }
    if (!((_b = (_a = this.props).callback) == null ? void 0 : _b.call(_a, buttonIndex))) {
      this.selectButton(buttonIndex);
    }
  }
  selectButton(buttonIndex) {
    if (buttonIndex !== this.buttonSelected) {
      this.unselectAllButtons();
      if (buttonIndex > this.buttonRefs.length || buttonIndex < 0) {
        buttonIndex = 0;
      }
      this.buttonSelected = buttonIndex;
      this.buttonRefs[buttonIndex].instance.selectButton();
    }
  }
  unselectAllButtons() {
    this.buttonRefs.forEach((button) => button.instance.unSelectButton());
  }
  renderSelectableButtons() {
    const buttons = [];
    for (let i = 0; i < this.props.titles.length; i++) {
      buttons.push(
        /* @__PURE__ */ FSComponent.buildComponent(
          SelectableButton$1,
          {
            class: "single-button",
            title: this.props.titles[i],
            ref: this.buttonRefs[i],
            callback: () => this.buttonCallback(i)
          }
        )
      );
    }
    return buttons;
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent("div", { ref: this.gamepadUiComponentRef, class: `multiple-buttons` }, this.renderSelectableButtons());
  }
}
let SelectableButton$1 = class SelectableButton extends DisplayComponent {
  constructor() {
    var _a;
    super(...arguments);
    this.selected = Subject.create((_a = this.props.selected) != null ? _a : false);
  }
  selectButton() {
    this.selected.set(true);
  }
  unSelectButton() {
    this.selected.set(false);
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent(
      Button,
      {
        class: mergeClassProp({ selected: this.selected }, this.props.class),
        style: this.props.style,
        callback: this.props.callback,
        hoverable: this.props.hoverable,
        selected: this.props.selected,
        disabled: this.props.disabled
      },
      /* @__PURE__ */ FSComponent.buildComponent(TT, { class: "title", type: "div", key: this.props.title, format: "uppercase" })
    );
  }
};
class TypedButton extends AbstractButton {
  onAfterRender(node) {
    var _a;
    super.onAfterRender(node);
    this.gamepadUiComponentRef.instance.classList.add((_a = this.props.type) != null ? _a : "primary");
  }
}
class SelectableButton2 extends TypedButton {
  constructor() {
    var _a;
    super(...arguments);
    this.selected = (_a = this.props.selected) != null ? _a : false;
  }
  selectButton() {
    if (!this.selected) {
      this.selected = true;
      this.switchSelection(true);
    }
  }
  unSelectButton() {
    if (this.selected) {
      this.selected = false;
      this.switchSelection(false);
    }
  }
  switchSelection(selected) {
    this.gamepadUiComponentRef.instance.classList.toggle("selected", selected);
  }
  buttonRender() {
    return /* @__PURE__ */ FSComponent.buildComponent("div", { class: "selectable-button" }, /* @__PURE__ */ FSComponent.buildComponent(TT, { class: "title", type: "div", key: this.props.title, format: "upper" }));
  }
}
class TTButton extends DisplayComponent {
  render() {
    var _a;
    return /* @__PURE__ */ FSComponent.buildComponent(
      Button,
      {
        class: mergeClassProp("tt-button", "classic-button", this.props.class),
        style: this.props.style,
        callback: this.props.callback,
        hoverable: this.props.hoverable,
        selected: this.props.selected,
        state: this.props.state,
        disabled: this.props.disabled
      },
      /* @__PURE__ */ FSComponent.buildComponent(
        TT,
        {
          class: "bold-text",
          key: this.props.key,
          type: this.props.type,
          format: (_a = this.props.format) != null ? _a : "uppercase",
          arguments: this.props.arguments
        }
      )
    );
  }
}
class ClassicButton extends Button {
  buttonRender() {
    var _a;
    return /* @__PURE__ */ FSComponent.buildComponent("div", { class: "classic-button" }, /* @__PURE__ */ FSComponent.buildComponent(TT, { key: this.props.title, format: (_a = this.props.format) != null ? _a : "upper", type: "div", class: "bold-text" }));
  }
}
class TabSelector extends GamepadUiComponent {
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent(
      "div",
      {
        ref: this.gamepadUiComponentRef,
        class: {
          "tab-selector": true,
          active: this.props.active,
          hidden: this.props.hidden
        }
      },
      /* @__PURE__ */ FSComponent.buildComponent("div", { class: "disabled-layer" }),
      /* @__PURE__ */ FSComponent.buildComponent(TT, { key: this.props.tabName, class: "text", format: "ucfirst" }),
      /* @__PURE__ */ FSComponent.buildComponent("div", { class: "state" })
    );
  }
  onAfterRender(node) {
    super.onAfterRender(node);
    if (this.props.callback) {
      this.gamepadUiComponentRef.instance.onclick = (e) => {
        var _a;
        if (SubscribableUtils.toSubscribable(this.props.disabled, true).get() === true) {
          return;
        }
        (_a = this.props.callback) == null ? void 0 : _a.call(e);
      };
    }
  }
}
class AccordionButton extends DisplayComponent {
  constructor() {
    super(...arguments);
    this.iconStyle = Subject.create("rotate(0)");
    this.isFoldedSubscription = this.props.isFolded.sub((isFolded) => {
      this.iconStyle.set(`rotate(${isFolded ? 0 : -0.5}turn)`);
    }, true);
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent(
      Button,
      {
        class: mergeClassProp("accordion-button", this.props.class),
        style: this.props.style,
        callback: this.props.callback,
        hoverable: this.props.hoverable,
        selected: this.props.selected,
        state: this.props.state,
        disabled: this.props.disabled
      },
      /* @__PURE__ */ FSComponent.buildComponent("div", { class: "icon-container", style: { transform: this.iconStyle } }, /* @__PURE__ */ FSComponent.buildComponent(
        IconElement,
        {
          class: "accordion-button-icon",
          url: `coui://html_ui/efb_ui/efb_os/Assets/icons/Arrow.svg`
        }
      )),
      /* @__PURE__ */ FSComponent.buildComponent("div", { class: "title" }, this.props.children)
    );
  }
  destroy() {
    this.isFoldedSubscription.destroy();
    super.destroy();
  }
}
class AbstractAccordion extends GamepadUiComponent {
  constructor() {
    var _a;
    super(...arguments);
    this.isFolded = SubscribableUtils.isMutableSubscribable(this.props.isFolded) ? this.props.isFolded : Subject.create((_a = this.props.isFolded) != null ? _a : true);
  }
  renderBody() {
    return /* @__PURE__ */ FSComponent.buildComponent(FSComponent.Fragment, null, this.props.children);
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent("div", { class: { accordion: true, folded: this.isFolded }, ref: this.gamepadUiComponentRef }, /* @__PURE__ */ FSComponent.buildComponent(
      AccordionButton,
      {
        isFolded: this.isFolded,
        disabled: this.props.disabled,
        class: "accordion-button abstract-button--full-width",
        callback: () => {
          this.isFolded.set(!this.isFolded.get());
        }
      },
      this.renderHeader()
    ), /* @__PURE__ */ FSComponent.buildComponent("div", { class: { "menu-content": true, hide: this.isFolded } }, this.renderBody()));
  }
}
class ElementAccordion extends AbstractAccordion {
  renderHeader() {
    return /* @__PURE__ */ FSComponent.buildComponent(FSComponent.Fragment, null, value(this.props.header));
  }
}
class StringAccordion extends AbstractAccordion {
  renderHeader() {
    return /* @__PURE__ */ FSComponent.buildComponent(TT, { key: this.props.title, format: "ucfirst" });
  }
}
const _BearingDisplay = class _BearingDisplay extends AbstractNumberUnitDisplay {
  constructor() {
    var _a;
    super(...arguments);
    this.unitFormatter = (_a = this.props.unitFormatter) != null ? _a : _BearingDisplay.DEFAULT_UNIT_FORMATTER;
    this.unitTextBigDisplay = Subject.create("");
    this.unitTextSmallDisplay = Subject.create("");
    this.numberText = Subject.create("");
    this.unitTextBig = Subject.create("");
    this.unitTextSmall = Subject.create("");
  }
  /** @inheritdoc */
  onValueChanged(value2) {
    let displayUnit = this.displayUnit.get();
    if (!displayUnit || !value2.unit.canConvert(displayUnit)) {
      displayUnit = value2.unit;
    }
    const numberValue = value2.asUnit(displayUnit);
    this.updateNumberText(numberValue);
    this.updateUnitText(numberValue, displayUnit);
    if (this.props.hideDegreeSymbolWhenNan === true) {
      this.updateUnitTextVisibility(numberValue);
    }
  }
  /** @inheritdoc */
  onDisplayUnitChanged(displayUnit) {
    const value2 = this.value.get();
    if (!displayUnit || !value2.unit.canConvert(displayUnit)) {
      displayUnit = value2.unit;
    }
    const numberValue = value2.asUnit(displayUnit);
    this.updateNumberText(numberValue);
    this.updateUnitText(numberValue, displayUnit);
    this.updateUnitTextVisibility(numberValue);
  }
  /**
   * Updates this component's displayed number text.
   * @param numberValue The numeric value to display.
   */
  updateNumberText(numberValue) {
    let numberText = this.props.formatter(numberValue);
    if (this.props.use360 !== false && parseFloat(numberText) === 0) {
      numberText = this.props.formatter(360);
    }
    this.numberText.set(numberText);
  }
  /**
   * Updates this component's displayed unit text.
   * @param numberValue The numeric value to display.
   * @param displayUnit The unit type in which to display the value.
   */
  updateUnitText(numberValue, displayUnit) {
    _BearingDisplay.unitTextCache[0] = "";
    _BearingDisplay.unitTextCache[1] = "";
    this.unitFormatter(_BearingDisplay.unitTextCache, displayUnit, numberValue);
    this.unitTextBig.set(_BearingDisplay.unitTextCache[0]);
    this.unitTextSmall.set(_BearingDisplay.unitTextCache[1]);
  }
  /**
   * Updates whether this component's unit text spans are visible.
   * @param numberValue The numeric value displayed by this component.
   */
  updateUnitTextVisibility(numberValue) {
    if (this.props.hideDegreeSymbolWhenNan === true) {
      if (isNaN(numberValue)) {
        this.unitTextBigDisplay.set("none");
        this.unitTextSmallDisplay.set("none");
        return;
      }
    }
    this.unitTextBigDisplay.set(this.unitTextBig.get() === "" ? "none" : "");
    this.unitTextSmallDisplay.set(this.unitTextSmall.get() === "" ? "none" : "");
  }
  /** @inheritdoc */
  render() {
    var _a;
    return /* @__PURE__ */ FSComponent.buildComponent("div", { class: (_a = this.props.class) != null ? _a : "", style: "white-space: nowrap;" }, /* @__PURE__ */ FSComponent.buildComponent("span", { class: "bearing-num" }, this.numberText), /* @__PURE__ */ FSComponent.buildComponent("span", { class: "bearing-unit", style: { display: this.unitTextBigDisplay } }, this.unitTextBig), /* @__PURE__ */ FSComponent.buildComponent("span", { class: "bearing-unit-small", style: { display: this.unitTextSmallDisplay } }, this.unitTextSmall));
  }
};
_BearingDisplay.DEFAULT_UNIT_FORMATTER = (out, unit) => {
  out[0] = "°";
  out[1] = unit.isMagnetic() ? "" : "T";
};
_BearingDisplay.unitTextCache = ["", ""];
let BearingDisplay = _BearingDisplay;
const INTERNAL_KEY_SEPARATOR = "__MSFS2024_INTERNAL_DATE_ANA_ACE__";
const FormatKeys = ["YYYY", "YY", "MMMM", "MMM", "MM", "DDDD", "DD", "hh", "mm", "ss"];
const dateToValue = {
  YYYY: (date) => date.getFullYear().toString(),
  YY: (date) => (date.getFullYear() % 100).toString().padStart(2, "0"),
  MMMM: (date) => monthKeys[date.getMonth()],
  MMM: (date) => monthShortKeys[date.getMonth()],
  MM: (date) => (date.getMonth() + 1).toString().padStart(2, "0"),
  DDDD: (date) => dayKeys[date.getDay()],
  DD: (date) => date.getDate().toString().padStart(2, "0"),
  hh: (date) => date.getHours().toString().padStart(2, "0"),
  mm: (date) => date.getMinutes().toString().padStart(2, "0"),
  ss: (date) => date.getSeconds().toString().padStart(2, "0")
};
const formatToInternalKey = {
  YYYY: "FULLYEAR",
  YY: "SHORTYEAR",
  MMMM: "FULLMONTH",
  MMM: "SHORTMONTH",
  MM: "DIGITMONTH",
  DDDD: "FULLDAY",
  DD: "DIGITDAY",
  hh: "HOURS",
  mm: "MINUTES",
  ss: "SECONDS"
};
class DateDisplay extends DisplayComponent {
  constructor() {
    super(...arguments);
    this.ready = Subject.create(false);
    this.date = SubscribableUtils.toSubscribable(this.props.date, true);
    this.format = SubscribableUtils.toSubscribable(this.props.format, true);
    this.dateSubscription = this.date.sub((date) => this.updateDate(date));
    this.formatSubscription = this.format.sub((format) => {
      this.renderFormat(this.date.get(), format);
    });
    this.dateSpanRef = FSComponent.createRef();
    this.dateNodes = [];
    this.cachedSubject = {
      YYYY: Subject.create(""),
      YY: Subject.create(""),
      MMMM: Subject.create(""),
      MMM: Subject.create(""),
      MM: Subject.create(""),
      DDDD: Subject.create(""),
      DD: Subject.create(""),
      hh: Subject.create(""),
      mm: Subject.create(""),
      ss: Subject.create("")
    };
  }
  renderFormat(rawDate, format) {
    if (!this.ready.get()) {
      return;
    }
    this.dateNodes.forEach((node) => FSComponent.shallowDestroy(node));
    this.dateSpanRef.instance.innerHTML = "";
    this.updateDate(rawDate);
    let editedFormat = format;
    for (const key in formatToInternalKey) {
      editedFormat = editedFormat.replace(
        key,
        `${INTERNAL_KEY_SEPARATOR}${formatToInternalKey[key]}${INTERNAL_KEY_SEPARATOR}`
      );
    }
    const splittedFormat = editedFormat.split(INTERNAL_KEY_SEPARATOR);
    for (const part of splittedFormat) {
      const foundKey = FormatKeys.find((key) => formatToInternalKey[key] === part);
      if (foundKey !== void 0) {
        this.dateNodes.push(/* @__PURE__ */ FSComponent.buildComponent(TT, { key: this.cachedSubject[foundKey] }));
      } else {
        this.dateNodes.push(/* @__PURE__ */ FSComponent.buildComponent("span", null, part));
      }
    }
    FSComponent.render(/* @__PURE__ */ FSComponent.buildComponent(FSComponent.Fragment, null, this.dateNodes), this.dateSpanRef.instance);
  }
  updateDate(rawDate) {
    if (!this.ready.get()) {
      return;
    }
    const date = new Date(rawDate);
    for (const key in this.cachedSubject) {
      this.cachedSubject[key].set(dateToValue[key](date));
    }
  }
  onAfterRender(node) {
    super.onAfterRender(node);
    this.ready.set(true);
    this.renderFormat(this.date.get(), this.format.get());
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent("span", { style: this.props.style, class: toClassProp(this.props.class), ref: this.dateSpanRef });
  }
  destroy() {
    this.dateSubscription.destroy();
    this.formatSubscription.destroy();
    super.destroy();
  }
}
class RunwaySelector extends GamepadUiComponent {
  constructor() {
    super(...arguments);
    this.runways = ArraySubject.create();
    this.currentRunwayName = MappedSubject.create(
      ([facility, currentRunway]) => {
        const runways = facility && FacilityUtils.isFacilityType(facility, FacilityType.Airport) ? RunwayUtils.getOneWayRunwaysFromAirport(facility) : [];
        this.runways.set(runways);
        const runwayIndex = typeof currentRunway === "number" ? currentRunway : runways.findIndex((runway) => runway.designation === currentRunway.designation);
        return runwayIndex >= 0 && runwayIndex < runways.length ? getRunwayName(runways[runwayIndex], this.props.runwayNameShortened) : "";
      },
      this.props.facility,
      this.props.currentRunway
    );
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent(
      "div",
      {
        class: {
          "runway-selector": true,
          hide: this.currentRunwayName.map((name) => !name)
        },
        ref: this.gamepadUiComponentRef
      },
      /* @__PURE__ */ FSComponent.buildComponent(
        DropdownButton,
        {
          title: this.currentRunwayName,
          type: "secondary",
          listDataset: this.runways,
          getItemLabel: (item) => getRunwayName(item, this.props.runwayNameShortened),
          disabled: this.props.disabled,
          onItemClick: (runway, index2) => {
            this.props.onRunwaySelected(runway, index2);
          },
          showArrowIcon: true
        }
      )
    );
  }
  /** @inheritdoc */
  destroy() {
    this.currentRunwayName.destroy();
    super.destroy();
  }
}
class Input extends GamepadUiComponent {
  constructor() {
    var _a, _b;
    super(...arguments);
    this.uuid = UUID.GenerateUuid();
    this.inputRef = this.gamepadUiComponentRef;
    this.model = this.props.model || Subject.create(SubscribableUtils.toSubscribable(this.props.value || "", true).get());
    this.internalInputManager = new InternalInputManager(InternalInputActions);
    this.dispatchFocusOutEvent = this._dispatchFocusOutEvent.bind(this);
    this.setValueFromOS = this._setValueFromOS.bind(this);
    this.onKeyDown = this._onKeyDown.bind(this);
    this.onKeyUp = this._onKeyUp.bind(this);
    this.onKeyPress = this._onKeyPress.bind(this);
    this.onInput = this._onInput.bind(this);
    this.align = this.props.align || "left";
    this.debounce = new DebounceTimer();
    this.reloadLocalisation = this._reloadLocalisation.bind(this);
    this._isFocused = Subject.create(false);
    this.isFocused = this._isFocused;
    this.focusSubscription = this._isFocused.sub((focus) => {
      if (focus) {
        this.onFocusIn();
      } else {
        this.onFocusOut();
      }
    });
    this.placeholderKey = SubscribableUtils.toSubscribable((_a = this.props.placeholder) != null ? _a : "", true);
    this.placeholderShown = Subject.create(true);
    this.placeholderTranslation = Subject.create(this.placeholderKey.get());
    this.hidePlaceholderOnFocus = (_b = this.props.hidePlaceholderOnFocus) != null ? _b : false;
    this.subs = [];
  }
  _reloadLocalisation() {
    this.placeholderTranslation.notify();
  }
  _onKeyDown(event) {
    var _a, _b;
    (_b = (_a = this.props).onKeyDown) == null ? void 0 : _b.call(_a, event);
  }
  _onKeyUp(event) {
    var _a, _b;
    (_b = (_a = this.props).onKeyUp) == null ? void 0 : _b.call(_a, event);
  }
  _onKeyPress(event) {
    var _a, _b;
    const keyCode = event.keyCode || event.which;
    (_b = (_a = this.props).onKeyPress) == null ? void 0 : _b.call(_a, event);
    if (event.defaultPrevented) {
      return;
    }
    if (this.props.charFilter && !this.props.charFilter(String.fromCharCode(keyCode))) {
      event.preventDefault();
      return;
    }
  }
  _onInput() {
    var _a;
    this.debounce.schedule(() => {
      const value2 = this.inputRef.instance.value;
      if (value2 === this.model.get()) {
        return;
      }
      this.model.set(value2);
    }, (_a = this.props.debounceDuration) != null ? _a : 0);
  }
  onInputUpdated(value2) {
    var _a, _b;
    this.inputRef.instance.value = value2;
    (_b = (_a = this.props).onInput) == null ? void 0 : _b.call(_a, this.inputRef.instance);
    if (!this.hidePlaceholderOnFocus && value2.length === 0) {
      this.placeholderShown.set(true);
    }
  }
  onFocusIn() {
    var _a, _b;
    Coherent.trigger("FOCUS_INPUT_FIELD", {
      // TODO Allow the following parameters to be customized via props
      title: "Title",
      description: "",
      default_text: "",
      is_numeric: this.props.type === "number",
      is_efb: true
    });
    Coherent.on("mousePressOutsideView", this.dispatchFocusOutEvent);
    Coherent.on("SetInputTextFromOS", this.setValueFromOS);
    this.focusOutActionDestructor = this.internalInputManager.addInputAction(
      InternalInputAction.BACK,
      () => {
        this.dispatchFocusOutEvent();
        return true;
      }
    );
    this.validateInputActionDestructor = this.internalInputManager.addInputAction(
      InternalInputAction.TEXTBOX_VALID,
      () => {
        var _a2, _b2;
        (_b2 = (_a2 = this.props).onValueValidated) == null ? void 0 : _b2.call(_a2, this.inputRef.instance.value);
        this.dispatchFocusOutEvent();
        return true;
      }
    );
    if (this.hidePlaceholderOnFocus && this.inputRef.instance.value.length === 0) {
      this.placeholderShown.set(false);
    }
    (_b = (_a = this.props).onFocusIn) == null ? void 0 : _b.call(_a);
  }
  onFocusOut() {
    var _a, _b, _c, _d;
    (_a = this.focusOutActionDestructor) == null ? void 0 : _a.call(this);
    (_b = this.validateInputActionDestructor) == null ? void 0 : _b.call(this);
    Coherent.trigger("UNFOCUS_INPUT_FIELD", this.uuid);
    Coherent.off("mousePressOutsideView", this.dispatchFocusOutEvent);
    Coherent.off("SetInputTextFromOS", this.setValueFromOS);
    if (this.hidePlaceholderOnFocus && this.inputRef.instance.value.length === 0) {
      this.placeholderShown.set(true);
    }
    (_d = (_c = this.props).onFocusOut) == null ? void 0 : _d.call(_c);
  }
  value() {
    return this.model.get();
  }
  clearInput() {
    this.model.set("");
  }
  focus() {
    const inputInstance = this.inputRef.getOrDefault();
    if (inputInstance !== null) {
      inputInstance.focus();
    }
  }
  blur() {
    const inputInstance = this.inputRef.getOrDefault();
    if (inputInstance !== null) {
      inputInstance.blur();
    }
  }
  _dispatchFocusOutEvent() {
    const inputInstance = this.inputRef.getOrDefault();
    if (inputInstance !== null) {
      inputInstance.blur();
      inputInstance.dispatchEvent(new InputEvent("focusout"));
    }
  }
  _setValueFromOS(text) {
    var _a, _b;
    const inputInstance = this.inputRef.getOrDefault();
    if (inputInstance !== null) {
      inputInstance.value = text;
      inputInstance.dispatchEvent(new InputEvent("change"));
      inputInstance.dispatchEvent(new InputEvent("input"));
      inputInstance.dispatchEvent(new InputEvent("submit"));
      (_b = (_a = this.props).onValueValidated) == null ? void 0 : _b.call(_a, text);
      this.dispatchFocusOutEvent();
    }
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent(
      "input",
      {
        id: this.uuid,
        ref: this.inputRef,
        type: this.props.type,
        class: {
          [`align-${this.align}`]: this.align !== "left"
        },
        placeholder: MappedSubject.create(
          ([placeholderShown, placeholderKey]) => {
            return placeholderShown ? Utils.Translate(placeholderKey) : "";
          },
          this.placeholderShown,
          this.placeholderKey
        ),
        disabled: this.props.disabled,
        value: SubscribableUtils.toSubscribable(this.props.model || this.props.value || "", true).get()
      }
    );
  }
  onAfterRender(node) {
    super.onAfterRender(node);
    this.subs.push(
      this.model.sub((value2) => {
        this.onInputUpdated(value2);
      }, true),
      this.placeholderKey.sub((key) => {
        this.placeholderTranslation.set(key);
      }, true)
    );
    this.inputRef.instance.addEventListener("focus", () => this._isFocused.set(true));
    this.inputRef.instance.addEventListener("focusout", () => this._isFocused.set(false));
    this.inputRef.instance.addEventListener("input", this.onInput);
    this.inputRef.instance.addEventListener("keydown", this.onKeyDown);
    this.inputRef.instance.addEventListener("keyup", this.onKeyUp);
    this.inputRef.instance.addEventListener("keypress", this.onKeyPress);
    Coherent.on("RELOAD_LOCALISATION", this.reloadLocalisation);
    if (this.props.focusOnInit) {
      this.focus();
    }
  }
  destroy() {
    var _a;
    this.subs.forEach((s) => s.destroy());
    this.focusSubscription.destroy();
    (_a = this.focusOutActionDestructor) == null ? void 0 : _a.call(this);
    if (this._isFocused.get()) {
      Coherent.trigger("UNFOCUS_INPUT_FIELD", this.uuid);
      Coherent.off("mousePressOutsideView", this.dispatchFocusOutEvent);
      Coherent.off("SetInputTextFromOS", this.setValueFromOS);
    }
    this.inputRef.instance.removeEventListener("keydown", this.onKeyDown);
    this.inputRef.instance.removeEventListener("keyup", this.onKeyUp);
    this.inputRef.instance.removeEventListener("keypress", this.onKeyPress);
    this.inputRef.instance.removeEventListener("input", this.onInput);
    Coherent.off("RELOAD_LOCALISATION", this.reloadLocalisation);
    super.destroy();
  }
}
const addIconPath = "coui://html_ui/efb_ui/efb_os/Assets/icons/NoMargin/Close.svg";
class TextBox extends GamepadUiComponent {
  constructor() {
    var _a, _b;
    super(...arguments);
    this.subscriptions = [];
    this.inputRef = FSComponent.createRef();
    this.model = this.props.model || Subject.create(SubscribableUtils.toSubscribable(this.props.value || "", true).get());
    this.hideDeleteTextButton = Subject.create(true);
    this.onDelete = this._onDelete.bind(this);
    this.onmousedown = (e) => {
      e.preventDefault();
      if (!this.inputRef.instance.isFocused.get()) {
        this.inputRef.instance.focus();
      }
    };
    this.prefix = SubscribableUtils.toSubscribable(
      this.props.prefix || "",
      true
    );
    this.suffix = SubscribableUtils.toSubscribable(
      this.props.suffix || "",
      true
    );
    this.showPrefix = SubscribableUtils.toSubscribable((_a = this.props.showPrefix) != null ? _a : true, true);
    this.showSuffix = SubscribableUtils.toSubscribable((_b = this.props.showSuffix) != null ? _b : true, true);
    this.prefixRef = FSComponent.createRef();
    this.suffixRef = FSComponent.createRef();
    this.showPrefixOrSuffixMapFunction = ([value2, show]) => {
      return !show || value2.toString().length === 0;
    };
  }
  _onDelete(event) {
    var _a, _b;
    event.preventDefault();
    this.hideDeleteTextButton.set(true);
    this.inputRef.instance.clearInput();
    (_b = (_a = this.props).onDelete) == null ? void 0 : _b.call(_a);
    if (this.props.focusOnClear) {
      this.inputRef.instance.focus();
    }
  }
  onInput(input) {
    var _a, _b;
    (_b = (_a = this.props).onInput) == null ? void 0 : _b.call(_a, input);
    if (this.props.showDeleteIcon) {
      this.hideDeleteTextButton.set(input.value.trim().length === 0);
    }
  }
  onFocusIn() {
    var _a, _b;
    (_b = (_a = this.props).onFocusIn) == null ? void 0 : _b.call(_a);
    this.gamepadUiComponentRef.instance.classList.add("textbox--focused");
  }
  onFocusOut() {
    var _a, _b;
    (_b = (_a = this.props).onFocusOut) == null ? void 0 : _b.call(_a);
    this.gamepadUiComponentRef.instance.classList.remove("textbox--focused");
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent("div", { ref: this.gamepadUiComponentRef, class: "textbox" }, /* @__PURE__ */ FSComponent.buildComponent("div", { class: "disabled-layer" }), /* @__PURE__ */ FSComponent.buildComponent(
      "div",
      {
        ref: this.prefixRef,
        class: {
          prefix: true,
          "prefix--hidden": MappedSubject.create(this.prefix, this.showPrefix).map(
            this.showPrefixOrSuffixMapFunction
          )
        }
      }
    ), /* @__PURE__ */ FSComponent.buildComponent(
      Input,
      {
        ref: this.inputRef,
        type: "text",
        align: this.props.align,
        model: this.model,
        placeholder: this.props.placeholder,
        disabled: this.props.disabled,
        hidePlaceholderOnFocus: this.props.hidePlaceholderOnFocus,
        focusOnInit: this.props.focusOnInit,
        onFocusIn: this.onFocusIn.bind(this),
        onFocusOut: this.onFocusOut.bind(this),
        onInput: this.onInput.bind(this),
        onValueValidated: this.props.onValueValidated,
        debounceDuration: this.props.debounceDuration,
        onKeyDown: this.props.onKeyDown,
        onKeyUp: this.props.onKeyUp,
        onKeyPress: this.props.onKeyPress,
        charFilter: this.props.charFilter
      }
    ), /* @__PURE__ */ FSComponent.buildComponent(
      "div",
      {
        ref: this.suffixRef,
        class: {
          suffix: true,
          "suffix--hidden": MappedSubject.create(this.suffix, this.showSuffix).map(
            this.showPrefixOrSuffixMapFunction
          )
        }
      }
    ), this.props.showDeleteIcon && /* @__PURE__ */ FSComponent.buildComponent(
      IconButton,
      {
        class: mergeClassProp("delete-text-button", { hide: this.hideDeleteTextButton }),
        iconPath: addIconPath,
        callback: this.onDelete
      }
    ));
  }
  onAfterRender(node) {
    super.onAfterRender(node);
    const prefixSuffixCb = (reference) => {
      let previousNode = null;
      return (value2) => {
        if (previousNode) {
          FSComponent.shallowDestroy(previousNode);
          const instance = previousNode.instance;
          if (instance && instance instanceof DisplayComponent) {
            instance.destroy();
          }
          previousNode = null;
        }
        if (typeof value2 === "string") {
          reference.instance.innerText = value2;
        } else if (typeof value2 === "object" && isVNode(value2)) {
          previousNode = value2;
          reference.instance.innerHTML = "";
          FSComponent.render(value2, reference.instance);
        } else {
          reference.instance.innerText = value2;
        }
      };
    };
    this.subscriptions.push(
      this.prefix.sub(prefixSuffixCb(this.prefixRef), true),
      this.suffix.sub(prefixSuffixCb(this.suffixRef), true)
    );
    this.gamepadUiComponentRef.instance.addEventListener("mousedown", this.onmousedown);
  }
  destroy() {
    this.gamepadUiComponentRef.instance.removeEventListener("mousedown", this.onmousedown);
    this.subscriptions.forEach((s) => s.destroy());
    super.destroy();
  }
}
const _UnitBox = class _UnitBox extends GamepadUiComponent {
  constructor() {
    var _a, _b;
    super(...arguments);
    this.valueNumber = this.props.value;
    this.unit = SubscribableUtils.toSubscribable((_a = this.props.unit) != null ? _a : null, true);
    this.showUnitSuffix = SubscribableUtils.toSubscribable((_b = this.props.showSuffix) != null ? _b : true, true);
    this.textValue = Subject.create("0");
    this.formattedTextValue = Subject.create("0");
    this.subs = [];
    this.canUpdateDisplay = true;
    this.suffixSmall = Subject.create("");
    this.suffixBig = Subject.create("");
  }
  static createDefaultUnitTextMap() {
    const originalMap = UnitFormatter.getUnitTextMap();
    const map = {};
    for (const family in originalMap) {
      const nameMap = map[family] = {};
      const originalNameMap = originalMap[family];
      for (const name in originalNameMap) {
        const text = nameMap[name] = ["", ""];
        const originalText = originalNameMap[name];
        if (originalText[0].startsWith("°")) {
          text[0] = "°";
          text[1] = originalText.substring(1);
        } else {
          text[1] = originalText;
        }
      }
    }
    return map;
  }
  setValue(value2, displayUnit) {
    var _a;
    if (!displayUnit || !value2.unit.canConvert(displayUnit)) {
      displayUnit = value2.unit;
    }
    let numberValue = value2.asUnit(displayUnit);
    if (this.props.min && numberValue < this.props.min.asUnit(displayUnit)) {
      numberValue = this.props.min.asUnit(displayUnit);
    }
    if (this.props.max && numberValue > this.props.max.asUnit(displayUnit)) {
      numberValue = this.props.max.asUnit(displayUnit);
    }
    if (numberValue >= 9999999) {
      this.textValue.set("9999999");
      return;
    }
    const numberText = NumberFormatter.create(__spreadValues({
      nanString: "0"
    }, (_a = this.props.numberFormatterOptions) != null ? _a : {}))(numberValue);
    if (numberText.includes(".")) {
      const dotPos = numberText.indexOf(".");
      this.textValue.set(numberText.substring(0, dotPos + 3));
    } else {
      this.textValue.set(this.textValue.get().includes(".") ? `${numberText}.` : numberText);
    }
    const unitTexts = _UnitBox.DEFAULT_UNIT_FORMATTER(displayUnit);
    if (unitTexts === null) {
      this.suffixSmall.set("");
      this.suffixBig.set("");
      return;
    }
    this.suffixSmall.set(unitTexts[0]);
    this.suffixBig.set(unitTexts[1]);
  }
  updateDisplay() {
    var _a, _b;
    (_b = (_a = this.props).onFocusOut) == null ? void 0 : _b.call(_a);
    if (this.canUpdateDisplay) {
      this.formattedTextValue.set(this.textValue.get());
    }
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent(
      "div",
      {
        ref: this.gamepadUiComponentRef,
        class: { unitbox: true, "unitbox--disabled": this.props.disabled || false }
      },
      /* @__PURE__ */ FSComponent.buildComponent(
        TextBox,
        {
          align: "right",
          showDeleteIcon: this.props.showDeleteIcon || false,
          customDeleteIcon: this.props.customDeleteIcon,
          focusOnClear: this.props.focusOnClear,
          onDelete: this.props.onDelete,
          model: this.formattedTextValue,
          placeholder: this.props.placeholder,
          hidePlaceholderOnFocus: this.props.hidePlaceholderOnFocus,
          disabled: this.props.disabled,
          focusOnInit: this.props.focusOnInit,
          onFocusIn: this.props.onFocusIn,
          onFocusOut: this.updateDisplay.bind(this),
          onInput: this.props.onInput,
          onKeyPress: this.props.onKeyPress,
          charFilter: (char) => char >= "0" && char <= "9" || [",", ".", "-"].includes(char),
          suffix: /* @__PURE__ */ FSComponent.buildComponent(FSComponent.Fragment, null, /* @__PURE__ */ FSComponent.buildComponent("span", null, this.suffixSmall), /* @__PURE__ */ FSComponent.buildComponent("span", null, this.suffixBig)),
          showSuffix: this.showUnitSuffix
        }
      )
    );
  }
  onAfterRender(node) {
    super.onAfterRender(node);
    this.subs.push(
      this.formattedTextValue.sub((value2) => {
        value2 = value2.replace(",", ".").replace(/\.+$/, "");
        let displayUnit = this.unit.get();
        if (!displayUnit || !this.valueNumber.get().unit.canConvert(displayUnit)) {
          displayUnit = this.valueNumber.get().unit;
        }
        const unitValue = displayUnit.convertTo(Number(value2), this.valueNumber.get().unit);
        this.canUpdateDisplay = false;
        this.valueNumber.set(unitValue);
        this.canUpdateDisplay = true;
      }),
      this.valueNumber.sub((value2) => {
        this.setValue(value2, this.unit.get());
        this.updateDisplay();
      }, true),
      this.unit.sub((unit) => {
        this.setValue(this.valueNumber.get(), unit);
        this.updateDisplay();
      }, true)
    );
  }
  destroy() {
    this.subs.forEach((s) => s.destroy());
    super.destroy();
  }
};
_UnitBox.DEFAULT_UNIT_TEXT_MAP = _UnitBox.createDefaultUnitTextMap();
_UnitBox.DEFAULT_UNIT_FORMATTER = (unit) => {
  var _a;
  const text = (_a = _UnitBox.DEFAULT_UNIT_TEXT_MAP[unit.family]) == null ? void 0 : _a[unit.name];
  if (text) {
    return text;
  }
  return null;
};
let UnitBox = _UnitBox;
class UnitsBox extends GamepadUiComponent {
  constructor() {
    super(...arguments);
    this.inputSub = Subject.create("");
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent("div", { class: "units-box-container", ref: this.gamepadUiComponentRef }, /* @__PURE__ */ FSComponent.buildComponent(
      Input,
      {
        type: "text",
        model: this.inputSub,
        class: "units-box light-text",
        charFilter: (char) => char >= "0" && char <= "9",
        onFocusOut: () => {
          var _a;
          let value2 = parseInt(this.inputSub.get());
          if (Number.isNaN(value2) || value2 < 0) {
            value2 = (_a = this.props.defaultValue) != null ? _a : 0;
          }
          this.props.valueSub.set(value2);
        }
      }
    ), /* @__PURE__ */ FSComponent.buildComponent("span", { class: "suffix light-text" }, "ft"));
  }
}
class Incremental extends GamepadUiComponent {
  constructor(props) {
    var _a, _b, _c, _d, _e;
    super(props);
    this.min = SubscribableUtils.toSubscribable((_a = this.props.min) != null ? _a : 0, true);
    this.max = SubscribableUtils.toSubscribable((_b = this.props.max) != null ? _b : Number.MAX_SAFE_INTEGER, true);
    this.step = SubscribableUtils.toSubscribable((_c = this.props.step) != null ? _c : 1, true);
    this.formatter = (_d = this.props.formatter) != null ? _d : NumberFormatter.create();
    this.useTextbox = (_e = this.props.useTextbox) != null ? _e : true;
    this.displayedValue = Subject.create("");
    if (SubscribableUtils.isMutableSubscribable(props.value)) {
      this.value = props.value;
    } else if (SubscribableUtils.isSubscribable(props.value)) {
      this.value = Subject.create(props.value.get());
      this.subscribableValueSubscription = props.value.sub((value2) => this.value.set(value2));
    } else {
      this.value = Subject.create(props.value);
    }
    this.valueSubscription = this.value.sub((newValue) => {
      var _a2, _b2;
      (_b2 = (_a2 = this.props).onChange) == null ? void 0 : _b2.call(_a2, newValue);
    });
    this.displayedValueSubscription = MappedSubject.create(
      ([value2, max]) => {
        this.displayedValue.set(this.formatter(value2, max));
      },
      this.value,
      this.max
    );
  }
  renderButton(icon, direction = -1) {
    if (icon !== void 0 && typeof icon !== "string") {
      return icon;
    }
    return /* @__PURE__ */ FSComponent.buildComponent(
      IconButton,
      {
        class: direction === -1 ? "incremental-decrease" : "incremental-increase",
        iconPath: (icon != null ? icon : direction === -1) ? "coui://html_ui/efb_ui/efb_os/Assets/icons/Remove.svg" : "coui://html_ui/efb_ui/efb_os/Assets/icons/Add.svg",
        callback: () => {
          var _a, _b;
          const newValue = Utils.Clamp(
            this.value.get() + this.step.get() * direction,
            this.min.get(),
            this.max.get()
          );
          this.value.set(newValue);
          (_b = (_a = this.props).onButtonClicked) == null ? void 0 : _b.call(_a, direction, newValue);
        },
        disabled: this.props.disabled
      }
    );
  }
  renderData() {
    if (this.useTextbox) {
      return /* @__PURE__ */ FSComponent.buildComponent(
        TextBox,
        {
          model: this.displayedValue,
          class: "data-display",
          charFilter: (char) => char >= "0" && char <= "9" || char === ".",
          suffix: this.props.suffix,
          align: "center",
          onInput: (e) => {
            const newValue = Number(e.value);
            if (newValue < this.min.get() || newValue > this.max.get()) {
              this.displayedValue.set(
                this.formatter(Utils.Clamp(newValue, this.min.get(), this.max.get()), this.max.get())
              );
              return;
            }
            if (!Number.isNaN(newValue)) {
              this.value.set(Utils.Clamp(newValue, this.min.get(), this.max.get()));
            }
          },
          onKeyDown: (event) => {
            var _a, _b;
            return (_b = (_a = this.props).onKeyDown) == null ? void 0 : _b.call(_a, event, this.value.get());
          },
          onKeyUp: (event) => {
            var _a, _b;
            return (_b = (_a = this.props).onKeyUp) == null ? void 0 : _b.call(_a, event, this.value.get());
          },
          onKeyPress: (event) => {
            var _a, _b;
            return (_b = (_a = this.props).onKeyPress) == null ? void 0 : _b.call(_a, event, this.value.get());
          },
          onFocusIn: this.onTextBoxFocusIn.bind(this),
          onFocusOut: this.onTextBoxFocusOut.bind(this),
          disabled: this.props.disabled
        }
      );
    } else {
      const dataSuffix = this.props.suffix ? /* @__PURE__ */ FSComponent.buildComponent(TT, { key: this.props.suffix, class: "suffix" }) : null;
      return /* @__PURE__ */ FSComponent.buildComponent("div", { class: "data-display" }, /* @__PURE__ */ FSComponent.buildComponent("span", { class: "data-value" }, this.displayedValue), dataSuffix);
    }
  }
  // Allow the user to properly enter a value by pausing it's update
  onTextBoxFocusIn() {
    this.displayedValueSubscription.pause();
  }
  onTextBoxFocusOut() {
    this.displayedValueSubscription.resume();
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent("div", { class: "incremental", ref: this.gamepadUiComponentRef }, this.renderButton(this.props.firstIcon, -1), this.renderData(), this.renderButton(this.props.firstIcon, 1));
  }
  destroy() {
    var _a;
    (_a = this.subscribableValueSubscription) == null ? void 0 : _a.destroy();
    this.valueSubscription.destroy();
    this.displayedValueSubscription.destroy();
    super.destroy();
  }
}
var MapShim = function() {
  if (typeof Map !== "undefined") {
    return Map;
  }
  function getIndex(arr, key) {
    var result = -1;
    arr.some(function(entry, index2) {
      if (entry[0] === key) {
        result = index2;
        return true;
      }
      return false;
    });
    return result;
  }
  return (
    /** @class */
    function() {
      function class_1() {
        this.__entries__ = [];
      }
      Object.defineProperty(class_1.prototype, "size", {
        /**
         * @returns {boolean}
         */
        get: function() {
          return this.__entries__.length;
        },
        enumerable: true,
        configurable: true
      });
      class_1.prototype.get = function(key) {
        var index2 = getIndex(this.__entries__, key);
        var entry = this.__entries__[index2];
        return entry && entry[1];
      };
      class_1.prototype.set = function(key, value2) {
        var index2 = getIndex(this.__entries__, key);
        if (~index2) {
          this.__entries__[index2][1] = value2;
        } else {
          this.__entries__.push([key, value2]);
        }
      };
      class_1.prototype.delete = function(key) {
        var entries = this.__entries__;
        var index2 = getIndex(entries, key);
        if (~index2) {
          entries.splice(index2, 1);
        }
      };
      class_1.prototype.has = function(key) {
        return !!~getIndex(this.__entries__, key);
      };
      class_1.prototype.clear = function() {
        this.__entries__.splice(0);
      };
      class_1.prototype.forEach = function(callback, ctx) {
        if (ctx === void 0) {
          ctx = null;
        }
        for (var _i = 0, _a = this.__entries__; _i < _a.length; _i++) {
          var entry = _a[_i];
          callback.call(ctx, entry[1], entry[0]);
        }
      };
      return class_1;
    }()
  );
}();
var isBrowser = typeof window !== "undefined" && typeof document !== "undefined" && window.document === document;
var global$1 = function() {
  if (typeof global !== "undefined" && global.Math === Math) {
    return global;
  }
  if (typeof self !== "undefined" && self.Math === Math) {
    return self;
  }
  if (typeof window !== "undefined" && window.Math === Math) {
    return window;
  }
  return Function("return this")();
}();
var requestAnimationFrame$1 = function() {
  if (typeof requestAnimationFrame === "function") {
    return requestAnimationFrame.bind(global$1);
  }
  return function(callback) {
    return setTimeout(function() {
      return callback(Date.now());
    }, 1e3 / 60);
  };
}();
var trailingTimeout = 2;
function throttle(callback, delay) {
  var leadingCall = false, trailingCall = false, lastCallTime = 0;
  function resolvePending() {
    if (leadingCall) {
      leadingCall = false;
      callback();
    }
    if (trailingCall) {
      proxy();
    }
  }
  function timeoutCallback() {
    requestAnimationFrame$1(resolvePending);
  }
  function proxy() {
    var timeStamp = Date.now();
    if (leadingCall) {
      if (timeStamp - lastCallTime < trailingTimeout) {
        return;
      }
      trailingCall = true;
    } else {
      leadingCall = true;
      trailingCall = false;
      setTimeout(timeoutCallback, delay);
    }
    lastCallTime = timeStamp;
  }
  return proxy;
}
var REFRESH_DELAY = 20;
var transitionKeys = ["top", "right", "bottom", "left", "width", "height", "size", "weight"];
var mutationObserverSupported = typeof MutationObserver !== "undefined";
var ResizeObserverController = (
  /** @class */
  function() {
    function ResizeObserverController2() {
      this.connected_ = false;
      this.mutationEventsAdded_ = false;
      this.mutationsObserver_ = null;
      this.observers_ = [];
      this.onTransitionEnd_ = this.onTransitionEnd_.bind(this);
      this.refresh = throttle(this.refresh.bind(this), REFRESH_DELAY);
    }
    ResizeObserverController2.prototype.addObserver = function(observer) {
      if (!~this.observers_.indexOf(observer)) {
        this.observers_.push(observer);
      }
      if (!this.connected_) {
        this.connect_();
      }
    };
    ResizeObserverController2.prototype.removeObserver = function(observer) {
      var observers2 = this.observers_;
      var index2 = observers2.indexOf(observer);
      if (~index2) {
        observers2.splice(index2, 1);
      }
      if (!observers2.length && this.connected_) {
        this.disconnect_();
      }
    };
    ResizeObserverController2.prototype.refresh = function() {
      var changesDetected = this.updateObservers_();
      if (changesDetected) {
        this.refresh();
      }
    };
    ResizeObserverController2.prototype.updateObservers_ = function() {
      var activeObservers = this.observers_.filter(function(observer) {
        return observer.gatherActive(), observer.hasActive();
      });
      activeObservers.forEach(function(observer) {
        return observer.broadcastActive();
      });
      return activeObservers.length > 0;
    };
    ResizeObserverController2.prototype.connect_ = function() {
      if (!isBrowser || this.connected_) {
        return;
      }
      document.addEventListener("transitionend", this.onTransitionEnd_);
      window.addEventListener("resize", this.refresh);
      if (mutationObserverSupported) {
        this.mutationsObserver_ = new MutationObserver(this.refresh);
        this.mutationsObserver_.observe(document, {
          attributes: true,
          childList: true,
          characterData: true,
          subtree: true
        });
      } else {
        document.addEventListener("DOMSubtreeModified", this.refresh);
        this.mutationEventsAdded_ = true;
      }
      this.connected_ = true;
    };
    ResizeObserverController2.prototype.disconnect_ = function() {
      if (!isBrowser || !this.connected_) {
        return;
      }
      document.removeEventListener("transitionend", this.onTransitionEnd_);
      window.removeEventListener("resize", this.refresh);
      if (this.mutationsObserver_) {
        this.mutationsObserver_.disconnect();
      }
      if (this.mutationEventsAdded_) {
        document.removeEventListener("DOMSubtreeModified", this.refresh);
      }
      this.mutationsObserver_ = null;
      this.mutationEventsAdded_ = false;
      this.connected_ = false;
    };
    ResizeObserverController2.prototype.onTransitionEnd_ = function(_a) {
      var _b = _a.propertyName, propertyName = _b === void 0 ? "" : _b;
      var isReflowProperty = transitionKeys.some(function(key) {
        return !!~propertyName.indexOf(key);
      });
      if (isReflowProperty) {
        this.refresh();
      }
    };
    ResizeObserverController2.getInstance = function() {
      if (!this.instance_) {
        this.instance_ = new ResizeObserverController2();
      }
      return this.instance_;
    };
    ResizeObserverController2.instance_ = null;
    return ResizeObserverController2;
  }()
);
var defineConfigurable = function(target, props) {
  for (var _i = 0, _a = Object.keys(props); _i < _a.length; _i++) {
    var key = _a[_i];
    Object.defineProperty(target, key, {
      value: props[key],
      enumerable: false,
      writable: false,
      configurable: true
    });
  }
  return target;
};
var getWindowOf = function(target) {
  var ownerGlobal = target && target.ownerDocument && target.ownerDocument.defaultView;
  return ownerGlobal || global$1;
};
var emptyRect = createRectInit(0, 0, 0, 0);
function toFloat(value2) {
  return parseFloat(value2) || 0;
}
function getBordersSize(styles) {
  var positions = [];
  for (var _i = 1; _i < arguments.length; _i++) {
    positions[_i - 1] = arguments[_i];
  }
  return positions.reduce(function(size, position) {
    var value2 = styles["border-" + position + "-width"];
    return size + toFloat(value2);
  }, 0);
}
function getPaddings(styles) {
  var positions = ["top", "right", "bottom", "left"];
  var paddings = {};
  for (var _i = 0, positions_1 = positions; _i < positions_1.length; _i++) {
    var position = positions_1[_i];
    var value2 = styles["padding-" + position];
    paddings[position] = toFloat(value2);
  }
  return paddings;
}
function getSVGContentRect(target) {
  var bbox = target.getBBox();
  return createRectInit(0, 0, bbox.width, bbox.height);
}
function getHTMLElementContentRect(target) {
  var clientWidth = target.clientWidth, clientHeight = target.clientHeight;
  if (!clientWidth && !clientHeight) {
    return emptyRect;
  }
  var styles = getWindowOf(target).getComputedStyle(target);
  var paddings = getPaddings(styles);
  var horizPad = paddings.left + paddings.right;
  var vertPad = paddings.top + paddings.bottom;
  var width = toFloat(styles.width), height = toFloat(styles.height);
  if (styles.boxSizing === "border-box") {
    if (Math.round(width + horizPad) !== clientWidth) {
      width -= getBordersSize(styles, "left", "right") + horizPad;
    }
    if (Math.round(height + vertPad) !== clientHeight) {
      height -= getBordersSize(styles, "top", "bottom") + vertPad;
    }
  }
  if (!isDocumentElement(target)) {
    var vertScrollbar = Math.round(width + horizPad) - clientWidth;
    var horizScrollbar = Math.round(height + vertPad) - clientHeight;
    if (Math.abs(vertScrollbar) !== 1) {
      width -= vertScrollbar;
    }
    if (Math.abs(horizScrollbar) !== 1) {
      height -= horizScrollbar;
    }
  }
  return createRectInit(paddings.left, paddings.top, width, height);
}
var isSVGGraphicsElement = function() {
  if (typeof SVGGraphicsElement !== "undefined") {
    return function(target) {
      return target instanceof getWindowOf(target).SVGGraphicsElement;
    };
  }
  return function(target) {
    return target instanceof getWindowOf(target).SVGElement && typeof target.getBBox === "function";
  };
}();
function isDocumentElement(target) {
  return target === getWindowOf(target).document.documentElement;
}
function getContentRect(target) {
  if (!isBrowser) {
    return emptyRect;
  }
  if (isSVGGraphicsElement(target)) {
    return getSVGContentRect(target);
  }
  return getHTMLElementContentRect(target);
}
function createReadOnlyRect(_a) {
  var x = _a.x, y = _a.y, width = _a.width, height = _a.height;
  var Constr = typeof DOMRectReadOnly !== "undefined" ? DOMRectReadOnly : Object;
  var rect = Object.create(Constr.prototype);
  defineConfigurable(rect, {
    x,
    y,
    width,
    height,
    top: y,
    right: x + width,
    bottom: height + y,
    left: x
  });
  return rect;
}
function createRectInit(x, y, width, height) {
  return { x, y, width, height };
}
var ResizeObservation = (
  /** @class */
  function() {
    function ResizeObservation2(target) {
      this.broadcastWidth = 0;
      this.broadcastHeight = 0;
      this.contentRect_ = createRectInit(0, 0, 0, 0);
      this.target = target;
    }
    ResizeObservation2.prototype.isActive = function() {
      var rect = getContentRect(this.target);
      this.contentRect_ = rect;
      return rect.width !== this.broadcastWidth || rect.height !== this.broadcastHeight;
    };
    ResizeObservation2.prototype.broadcastRect = function() {
      var rect = this.contentRect_;
      this.broadcastWidth = rect.width;
      this.broadcastHeight = rect.height;
      return rect;
    };
    return ResizeObservation2;
  }()
);
var ResizeObserverEntry = (
  /** @class */
  /* @__PURE__ */ function() {
    function ResizeObserverEntry2(target, rectInit) {
      var contentRect = createReadOnlyRect(rectInit);
      defineConfigurable(this, { target, contentRect });
    }
    return ResizeObserverEntry2;
  }()
);
var ResizeObserverSPI = (
  /** @class */
  function() {
    function ResizeObserverSPI2(callback, controller, callbackCtx) {
      this.activeObservations_ = [];
      this.observations_ = new MapShim();
      if (typeof callback !== "function") {
        throw new TypeError("The callback provided as parameter 1 is not a function.");
      }
      this.callback_ = callback;
      this.controller_ = controller;
      this.callbackCtx_ = callbackCtx;
    }
    ResizeObserverSPI2.prototype.observe = function(target) {
      if (!arguments.length) {
        throw new TypeError("1 argument required, but only 0 present.");
      }
      if (typeof Element === "undefined" || !(Element instanceof Object)) {
        return;
      }
      if (!(target instanceof getWindowOf(target).Element)) {
        throw new TypeError('parameter 1 is not of type "Element".');
      }
      var observations = this.observations_;
      if (observations.has(target)) {
        return;
      }
      observations.set(target, new ResizeObservation(target));
      this.controller_.addObserver(this);
      this.controller_.refresh();
    };
    ResizeObserverSPI2.prototype.unobserve = function(target) {
      if (!arguments.length) {
        throw new TypeError("1 argument required, but only 0 present.");
      }
      if (typeof Element === "undefined" || !(Element instanceof Object)) {
        return;
      }
      if (!(target instanceof getWindowOf(target).Element)) {
        throw new TypeError('parameter 1 is not of type "Element".');
      }
      var observations = this.observations_;
      if (!observations.has(target)) {
        return;
      }
      observations.delete(target);
      if (!observations.size) {
        this.controller_.removeObserver(this);
      }
    };
    ResizeObserverSPI2.prototype.disconnect = function() {
      this.clearActive();
      this.observations_.clear();
      this.controller_.removeObserver(this);
    };
    ResizeObserverSPI2.prototype.gatherActive = function() {
      var _this = this;
      this.clearActive();
      this.observations_.forEach(function(observation) {
        if (observation.isActive()) {
          _this.activeObservations_.push(observation);
        }
      });
    };
    ResizeObserverSPI2.prototype.broadcastActive = function() {
      if (!this.hasActive()) {
        return;
      }
      var ctx = this.callbackCtx_;
      var entries = this.activeObservations_.map(function(observation) {
        return new ResizeObserverEntry(observation.target, observation.broadcastRect());
      });
      this.callback_.call(ctx, entries, ctx);
      this.clearActive();
    };
    ResizeObserverSPI2.prototype.clearActive = function() {
      this.activeObservations_.splice(0);
    };
    ResizeObserverSPI2.prototype.hasActive = function() {
      return this.activeObservations_.length > 0;
    };
    return ResizeObserverSPI2;
  }()
);
var observers = typeof WeakMap !== "undefined" ? /* @__PURE__ */ new WeakMap() : new MapShim();
var ResizeObserver = (
  /** @class */
  /* @__PURE__ */ function() {
    function ResizeObserver2(callback) {
      if (!(this instanceof ResizeObserver2)) {
        throw new TypeError("Cannot call a class as a function.");
      }
      if (!arguments.length) {
        throw new TypeError("1 argument required, but only 0 present.");
      }
      var controller = ResizeObserverController.getInstance();
      var observer = new ResizeObserverSPI(callback, controller, this);
      observers.set(this, observer);
    }
    return ResizeObserver2;
  }()
);
[
  "observe",
  "unobserve",
  "disconnect"
].forEach(function(method) {
  ResizeObserver.prototype[method] = function() {
    var _a;
    return (_a = observers.get(this))[method].apply(_a, arguments);
  };
});
var index = function() {
  if (typeof global$1.ResizeObserver !== "undefined") {
    return global$1.ResizeObserver;
  }
  return ResizeObserver;
}();
const defaultProps = {
  speed: 50,
  // 50 pixels/seconds => 0.05 px/ms => 1px/20ms
  startDelay: 500,
  resetDelay: 500
};
class Marquee extends DisplayComponent {
  constructor() {
    var _a, _b, _c, _d, _e;
    super(...arguments);
    this.containerRef = FSComponent.createRef();
    this.marqueeRef = FSComponent.createRef();
    this.speedPxPerSeconds = SubscribableUtils.toSubscribable(
      (_a = this.props.speed) != null ? _a : defaultProps.speed,
      true
    );
    this.startDelayMs = SubscribableUtils.toSubscribable(
      (_c = (_b = this.props.startDelay) != null ? _b : this.props.delay) != null ? _c : defaultProps.startDelay,
      true
    );
    this.resetDelayMs = SubscribableUtils.toSubscribable(
      (_e = (_d = this.props.resetDelay) != null ? _d : this.props.delay) != null ? _e : defaultProps.resetDelay,
      true
    );
    this.transitionActive = Subject.create(false);
    this.delayMs = Subject.create(this.startDelayMs.get());
    this.durationMs = Subject.create(0);
    this.translationPx = Subject.create(0);
    this.containerWidth = 0;
    this.marqueeWidth = 0;
    this.transitionRightPx = 0;
    this.isLeftToRight = true;
    this.onMouseEnter = this._onMouseEnter.bind(this);
    this.onMouseLeave = this._onMouseLeave.bind(this);
    this.onTransitionEnded = this._onTransitionEnded.bind(this);
  }
  getDurationForDistance(distance) {
    return Math.abs(distance) * (1e3 / Math.abs(this.speedPxPerSeconds.get()));
  }
  _onMouseEnter() {
    if (this.transitionActive.get() || this.transitionRightPx >= 0) {
      return;
    }
    this.transitionActive.set(true);
    this.isLeftToRight = true;
    this.delayMs.set(this.startDelayMs.get());
    this.durationMs.set(this.getDurationForDistance(this.transitionRightPx));
    this.translationPx.set(this.transitionRightPx);
  }
  _onMouseLeave() {
    if (!this.transitionActive.get()) {
      return;
    }
    this.transitionActive.set(false);
    if (this.timeoutId) {
      clearTimeout(this.timeoutId);
      this.timeoutId = void 0;
    }
    this.isLeftToRight = true;
    this.delayMs.set(0);
    this.durationMs.set(0);
    this.translationPx.set(0);
  }
  _onTransitionEnded(event) {
    if (event.target !== this.marqueeRef.instance) {
      return;
    }
    if (event.propertyName !== "transform") {
      return;
    }
    if (!this.transitionActive.get()) {
      return;
    }
    const leftToRightEnded = this.isLeftToRight;
    this.isLeftToRight = !leftToRightEnded;
    if (leftToRightEnded) {
      this.delayMs.set(this.resetDelayMs.get());
      this.durationMs.set(0);
      this.translationPx.set(0);
      if (this.delayMs.get() === 0) {
        this.timeoutId = setTimeout(() => {
          this._onTransitionEnded(event);
        });
      }
    } else {
      this.delayMs.set(this.startDelayMs.get());
      this.durationMs.set(this.getDurationForDistance(this.transitionRightPx));
      this.translationPx.set(this.transitionRightPx);
    }
  }
  getMarqueeActivator() {
    let activator = this.marqueeRef.getOrDefault();
    if (this.props.activator) {
      const instance = this.props.activator.getOrDefault();
      if (instance instanceof HTMLElement) {
        activator = instance;
      } else if (instance instanceof GamepadUiComponent) {
        activator = instance.gamepadUiComponentRef.getOrDefault();
      }
    }
    return activator;
  }
  render() {
    const marqueeStyle = {
      "--duration": this.durationMs.map((duration) => `${duration}ms`),
      "--delay": this.delayMs.map((delay) => `${delay}ms`),
      "--translation": this.translationPx.map((translation) => `${translation}px`)
    };
    return /* @__PURE__ */ FSComponent.buildComponent("div", { ref: this.containerRef, class: mergeClassProp("efb-marquee-container", this.props.class) }, /* @__PURE__ */ FSComponent.buildComponent("div", { ref: this.marqueeRef, class: "efb-marquee", style: marqueeStyle }, this.props.children));
  }
  onAfterRender(node) {
    super.onAfterRender(node);
    this.containerWidth = this.containerRef.instance.clientWidth;
    this.marqueeWidth = this.marqueeRef.instance.clientWidth;
    this.transitionRightPx = Math.min(this.containerWidth - this.marqueeWidth, 0);
    const self2 = this;
    this.observer = new index(function(mutations) {
      mutations.forEach(function(_mutation) {
        self2.containerWidth = self2.containerRef.instance.clientWidth;
        self2.marqueeWidth = self2.marqueeRef.instance.clientWidth;
        self2.transitionRightPx = Math.min(self2.containerWidth - self2.marqueeWidth, 0);
        if (self2.transitionActive.get()) {
          self2.isLeftToRight = true;
          self2.durationMs.set(0);
          self2.translationPx.set(0);
          if (self2.transitionRightPx < 0) {
            setTimeout(() => {
              if (self2.transitionActive.get()) {
                self2.durationMs.set(self2.getDurationForDistance(self2.transitionRightPx));
                self2.translationPx.set(self2.transitionRightPx);
              }
            }, self2.startDelayMs.get());
          }
        }
      });
    });
    this.observer.observe(this.marqueeRef.instance);
    const activator = this.getMarqueeActivator();
    if (activator) {
      activator.addEventListener("mouseenter", this.onMouseEnter);
      activator.addEventListener("mouseleave", this.onMouseLeave);
    }
    this.marqueeRef.instance.addEventListener("transitionend", this.onTransitionEnded);
  }
  destroy() {
    var _a;
    (_a = this.observer) == null ? void 0 : _a.disconnect();
    const activator = this.getMarqueeActivator();
    if (activator) {
      activator.removeEventListener("mouseenter", this.onMouseEnter);
      activator.removeEventListener("mouseleave", this.onMouseLeave);
    }
    this.marqueeRef.instance.removeEventListener("transitionend", this.onTransitionEnded);
    super.destroy();
  }
}
const _NumberUnitDisplay = class _NumberUnitDisplay extends AbstractNumberUnitDisplay {
  constructor() {
    var _a, _b;
    super(...arguments);
    this.formatter = (_a = this.props.formatter) != null ? _a : NumberFormatter.create({
      // TODO Properly format the time
      precision: 0.01,
      maxDigits: 5,
      forceDecimalZeroes: false,
      nanString: "0"
    });
    this.unitFormatter = (_b = this.props.unitFormatter) != null ? _b : _NumberUnitDisplay.DEFAULT_UNIT_FORMATTER;
    this.unitTextBigDisplay = Subject.create("");
    this.unitTextSmallDisplay = Subject.create("");
    this.numberText = Subject.create("");
    this.unitTextBig = Subject.create("");
    this.unitTextSmall = Subject.create("");
  }
  /** @inheritdoc */
  onValueChanged(value2) {
    this.updateDisplay(value2, this.displayUnit.get());
  }
  /** @inheritdoc */
  onDisplayUnitChanged(displayUnit) {
    this.updateDisplay(this.value.get(), displayUnit);
  }
  /**
   * Updates this component's displayed number and unit text.
   * @param value The value to display.
   * @param displayUnit The unit type in which to display the value, or `null` if the value should be displayed in its
   * native unit type.
   */
  updateDisplay(value2, displayUnit) {
    if (!displayUnit || !value2.unit.canConvert(displayUnit)) {
      displayUnit = value2.unit;
    }
    const numberValue = value2.asUnit(displayUnit);
    const numberText = this.formatter(numberValue);
    this.numberText.set(numberText);
    _NumberUnitDisplay.unitTextCache[0] = "";
    _NumberUnitDisplay.unitTextCache[1] = "";
    this.unitFormatter(_NumberUnitDisplay.unitTextCache, displayUnit, numberValue);
    this.unitTextBig.set(_NumberUnitDisplay.unitTextCache[0]);
    this.unitTextSmall.set(_NumberUnitDisplay.unitTextCache[1]);
    this.updateUnitTextVisibility(
      numberValue,
      _NumberUnitDisplay.unitTextCache[0],
      _NumberUnitDisplay.unitTextCache[1]
    );
  }
  /**
   * Updates whether this component's unit text spans are visible.
   * @param numberValue The numeric value displayed by this component.
   * @param unitTextBig The text to display in the big text span.
   * @param unitTextSmall The text to display in the small text span.
   */
  updateUnitTextVisibility(numberValue, unitTextBig, unitTextSmall) {
    if (this.props.hideUnitWhenNaN === true && isNaN(numberValue)) {
      this.unitTextBigDisplay.set("none");
      this.unitTextSmallDisplay.set("none");
      return;
    }
    this.unitTextBigDisplay.set(unitTextBig === "" ? "none" : "");
    this.unitTextSmallDisplay.set(unitTextSmall === "" ? "none" : "");
  }
  /**
   * Creates the default mapping from unit to displayed text.
   * @returns The default mapping from unit to displayed text.
   */
  static createDefaultUnitTextMap() {
    const originalMap = UnitFormatter.getUnitTextMap();
    const map = {};
    for (const family in originalMap) {
      const nameMap = map[family] = {};
      const originalNameMap = originalMap[family];
      for (const name in originalNameMap) {
        const text = nameMap[name] = ["", ""];
        const originalText = originalNameMap[name];
        if (originalText[0] === "°") {
          text[0] = "°";
          text[1] = originalText.substring(1);
        } else {
          text[1] = originalText;
        }
      }
    }
    return map;
  }
  render() {
    var _a;
    return /* @__PURE__ */ FSComponent.buildComponent("div", { class: (_a = this.props.class) != null ? _a : "", style: "white-space: nowrap;" }, /* @__PURE__ */ FSComponent.buildComponent("span", { class: "numberunit-num" }, this.numberText), /* @__PURE__ */ FSComponent.buildComponent("span", { class: "numberunit-unit-big", style: { display: this.unitTextBigDisplay } }, this.unitTextBig), /* @__PURE__ */ FSComponent.buildComponent("span", { class: "numberunit-unit-small", style: { display: this.unitTextSmallDisplay } }, this.unitTextSmall));
  }
};
_NumberUnitDisplay.DEFAULT_UNIT_TEXT_MAP = _NumberUnitDisplay.createDefaultUnitTextMap();
_NumberUnitDisplay.DEFAULT_UNIT_FORMATTER = (out, unit) => {
  var _a;
  const text = (_a = _NumberUnitDisplay.DEFAULT_UNIT_TEXT_MAP[unit.family]) == null ? void 0 : _a[unit.name];
  if (text) {
    out[0] = text[0];
    out[1] = text[1];
  }
};
_NumberUnitDisplay.unitTextCache = ["", ""];
let NumberUnitDisplay = _NumberUnitDisplay;
class ProgressComponent extends GamepadUiComponent {
  constructor() {
    super(...arguments);
    this.fullCompletionSub = Subject.create(false);
  }
  _updateProgress(progressRatio) {
    this.updateProgress(progressRatio);
    this.fullCompletionSub.set(progressRatio === 1);
  }
  onAfterRender(node) {
    super.onAfterRender(node);
    if (typeof this.props.progressRatio === "number") {
      this._updateProgress(this.props.progressRatio);
    } else {
      this.progressRatioSubscription = this.props.progressRatio.sub(this._updateProgress.bind(this), true);
    }
  }
  destroy() {
    var _a;
    (_a = this.progressRatioSubscription) == null ? void 0 : _a.destroy();
    super.destroy();
  }
}
class CircularProgress extends ProgressComponent {
  constructor() {
    super(...arguments);
    this.circularProgressRef = FSComponent.createRef();
    this.lowerSliceRef = FSComponent.createRef();
    this.higherSliceRef = FSComponent.createRef();
    this.sliceClassToggleSub = Subject.create(false);
    this.sliceClassToggleNotSub = this.sliceClassToggleSub.map(SubscribableMapFunctions.not());
    this.circularProgressStyle = ObjectSubject.create({
      "--circular-progress-rotation-value-left": "0",
      "--circular-progress-rotation-value-right": "0"
    });
    this.circularProgressPrimaryBackgroundClass = "circular-progress-primary-background";
    this.circularProgressSecondaryBackgroundClass = "circular-progress-secondary-background";
    this.circularProgressSliceClasses = {
      "circular-progress-slice": true,
      [this.circularProgressSecondaryBackgroundClass]: this.sliceClassToggleSub,
      [this.circularProgressPrimaryBackgroundClass]: this.sliceClassToggleNotSub
    };
  }
  updateProgressByHalf(progressRatio, overHalf) {
    if (overHalf) {
      this.circularProgressStyle.set("--circular-progress-rotation-value-left", `${progressRatio}turn`);
      this.circularProgressStyle.set("--circular-progress-rotation-value-right", "0");
    } else {
      this.circularProgressStyle.set("--circular-progress-rotation-value-left", "0");
      this.circularProgressStyle.set("--circular-progress-rotation-value-right", `${progressRatio}turn`);
    }
    this.sliceClassToggleSub.set(overHalf);
  }
  updateProgress(progressRatio) {
    this.updateProgressByHalf(progressRatio, progressRatio > 0.5);
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent(
      "div",
      {
        class: { "circular-progress-container": true, "full-completion": this.fullCompletionSub },
        ref: this.gamepadUiComponentRef
      },
      /* @__PURE__ */ FSComponent.buildComponent("div", { class: "circular-progress-circle-container" }, /* @__PURE__ */ FSComponent.buildComponent("div", { class: "circular-progress-circle outer" }), /* @__PURE__ */ FSComponent.buildComponent("div", { class: "circular-progress-circle inner" })),
      /* @__PURE__ */ FSComponent.buildComponent(
        "div",
        {
          class: {
            "circular-progress": true,
            [this.circularProgressPrimaryBackgroundClass]: this.sliceClassToggleSub,
            [this.circularProgressSecondaryBackgroundClass]: this.sliceClassToggleNotSub
          },
          ref: this.circularProgressRef,
          style: this.circularProgressStyle
        },
        /* @__PURE__ */ FSComponent.buildComponent(
          "div",
          {
            class: __spreadProps(__spreadValues({}, this.circularProgressSliceClasses), {
              left: true
            }),
            ref: this.higherSliceRef
          }
        ),
        /* @__PURE__ */ FSComponent.buildComponent(
          "div",
          {
            class: __spreadProps(__spreadValues({}, this.circularProgressSliceClasses), {
              right: true
            }),
            ref: this.lowerSliceRef
          }
        ),
        this.props.iconPath && /* @__PURE__ */ FSComponent.buildComponent(
          IconElement,
          {
            class: {
              "full-completion-icon": true,
              hide: this.fullCompletionSub.map(SubscribableMapFunctions.not())
            },
            url: this.props.iconPath
          }
        )
      )
    );
  }
}
class ProgressBar extends ProgressComponent {
  constructor() {
    super(...arguments);
    this.circularProgressStyle = ObjectSubject.create({
      "--progress-bar-width-percentage": "0%"
    });
  }
  updateProgress(progressRatio) {
    let progressPercentage = progressRatio * 100;
    if (progressPercentage > 100) progressPercentage = 100;
    else if (progressPercentage < 0) progressPercentage = 0;
    this.circularProgressStyle.set("--progress-bar-width-percentage", `${progressPercentage}%`);
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent(
      "div",
      {
        class: { "progress-bar-container": true, "full-completion": this.fullCompletionSub },
        ref: this.gamepadUiComponentRef
      },
      /* @__PURE__ */ FSComponent.buildComponent("div", { class: "progress-bar", style: this.circularProgressStyle })
    );
  }
}
class SearchBar extends GamepadUiComponent {
  constructor(props) {
    var _a, _b, _c, _d, _e, _f;
    super(props);
    this.resultItems = ArraySubject.create();
    this.onInputSearchSub = this.props.model || Subject.create("");
    this.textBoxRef = (_a = this.props.textBoxRef) != null ? _a : FSComponent.createRef();
    this.searchBarListRef = (_b = this.props.searchBarListRef) != null ? _b : FSComponent.createRef();
    this.isSearchBarFocus = Subject.create(false);
    this.placeholder = (_c = this.props.placeholder) != null ? _c : "@fs-base-efb,TT:EFB.COMMON.SEARCH_PLACEHOLDER";
    this.DEBOUNCE_DURATION = 0;
    this.subs = [];
    this.currentSearchId = 0;
    this.isSearchPending = Subject.create(false);
    this.gamepadNavigationOptions = (_d = this.props.gamepadNavigationOptions) != null ? _d : {};
    this.askForResultNavigationSubscription = this.isSearchPending.sub((pending) => {
      if (!pending) {
        this.askForResultNavigationSubscription.pause();
        this.enableSearchResultNavigation();
      }
    });
    this.onDelete = () => {
    };
    this.prefix = Subject.create(SubscribableUtils.toSubscribable(this.props.prefix || "", true).get());
    this.suffix = Subject.create(SubscribableUtils.toSubscribable(this.props.suffix || "", true).get());
    this.listRef = FSComponent.createRef();
    this.askForResultNavigationSubscription.pause();
    if (((_e = props.gamepadNavigationOptions) == null ? void 0 : _e.isListFocused) !== void 0) {
      if (SubscribableUtils.isMutableSubscribable(props.gamepadNavigationOptions.isListFocused)) {
        this.isResultListFocus = props.gamepadNavigationOptions.isListFocused;
      } else if (SubscribableUtils.isSubscribable(props.gamepadNavigationOptions.isListFocused)) {
        this.isResultListFocus = Subject.create(props.gamepadNavigationOptions.isListFocused.get());
        this.subs.push(
          props.gamepadNavigationOptions.isListFocused.sub(
            (value2) => this.isResultListFocus.set(value2)
          )
        );
      } else {
        this.isResultListFocus = Subject.create(props.gamepadNavigationOptions.isListFocused);
      }
    } else {
      this.isResultListFocus = Subject.create(false);
    }
    this.isListVisible = (_f = this.props.isListVisible) != null ? _f : MappedSubject.create(
      ([searchBarFocus, listFocus]) => searchBarFocus || listFocus,
      this.isSearchBarFocus,
      this.isResultListFocus
    );
  }
  enableSearchResultNavigation() {
    this.isResultListFocus.set(true);
  }
  disableSearchResultNavigation() {
    this.isResultListFocus.set(false);
  }
  tryRenderItem(data, index2) {
    return this.props.renderItem ? this.props.renderItem(data, index2) : this.renderItem(data, index2);
  }
  /* Must be implemented by children classes */
  renderItem(_data, _index) {
    throw new Error("Missing renderItem props");
  }
  tryUpdateResultItems(input) {
    return this.props.updateResultItems ? this.props.updateResultItems(input) : this.updateResultItems(input);
  }
  /* Must be implemented by children classes */
  updateResultItems(_input) {
    throw new Error("Missing updateResultItems props");
  }
  async onSearchUpdated(input) {
    this.currentSearchId++;
    const searchId = this.currentSearchId;
    this.isSearchPending.set(true);
    const values = await this.tryUpdateResultItems(input);
    if (searchId !== this.currentSearchId) {
      return;
    }
    this.isSearchPending.set(false);
    this.resultItems.set(values);
    if (values.length === 0) {
      this.disableSearchResultNavigation();
    }
  }
  /** @deprecated Do nothing. */
  onResultItemsUpdated() {
  }
  render() {
    var _a, _b;
    return /* @__PURE__ */ FSComponent.buildComponent("div", { class: "search-bar", ref: this.gamepadUiComponentRef }, /* @__PURE__ */ FSComponent.buildComponent(
      TextBox,
      {
        model: this.onInputSearchSub,
        ref: this.textBoxRef,
        placeholder: this.placeholder,
        disabled: this.props.disabled,
        hidePlaceholderOnFocus: this.props.hidePlaceholderOnFocus,
        focusOnInit: this.props.focusOnInit,
        onFocusIn: () => {
          var _a2, _b2;
          Wait.awaitFrames(1).catch().then(() => this.isSearchBarFocus.set(true));
          (_b2 = (_a2 = this.props).onFocusIn) == null ? void 0 : _b2.call(_a2);
        },
        onFocusOut: () => {
          var _a2, _b2;
          Wait.awaitFrames(1).catch().then(() => this.isSearchBarFocus.set(false));
          (_b2 = (_a2 = this.props).onFocusOut) == null ? void 0 : _b2.call(_a2);
          if (this.props.emptySearchOnFocusOut || !this.onInputSearchSub.get().trim()) {
            this.textBoxRef.instance.inputRef.instance.clearInput();
          }
        },
        onInput: this.props.onInput,
        onValueValidated: (text) => {
          var _a2, _b2;
          this.askForResultNavigationSubscription.resume(true);
          (_b2 = (_a2 = this.props).onValueValidated) == null ? void 0 : _b2.call(_a2, text);
        },
        debounceDuration: (_a = this.props.debounceDuration) != null ? _a : this.DEBOUNCE_DURATION,
        onKeyPress: this.props.onKeyPress,
        charFilter: this.props.charFilter,
        onDelete: this.onDelete,
        focusOnClear: this.props.focusOnClear,
        showDeleteIcon: (_b = this.props.showDeleteIcon) != null ? _b : true,
        customDeleteIcon: this.props.customDeleteIcon,
        prefix: this.prefix,
        suffix: this.suffix
      }
    ), /* @__PURE__ */ FSComponent.buildComponent("div", { class: "search-bar-list-container", ref: this.searchBarListRef }, /* @__PURE__ */ FSComponent.buildComponent(
      List,
      {
        ref: this.listRef,
        data: this.resultItems,
        isListVisible: this.isListVisible,
        isScrollable: true,
        class: "search-bar-list",
        renderItem: this.tryRenderItem.bind(this),
        refreshOnUpdate: this.props.refreshOnUpdate,
        gamepadNavigationOptions: __spreadProps(__spreadValues({}, this.gamepadNavigationOptions), {
          onItemHovered: (item, index2) => {
            var _a2, _b2;
            const scrollToIndex = Math.max(0, index2);
            this.listRef.instance.scrollToItem(scrollToIndex);
            (_b2 = (_a2 = this.gamepadNavigationOptions).onItemHovered) == null ? void 0 : _b2.call(_a2, item, index2);
          },
          onItemSelected: (item, index2) => {
            var _a2, _b2;
            this.disableSearchResultNavigation();
            (_b2 = (_a2 = this.gamepadNavigationOptions).onItemSelected) == null ? void 0 : _b2.call(_a2, item, index2);
          },
          isListFocused: this.isResultListFocus
        })
      }
    )));
  }
  onAfterRender(node) {
    super.onAfterRender(node);
    if (this.props.emptySearchOnInit) {
      this.onSearchUpdated("");
    }
    this.subs.push(
      this.onInputSearchSub.sub((value2) => {
        this.onSearchUpdated(value2);
      }),
      this.isSearchBarFocus.sub((focus) => {
        var _a, _b;
        if (focus) {
          (_b = (_a = this.props).onListDisplayed) == null ? void 0 : _b.call(_a);
        }
      })
    );
  }
  destroy() {
    this.subs.forEach((s) => s.destroy());
    this.askForResultNavigationSubscription.destroy();
    super.destroy();
  }
}
const _FacilitySearchUtils = class _FacilitySearchUtils {
  constructor(bus) {
    this.facilityLoader = null;
    this.position = new GeoPoint(0, 0);
    bus.getSubscriber().on("gps-position").handle((pos) => {
      this.position.set(pos.lat, pos.long);
    });
    this.facilityLoader = new FacilityLoader(FacilityRepository.getRepository(bus));
  }
  static getSearchUtils(bus) {
    var _a;
    return (_a = _FacilitySearchUtils.INSTANCE) != null ? _a : _FacilitySearchUtils.INSTANCE = new _FacilitySearchUtils(bus);
  }
  orderByIdentsAndDistance(a, b) {
    const aIdent = a.icaoStruct.ident.trim();
    const bIdent = b.icaoStruct.ident.trim();
    if (aIdent === bIdent) {
      const aDist = this.position.distance(a.lat, a.lon);
      const bDist = this.position.distance(b.lat, b.lon);
      return aDist - bDist;
    } else {
      return aIdent.localeCompare(bIdent);
    }
  }
  /**
   * Loads facilities based on an ident to search for, a search type and whether to exclude terminal facilities
   * @param ident The ident to search for
   * @param facilitySearchType The search type. Defaults to {@link FacilitySearchType.All}
   * @param excludeTerminalFacilities Whether to exclude terminal facilities. Defaults to `true`.
   * @returns a readonly array of facilities
   */
  async loadFacilities(ident, facilitySearchType = FacilitySearchType.All, excludeTerminalFacilities = true) {
    if (this.facilityLoader === null) {
      return Promise.resolve([]);
    }
    const icaos = await this.facilityLoader.searchByIdentWithIcaoStructs(facilitySearchType, ident, 15);
    const facilities = (await this.facilityLoader.getFacilities(icaos)).filter(
      (fac) => fac !== null && (!excludeTerminalFacilities || fac.icaoStruct.airport.length === 0)
    );
    facilities.sort((a, b) => this.orderByIdentsAndDistance(a, b));
    return facilities;
  }
};
_FacilitySearchUtils.INSTANCE = null;
let FacilitySearchUtils = _FacilitySearchUtils;
class FacilityResultItem extends GamepadUiComponent {
  onAfterRender(node) {
    super.onAfterRender(node);
    this.gamepadUiComponentRef.instance.onmousedown = (ev) => {
      ev.preventDefault();
      if (this.props.callback === void 0) {
        return;
      }
      this.props.callback(this.props.facility);
    };
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent("div", { class: mergeClassProp("facility-result-item", this.props.class), ref: this.gamepadUiComponentRef }, /* @__PURE__ */ FSComponent.buildComponent("div", { class: "icon-container" }, /* @__PURE__ */ FSComponent.buildComponent("icon-element", { "icon-url": getFacilityIconPath(ICAO.getFacilityType(this.props.facility.icao)) })), /* @__PURE__ */ FSComponent.buildComponent("span", { class: "icao" }, ICAO.getIdent(this.props.facility.icao), this.props.separator), /* @__PURE__ */ FSComponent.buildComponent("span", { class: "facility-name" }, Utils.Translate(this.props.facility.name).padEnd(5, "...")));
  }
}
const _SearchFacilityHistoryManager = class _SearchFacilityHistoryManager {
  constructor() {
    this.MAX_ITEMS_STORED = 5;
    this.storedICAOs = ArraySubject.create();
    this.loadICAOsFromStorage();
  }
  /**
   * Retrieves all the stored recent searches
   */
  loadICAOsFromStorage() {
    let arrayICAOs = [];
    const stringICAOs = DataStore.get(_SearchFacilityHistoryManager.DATASTORE_KEY);
    if (stringICAOs === void 0 || typeof stringICAOs !== "string") {
      this.storedICAOs.set(arrayICAOs);
      return;
    }
    try {
      arrayICAOs = JSON.parse(stringICAOs);
    } catch (e) {
      console.error("JSON failed, impossible to parse : ", stringICAOs);
    }
    this.storedICAOs.set(arrayICAOs);
  }
  saveICAOsToStorage() {
    const stringICAOs = JSON.stringify(this.storedICAOs.getArray());
    DataStore.set(_SearchFacilityHistoryManager.DATASTORE_KEY, stringICAOs);
  }
  mostRecentSearch(icao) {
    const foundIndex = this.storedICAOs.getArray().findIndex((storedIcao) => storedIcao === icao);
    if (foundIndex !== -1) {
      this.storedICAOs.removeAt(foundIndex);
    }
    this.storedICAOs.insert(icao, 0);
    while (this.storedICAOs.length > this.MAX_ITEMS_STORED) {
      this.storedICAOs.removeAt(this.MAX_ITEMS_STORED);
    }
    this.saveICAOsToStorage();
  }
  /**
   * Retrieve the search facility history as an array
   * @param input the input of the search
   * @param max_items the maximum number of items returned
   * @returns the recent searches as an array of icaos
   */
  getStoredICAOs(input, max_items = this.MAX_ITEMS_STORED) {
    this.loadICAOsFromStorage();
    return this.storedICAOs.getArray().filter((icao) => ICAO.getIdent(icao).startsWith(input.toUpperCase())).slice(0, max_items);
  }
};
_SearchFacilityHistoryManager.DATASTORE_KEY = "efb.search-bar-history";
let SearchFacilityHistoryManager = _SearchFacilityHistoryManager;
class SearchFacility extends SearchBar {
  constructor(props) {
    super(props);
    this.facilityLoader = new FacilityLoader(FacilityRepository.getRepository(this.props.bus));
    this.internalSelectedFacilitySub = Subject.create(null);
    this.selectedFacilitySub = this.props.selectedFacilitySub || this.internalSelectedFacilitySub;
    this.selectedFacility = null;
    this.recentSearchesICAO = ArraySubject.create();
    this.historyManager = new SearchFacilityHistoryManager();
    this.DEBOUNCE_DURATION = 300;
    this.ppos = new GeoPoint(0, 0);
    this.currentHoveredItemIndex = Subject.create(-1);
    this.gamepadNavigationOptions = __spreadProps(__spreadValues({}, this.props.gamepadNavigationOptions), {
      onItemHovered: (item, index2) => {
        var _a, _b;
        this.currentHoveredItemIndex.set(index2);
        (_b = (_a = this.props.gamepadNavigationOptions) == null ? void 0 : _a.onItemHovered) == null ? void 0 : _b.call(_a, item, index2);
      },
      onItemSelected: (item, index2) => {
        var _a, _b;
        this.itemCallback(item);
        (_b = (_a = this.props.gamepadNavigationOptions) == null ? void 0 : _a.onItemSelected) == null ? void 0 : _b.call(_a, item, index2);
      }
    });
    this.hideCustoms = MappedSubject.create(
      ([focus, input]) => !focus || input.length !== 0,
      this.isSearchBarFocus,
      this.onInputSearchSub
    );
    this.customItemSelectRef = FSComponent.createRef();
    this.customItemPositionRef = FSComponent.createRef();
    this.onDelete = () => {
      var _a, _b;
      (_b = (_a = this.props).onDelete) == null ? void 0 : _b.call(_a);
      this.prefix.set("");
    };
    props.bus.getSubscriber().on("gps-position").atFrequency(1).handle((pos) => this.ppos.set(pos.lat, pos.long));
  }
  renderItem(data, index2) {
    return /* @__PURE__ */ FSComponent.buildComponent(
      FacilityResultItem,
      {
        class: {
          hover: this.currentHoveredItemIndex.map(
            (hoveredItemIndex) => hoveredItemIndex === index2
          )
        },
        facility: data,
        separator: ", ",
        callback: this.itemCallback.bind(this)
      }
    );
  }
  itemCallback(data) {
    this.selectedFacility = data;
    if (this.props.onFacilitySelectionFormatter) {
      this.onInputSearchSub.set(this.props.onFacilitySelectionFormatter(data));
    }
    this.updateIcon(data);
    this.props.onFacilityClick(data);
    if (ICAO.getFacilityType(data.icao) !== FacilityType.USR) {
      this.historyManager.mostRecentSearch(data.icao);
    }
    this.textBoxRef.instance.inputRef.instance.blur();
  }
  updateIcon(fac) {
    this.prefix.set(/* @__PURE__ */ FSComponent.buildComponent(IconElement, { url: getFacilityIconPath(ICAO.getFacilityType(fac.icao)) }));
  }
  async updateResultItems(input) {
    var _a;
    const searchInput = input.trim();
    const facilities = [];
    const recentSearchs = (await this.updateRecentSearches(searchInput)).filter((e) => {
      if (e === null) {
        return false;
      }
      switch (this.props.facilitySearchType) {
        case FacilitySearchType.Airport:
          return ICAO.getFacilityType(e.icao) === FacilityType.Airport;
        case FacilitySearchType.Intersection:
          return ICAO.getFacilityType(e.icao) === FacilityType.Intersection;
        case FacilitySearchType.Vor:
          return ICAO.getFacilityType(e.icao) === FacilityType.VOR;
        case FacilitySearchType.Ndb:
          return ICAO.getFacilityType(e.icao) === FacilityType.NDB;
        case FacilitySearchType.User:
          return ICAO.getFacilityType(e.icao) === FacilityType.USR;
        case FacilitySearchType.Visual:
          return ICAO.getFacilityType(e.icao) === FacilityType.VIS;
        case FacilitySearchType.AllExceptVisual:
          return ICAO.getFacilityType(e.icao) !== FacilityType.VIS;
        case FacilitySearchType.Boundary:
        case FacilitySearchType.All:
        default:
          return true;
      }
    });
    facilities.push(...recentSearchs);
    if (searchInput.length > 0) {
      facilities.push(
        ...await FacilitySearchUtils.getSearchUtils(this.props.bus).loadFacilities(
          searchInput,
          this.props.facilitySearchType,
          (_a = this.props.excludeTerminalFacilities) != null ? _a : true
        )
      );
    }
    return unique(facilities);
  }
  async updateRecentSearches(input) {
    const facilitiesPromises = this.historyManager.getStoredICAOs(input, this.props.maxHistoryItems).map((icao) => {
      return this.facilityLoader.getFacility(ICAO.getFacilityType(icao), icao).catch(() => {
        return null;
      });
    });
    return Promise.all(facilitiesPromises);
  }
  resetInput() {
    var _a, _b;
    this.onInputSearchSub.set("");
    (_b = (_a = this.props).onResetInput) == null ? void 0 : _b.call(_a);
    this.internalSelectedFacilitySub.set(null);
    this.prefix.set("");
  }
  restoreInput() {
    const inputHasBeenCleared = !this.onInputSearchSub.get();
    if (this.selectedFacility && inputHasBeenCleared) {
      this.itemCallback(this.selectedFacility);
    }
  }
  onAfterRender(node) {
    var _a;
    super.onAfterRender(node);
    this.selectedFacilitySubscription = (_a = this.props.selectedFacilitySub) == null ? void 0 : _a.sub((facility) => {
      var _a2, _b;
      const localFacility = this.selectedFacilitySub.get();
      this.selectedFacility = facility;
      if (facility) {
        this.updateIcon(facility);
      }
      if ((facility == null ? void 0 : facility.icao) !== (localFacility == null ? void 0 : localFacility.icao) || this.props.selectedFacilitySub === this.selectedFacilitySub) {
        this.onInputSearchSub.set(
          facility ? ((_b = (_a2 = this.props).onFacilitySelectionFormatter) == null ? void 0 : _b.call(_a2, facility)) || facility.icaoStruct.ident : ""
        );
      }
    }, true);
    this.subs.push(
      this.isSearchBarFocus.sub((focus) => {
        var _a2, _b;
        (_b = (_a2 = this.props).onTextBoxFocused) == null ? void 0 : _b.call(_a2, focus);
        if (focus) {
          this.onSearchUpdated(this.onInputSearchSub.get());
        }
      })
    );
    if (this.props.positionSelectable) {
      FSComponent.render(
        /* @__PURE__ */ FSComponent.buildComponent("div", { class: { "search-facility-custom-items": true, hide: this.hideCustoms } }, /* @__PURE__ */ FSComponent.buildComponent("div", { class: "facility-result-item", ref: this.customItemPositionRef }, /* @__PURE__ */ FSComponent.buildComponent("div", { class: "icon-container" }, /* @__PURE__ */ FSComponent.buildComponent(
          "icon-element",
          {
            "icon-url": `coui://html_ui/efb_ui/efb_os/Assets/icons/facilities/Challenges.svg`
          }
        )), /* @__PURE__ */ FSComponent.buildComponent(
          TT,
          {
            key: "@fs-base-efb-app-navigation-map,TT:EFB.NAVIGATION_MAP.YOUR_POSITION",
            format: "ucfirst"
          }
        ))),
        this.searchBarListRef.instance,
        0
      );
      this.customItemPositionRef.instance.onmousedown = () => {
        this.itemCallback(
          createCustomFacility(FacilityRepository.getRepository(this.props.bus), this.ppos.lat, this.ppos.lon)
        );
      };
    }
    this.onSearchUpdated("");
  }
  destroy() {
    var _a;
    (_a = this.selectedFacilitySubscription) == null ? void 0 : _a.destroy();
    super.destroy();
  }
}
class Slider extends GamepadUiComponent {
  constructor(props) {
    var _a, _b, _c, _d, _e, _f, _g;
    super(props);
    this.max = SubscribableUtils.toSubscribable((_a = this.props.max) != null ? _a : 100, true);
    this.min = SubscribableUtils.toSubscribable((_b = this.props.min) != null ? _b : 0, true);
    this.isButtonDisabled = SubscribableUtils.toSubscribable((_c = this.props.disabled) != null ? _c : false, true);
    this.isButtonSelected = Subject.create(false);
    this.precision = MappedSubject.create(
      ([step, min, _max]) => this.convertValueToPercent(step + min),
      SubscribableUtils.toSubscribable((_d = this.props.step) != null ? _d : 0, true),
      this.min,
      this.max
    );
    this.isButtonHoverable = MappedSubject.create(
      ([isHoverable, isDisabled, isSelected]) => {
        return isHoverable && !isDisabled && !isSelected;
      },
      SubscribableUtils.toSubscribable((_e = this.props.hoverable) != null ? _e : true, true),
      this.isButtonDisabled,
      this.isButtonSelected
    );
    this.verticalSlider = (_f = this.props.vertical) != null ? _f : false;
    this.allowWheel = (_g = this.props.allowWheel) != null ? _g : false;
    this.sliderBarRef = FSComponent.createRef();
    this.sliderBarRect = new DOMRect();
    this.allowMovement = false;
    this.mousePos = 0;
    this.subs = [];
    this.onGlobalMouseUp = this._onGlobalMouseUp.bind(this);
    this.onGlobalMouseMove = this._onGlobalMouseMove.bind(this);
    if (SubscribableUtils.isMutableSubscribable(props.value)) {
      this.value = props.value;
    } else if (SubscribableUtils.isSubscribable(props.value)) {
      this.value = Subject.create(props.value.get());
      this.subscribableValueSubscription = props.value.sub((value2) => this.value.set(value2));
    } else {
      this.value = Subject.create(props.value);
    }
    this.valuePercent = Subject.create(this.convertValueToPercent(this.value.get()));
    this.completionRatio = this.valuePercent.map((ratio) => `${ratio.toString()}%`);
  }
  convertValueToPercent(val) {
    return Utils.Clamp(Math.abs((val - this.min.get()) / (this.max.get() - this.min.get())) * 100, 0, 100);
  }
  onMouseDown() {
    var _a, _b;
    if (this.disabled.get()) {
      return;
    }
    (_b = (_a = this.props).onFocusIn) == null ? void 0 : _b.call(_a);
    this.isButtonSelected.set(true);
    this.allowMovement = true;
    this.sliderBarRect = this.gamepadUiComponentRef.instance.getBoundingClientRect();
    document.addEventListener("mouseup", this.onGlobalMouseUp);
    document.addEventListener("mouseleave", this.onGlobalMouseUp);
    document.addEventListener("mousemove", this.onGlobalMouseMove);
  }
  onMouseWheel(e) {
    e.preventDefault();
    const precision = this.precision.get() || 1;
    const scrollDirection = this.verticalSlider ? -1 : 1;
    const step = precision * (e.deltaY < 1 ? -1 : 1) * scrollDirection;
    this.valuePercent.set(Utils.Clamp(this.valuePercent.get() + step, 0, 100));
  }
  _onGlobalMouseUp() {
    var _a, _b;
    this.allowMovement = false;
    (_b = (_a = this.props).onFocusOut) == null ? void 0 : _b.call(_a);
    this.isButtonSelected.set(false);
    document.removeEventListener("mouseup", this.onGlobalMouseUp);
    document.removeEventListener("mouseleave", this.onGlobalMouseUp);
    document.removeEventListener("mousemove", this.onGlobalMouseMove);
  }
  _onGlobalMouseMove(e) {
    const clientPos = this.verticalSlider ? e.clientY : e.clientX;
    if (!this.allowMovement || clientPos === this.mousePos) {
      return;
    }
    this.mousePos = clientPos;
    let sliderStart = this.sliderBarRect.left;
    let sliderEnd = this.sliderBarRect.right;
    let sliderSize = this.sliderBarRect.width;
    let startRatio = 0;
    if (this.verticalSlider) {
      sliderStart = this.sliderBarRect.top;
      sliderEnd = this.sliderBarRect.bottom;
      sliderSize = this.sliderBarRect.height;
      startRatio = 100;
    }
    if (clientPos < sliderStart) {
      this.valuePercent.set(startRatio);
      return;
    }
    if (clientPos > sliderEnd) {
      this.valuePercent.set(100 - startRatio);
      return;
    }
    const mousePosOnSlider = Utils.Clamp(
      Math.abs(startRatio - (clientPos - sliderStart) / sliderSize * 100),
      0,
      100
    );
    let sliderPos = mousePosOnSlider;
    const precision = this.precision.get();
    if (precision !== 0) {
      const quotient = Math.trunc((mousePosOnSlider + precision / 2) / precision);
      sliderPos = Utils.Clamp(quotient * precision, 0, 100);
    }
    this.valuePercent.set(sliderPos);
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent(
      "div",
      {
        class: {
          slider: true,
          reverse: this.min > this.max,
          vertical: this.verticalSlider,
          hoverable: this.isButtonHoverable,
          selected: this.isButtonSelected
        },
        style: { "--ratio-value": this.completionRatio },
        ref: this.gamepadUiComponentRef
      },
      /* @__PURE__ */ FSComponent.buildComponent("div", { class: "disabled-layer" }),
      /* @__PURE__ */ FSComponent.buildComponent("div", { class: "slider-bar", ref: this.sliderBarRef }, /* @__PURE__ */ FSComponent.buildComponent("div", { class: "slider-rail" }), /* @__PURE__ */ FSComponent.buildComponent("div", { class: "slider-track" }), /* @__PURE__ */ FSComponent.buildComponent("div", { class: "slider-button" }))
    );
  }
  onAfterRender(node) {
    super.onAfterRender(node);
    Coherent.on("mouseReleaseOutsideView", this.onGlobalMouseUp);
    this.sliderBarRef.instance.onmousedown = this.onMouseDown.bind(this);
    if (this.allowWheel) {
      this.sliderBarRef.instance.onwheel = this.onMouseWheel.bind(this);
    }
    this.subs.push(
      this.valuePercent.sub(
        (val) => this.value.set(val * (this.max.get() - this.min.get()) / 100 + this.min.get())
      ),
      this.value.sub((value2) => {
        var _a, _b;
        (_b = (_a = this.props).onValueChange) == null ? void 0 : _b.call(_a, value2);
        this.valuePercent.set(this.convertValueToPercent(value2));
      })
    );
    const sliderElement = this.gamepadUiComponentRef.getOrDefault();
    if (sliderElement !== null) {
      const inputActions = this.verticalSlider ? {
        decrease: InputAction.SCROLL_DOWN,
        increase: InputAction.SCROLL_UP
      } : {
        decrease: InputAction.SCROLL_LEFT,
        increase: InputAction.SCROLL_RIGHT
      };
      let isScrolling = false;
      const addMoveSliderInputAction = (inputAction, factor) => this.inputManager.addInputActionOnHover(
        sliderElement,
        inputAction,
        (axis) => {
          var _a, _b;
          if (!isScrolling) {
            (_b = (_a = this.props).onFocusIn) == null ? void 0 : _b.call(_a);
            this.isButtonSelected.set(true);
            isScrolling = true;
          }
          this.valuePercent.set(this.valuePercent.get() + factor * axis * axis * 2);
          return true;
        },
        { inputType: "axis" }
      );
      this.decreaseSliderActionDestructor = addMoveSliderInputAction(inputActions.decrease, -1);
      this.increaseSliderActionDestructor = addMoveSliderInputAction(inputActions.increase, 1);
      const releasedInputTypeOptions = { inputType: "released" };
      const onMoveSliderInputActionReleased = () => {
        var _a, _b;
        if (isScrolling) {
          (_b = (_a = this.props).onFocusOut) == null ? void 0 : _b.call(_a);
          this.isButtonSelected.set(false);
          isScrolling = false;
        }
        return false;
      };
      this.decreaseSliderActionReleasedDestructor = this.inputManager.addInputAction(
        inputActions.decrease,
        onMoveSliderInputActionReleased,
        releasedInputTypeOptions
      );
      this.increaseSliderActionReleasedDestructor = this.inputManager.addInputAction(
        inputActions.increase,
        onMoveSliderInputActionReleased,
        releasedInputTypeOptions
      );
    }
  }
  destroy() {
    var _a, _b, _c, _d;
    (_a = this.decreaseSliderActionDestructor) == null ? void 0 : _a.call(this);
    (_b = this.increaseSliderActionDestructor) == null ? void 0 : _b.call(this);
    (_c = this.decreaseSliderActionReleasedDestructor) == null ? void 0 : _c.call(this);
    (_d = this.increaseSliderActionReleasedDestructor) == null ? void 0 : _d.call(this);
    this.isButtonHoverable.destroy();
    this.subs.forEach((sub) => sub.destroy());
    super.destroy();
  }
}
class Switch extends GamepadUiComponent {
  constructor() {
    var _a;
    super(...arguments);
    this.turnOnDirection = (_a = this.props.turnOnDirection) != null ? _a : "right";
    this.checked = SubscribableUtils.isMutableSubscribable(this.props.checked) ? this.props.checked : Subject.create(!!this.props.checked);
    this.turnOnDirectionSub = this.turnOnDirection === "right" ? this.checked : this.checked.map(SubscribableMapFunctions.not());
    this.sliderRef = FSComponent.createRef();
  }
  onAfterRender(node) {
    super.onAfterRender(node);
    this.gamepadUiComponentRef.instance.onclick = () => {
      if (SubscribableUtils.toSubscribable(this.props.disabled, true).get() === true) {
        return;
      }
      this.checked.set(!this.checked.get());
    };
    this.checked.sub((checked) => {
      if (this.props.callback) this.props.callback(checked);
    });
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent("div", { class: { "switch-container": true, checked: this.checked }, ref: this.gamepadUiComponentRef }, /* @__PURE__ */ FSComponent.buildComponent("div", { class: { slider: true, "slider--right": this.turnOnDirectionSub }, ref: this.sliderRef }, /* @__PURE__ */ FSComponent.buildComponent("div", { class: "disabled-layer" })));
  }
}
class Tag extends GamepadUiComponent {
  constructor() {
    super(...arguments);
    this.closeButtonRef = FSComponent.createRef();
  }
  renderButton() {
    if (this.props.iconPath === void 0) {
      return null;
    }
    return /* @__PURE__ */ FSComponent.buildComponent(
      IconButton,
      {
        class: "tag-button",
        iconPath: this.props.iconPath,
        callback: this.props.onButtonClick || this.props.callback,
        hoverable: this.props.hoverable,
        selected: this.props.selected,
        disabled: this.props.disabled
      }
    );
  }
  render() {
    var _a;
    return /* @__PURE__ */ FSComponent.buildComponent(
      "div",
      {
        ref: this.gamepadUiComponentRef,
        class: mergeClassProp("tag-container", this.props.class, {
          hide: (_a = this.props.visible) != null ? _a : false
        }),
        style: this.props.style
      },
      /* @__PURE__ */ FSComponent.buildComponent(
        Marquee,
        {
          class: "title light-text",
          speed: this.props.speed,
          delay: this.props.delay,
          startDelay: this.props.startDelay,
          resetDelay: this.props.resetDelay,
          activator: this.props.activator
        },
        /* @__PURE__ */ FSComponent.buildComponent(
          TT,
          {
            key: this.props.title,
            type: this.props.type,
            format: this.props.format,
            arguments: this.props.arguments
          }
        )
      ),
      this.renderButton()
    );
  }
}
class TextArea extends GamepadUiComponent {
  constructor() {
    var _a, _b;
    super(...arguments);
    this.uuid = UUID.GenerateUuid();
    this.textAreaRef = this.gamepadUiComponentRef;
    this.model = this.props.model || Subject.create(SubscribableUtils.toSubscribable(this.props.value || "", true).get());
    this.dispatchFocusOutEvent = this._dispatchFocusOutEvent.bind(this);
    this.setValueFromOS = this._setValueFromOS.bind(this);
    this._onKeyPress = this.onKeyPress.bind(this);
    this._onInput = this.onInput.bind(this);
    this.reloadLocalisation = this._reloadLocalisation.bind(this);
    this._isFocused = Subject.create(false);
    this.isFocused = this._isFocused;
    this.placeholderKey = SubscribableUtils.toSubscribable((_a = this.props.placeholder) != null ? _a : "", true);
    this.placeholderShown = Subject.create(true);
    this.placeholderTranslation = Subject.create(this.placeholderKey.get());
    this.hidePlaceholderOnFocus = (_b = this.props.hidePlaceholderOnFocus) != null ? _b : false;
    this.subs = [];
  }
  _reloadLocalisation() {
    this.placeholderTranslation.notify();
  }
  onKeyPress(event) {
    var _a, _b;
    const keyCode = event.keyCode || event.which;
    (_b = (_a = this.props).onKeyPress) == null ? void 0 : _b.call(_a, event);
    if (event.defaultPrevented) {
      return;
    }
    if (this.props.charFilter && !this.props.charFilter(String.fromCharCode(keyCode))) {
      event.preventDefault();
      return;
    }
  }
  onInput() {
    const value2 = this.textAreaRef.instance.value;
    if (value2 === this.model.get()) {
      return;
    }
    this.model.set(value2);
  }
  onInputUpdated(value2) {
    var _a, _b;
    this.textAreaRef.instance.value = value2;
    (_b = (_a = this.props).onInput) == null ? void 0 : _b.call(_a, this.textAreaRef.instance);
    if (!this.hidePlaceholderOnFocus && value2.length === 0) {
      this.placeholderShown.set(true);
    }
  }
  onFocusIn() {
    var _a, _b;
    (_b = (_a = this.props).onFocusIn) == null ? void 0 : _b.call(_a);
    if (this.hidePlaceholderOnFocus && this.textAreaRef.instance.value.length === 0) {
      this.placeholderShown.set(false);
    }
  }
  onFocusOut() {
    var _a, _b;
    (_b = (_a = this.props).onFocusOut) == null ? void 0 : _b.call(_a);
    if (this.hidePlaceholderOnFocus && this.textAreaRef.instance.value.length === 0) {
      this.placeholderShown.set(true);
    }
  }
  focus() {
    this.textAreaRef.instance.focus();
  }
  blur() {
    this.textAreaRef.instance.blur();
  }
  value() {
    return this.model.get();
  }
  clearInput() {
    this.model.set("");
  }
  _dispatchFocusOutEvent() {
    this.textAreaRef.instance.blur();
  }
  _setValueFromOS(text) {
    this.textAreaRef.instance.value = text;
    this.textAreaRef.instance.dispatchEvent(new InputEvent("change"));
    this.textAreaRef.instance.dispatchEvent(new InputEvent("input"));
    this.textAreaRef.instance.dispatchEvent(new InputEvent("focusout"));
    this.textAreaRef.instance.blur();
  }
  getTranslation(key) {
    var _a;
    let translatedKey = key.startsWith("@") || key.startsWith("TT") ? Utils.Translate(key) : key;
    (_a = this.props.placeholderArguments) == null ? void 0 : _a.forEach((argumentValue, argumentKey) => {
      const argumentValueSub = SubscribableUtils.toSubscribable(argumentValue, true);
      translatedKey = translatedKey.replace(argumentKey, argumentValueSub.get());
    });
    switch (this.props.placeholderFormat) {
      case "upper":
      case "uppercase":
        return translatedKey.toUpperCase();
      case "lower":
      case "lowercase":
        return translatedKey.toLowerCase();
      case "ucfirst":
        return `${translatedKey.charAt(0).toUpperCase()}${translatedKey.slice(1)}`;
      case "capitalize":
        return translatedKey.replace(/(^\w{1})|(\s+\w{1})/g, (letter) => letter.toUpperCase());
      default:
        return translatedKey;
    }
  }
  render() {
    var _a;
    return /* @__PURE__ */ FSComponent.buildComponent(
      "textarea",
      {
        id: this.uuid,
        class: "textarea",
        ref: this.textAreaRef,
        placeholder: MappedSubject.create(
          ([placeholderShown, placeholderKey]) => {
            return placeholderShown ? this.getTranslation(placeholderKey) : "";
          },
          this.placeholderShown,
          this.placeholderKey
        ),
        disabled: this.props.disabled,
        value: SubscribableUtils.toSubscribable(this.props.model || this.props.value || "", true).get(),
        rows: (_a = this.props.rows) != null ? _a : 4
      }
    );
  }
  onAfterRender(node) {
    super.onAfterRender(node);
    this.subs.push(
      this.model.sub((value2) => {
        this.onInputUpdated(value2);
      }, true),
      this.placeholderKey.sub((key) => {
        this.placeholderTranslation.set(key);
      }, true)
    );
    this.textAreaRef.instance.addEventListener("focus", () => {
      if (this._isFocused.get()) {
        return;
      }
      this._isFocused.set(true);
      Coherent.trigger("FOCUS_INPUT_FIELD", this.uuid, "", "", "", false);
      Coherent.on("mousePressOutsideView", this.dispatchFocusOutEvent);
      Coherent.on("SetInputTextFromOS", this.setValueFromOS);
      this.onFocusIn();
    });
    this.textAreaRef.instance.addEventListener("focusout", () => {
      if (!this._isFocused.get()) {
        return;
      }
      this._isFocused.set(false);
      Coherent.trigger("UNFOCUS_INPUT_FIELD", this.uuid);
      Coherent.off("mousePressOutsideView", this.dispatchFocusOutEvent);
      Coherent.off("SetInputTextFromOS", this.setValueFromOS);
      this.onFocusOut();
    });
    this.textAreaRef.instance.addEventListener("input", this._onInput);
    this.textAreaRef.instance.addEventListener("keypress", this._onKeyPress);
    Coherent.on("RELOAD_LOCALISATION", this.reloadLocalisation);
    if (this.props.focusOnInit) {
      this.focus();
    }
  }
  destroy() {
    this.subs.forEach((s) => s.destroy());
    if (this._isFocused.get()) {
      Coherent.trigger("UNFOCUS_INPUT_FIELD", this.uuid);
      Coherent.off("mousePressOutsideView", this.dispatchFocusOutEvent);
    }
    this.textAreaRef.instance.removeEventListener("keypress", this._onKeyPress);
    this.textAreaRef.instance.removeEventListener("input", this._onInput);
    Coherent.off("RELOAD_LOCALISATION", this.reloadLocalisation);
    super.destroy();
  }
}
class Timer extends DisplayComponent {
  constructor() {
    var _a;
    super(...arguments);
    this.displayedTime = this.props.displayedTimeFormatter ? this.props.stopwatch.timerSeconds.map(this.props.displayedTimeFormatter) : this.props.stopwatch.timerSeconds.map((timeSeconds) => {
      const date = /* @__PURE__ */ new Date(0);
      date.setSeconds(timeSeconds);
      return `${date.toISOString().substring(11, 19)}`;
    });
    this.showDotIndicator = SubscribableUtils.toSubscribable(
      (_a = this.props.showDotIndicator) != null ? _a : false,
      true
    );
    this.hideDotIndicator = this.showDotIndicator.map(SubscribableMapFunctions.not());
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent("div", { class: "timer" }, /* @__PURE__ */ FSComponent.buildComponent(
      "div",
      {
        class: {
          dot: true,
          hide: this.hideDotIndicator
        }
      }
    ), /* @__PURE__ */ FSComponent.buildComponent("div", { class: "timer-text" }, this.displayedTime));
  }
  destroy() {
    this.displayedTime.destroy();
    this.hideDotIndicator.destroy();
    super.destroy();
  }
}
class Tooltip extends GamepadUiComponent {
  constructor() {
    var _a;
    super(...arguments);
    this.forceHide = (_a = this.props.condition) != null ? _a : false;
  }
  render() {
    var _a;
    const position = (_a = this.props.position) != null ? _a : "bottom";
    const content = value(this.props.content);
    return /* @__PURE__ */ FSComponent.buildComponent("div", { class: "tooltip-component", ref: this.gamepadUiComponentRef }, this.props.children, /* @__PURE__ */ FSComponent.buildComponent("div", { class: { tooltip: true, forceHide: this.forceHide, [position]: true } }, !SubscribableUtils.isSubscribable(content) && typeof content !== "string" && isVNode(content) ? content : /* @__PURE__ */ FSComponent.buildComponent(TT, { class: "tooltip-description", key: content, format: "ucfirst" }), /* @__PURE__ */ FSComponent.buildComponent("svg", { class: "pointer-svg" }, /* @__PURE__ */ FSComponent.buildComponent("path", { d: "M 0 9 l10 -8 l10 8", fill: "rgb(15, 20, 27)", stroke: "rgb(63, 67, 73)", "stroke-width": "2" }))));
  }
}
class NotificationManager {
  constructor(bus) {
    this.bus = bus;
    this.allowNotification = true;
    this.isNotificationAppOpen = false;
    this.pendingNotifications = [];
    this._shownNotifications = ArraySubject.create([]);
    this.shownNotifications = this._shownNotifications;
    this._storedNotifications = ArraySubject.create([]);
    this.storedNotifications = this._storedNotifications;
    this._unseenNotificationsCount = Subject.create(0);
    this.unseenNotificationsCount = this._unseenNotificationsCount;
    this.maxShownItems = Subject.create(1);
    this.timeBetweenNotifsMs = 500;
    this.subs = [];
    this.subs.push(
      this._shownNotifications.sub(this.onShownNotifsUpdate.bind(this)),
      this._storedNotifications.sub(this.onStoredNotifsUpdate.bind(this))
    );
  }
  static getManager(bus) {
    var _a;
    return (_a = NotificationManager.INSTANCE) != null ? _a : NotificationManager.INSTANCE = new NotificationManager(bus);
  }
  /**
   * internaly called by efb os
   * @internal
   */
  update() {
    if (!this.allowNotification) {
      return;
    }
    if (this._shownNotifications.length >= this.maxShownItems.get() || this.pendingNotifications.length === 0) {
      return;
    }
    const notifToShow = this.pendingNotifications.shift();
    if (!notifToShow) {
      return;
    }
    this._shownNotifications.insert(notifToShow);
  }
  onShownNotifsUpdate(_index, eventType, notif) {
    if (eventType !== SubscribableArrayEventType.Added || notif === void 0) {
      return;
    }
    const notifs = Array.isArray(notif) ? notif : [notif];
    notifs.forEach((notifToDelete) => {
      const notifDelay = notifToDelete.delayMs;
      setTimeout(() => {
        notifToDelete.hide.set(true);
      }, notifDelay);
      setTimeout(() => {
        this._shownNotifications.removeItem(notifToDelete);
      }, notifToDelete.delayMs + this.timeBetweenNotifsMs);
    });
  }
  onStoredNotifsUpdate(_i, _t, _n, arr) {
    var _a;
    this._unseenNotificationsCount.set(
      (_a = arr == null ? void 0 : arr.reduce((accumulator, notif) => accumulator + (notif.viewed.get() ? 0 : 1), 0)) != null ? _a : 0
    );
  }
  /**
   * Adds a notification to the stack
   * @param notif the notification to add, could be a TemporaryNotification or a PermanentNotification
   */
  addNotification(notif) {
    notif.createdAt = /* @__PURE__ */ new Date();
    if (isNotificationPermanent(notif)) {
      this._storedNotifications.insert(notif);
      if (this.isNotificationAppOpen) {
        return;
      }
    }
    this.pendingNotifications.push(notif);
  }
  /**
   * Removes a permanent notification from the notification menu
   * @param notif the notification to remove
   */
  deletePermanentNotification(notif) {
    this._storedNotifications.removeItem(notif);
  }
  /* Removes all permanent notifications from the notification menu */
  clearNotifications() {
    this._storedNotifications.clear();
  }
  onNotificationAppOpen() {
    this.isNotificationAppOpen = true;
    this.pendingNotifications = this.pendingNotifications.filter(
      (notif) => !isNotificationPermanent(notif)
    );
    this._shownNotifications.getArray().forEach((notif) => {
      if (isNotificationPermanent(notif)) {
        this._shownNotifications.removeItem(notif);
      }
    });
  }
  onNotificationAppClosed() {
    this.isNotificationAppOpen = false;
    for (let i = 0; i < this._storedNotifications.length; i++) {
      this._storedNotifications.get(i).viewed.set(true);
    }
    this._unseenNotificationsCount.set(0);
  }
}
const _OnboardingManager = class _OnboardingManager {
  constructor() {
    this.isStarted = false;
    this.stepIndex = 0;
    this.steps = [];
  }
  static getManager() {
    var _a;
    return (_a = _OnboardingManager.INSTANCE) != null ? _a : _OnboardingManager.INSTANCE = new _OnboardingManager();
  }
  bindContainer(containerRef) {
    _OnboardingManager.getManager().containerRef = containerRef;
  }
  start(onboarding) {
    const onboardingManager = _OnboardingManager.getManager();
    onboardingManager.steps = onboarding.steps.map((step) => {
      return __spreadProps(__spreadValues({}, step), { actions: [...step.actions] });
    });
    if (!onboardingManager.containerRef) {
      console.warn("Onboarding container not bound");
      return;
    }
    if (!onboardingManager.steps.length) {
      console.warn("No onboarding steps provided");
      return;
    }
    if (onboardingManager.isStarted) {
      console.warn("Onboarding already started");
      return;
    }
    onboardingManager.stepIndex = 0;
    onboardingManager.isStarted = true;
    onboardingManager.onFinish = onboarding.onFinish;
    onboardingManager.steps[onboardingManager.stepIndex].actions.unshift(
      {
        key: "@fs-base-efb,TT:EFB.COMMON.QUICK_TOUR_AROUND",
        callback: () => {
          onboardingManager.next();
        }
      },
      {
        key: "@fs-base-efb,TT:EFB.COMMON.SKIP",
        callback: () => {
          onboardingManager.stop();
        }
      }
    );
    onboardingManager.containerRef.instance.show();
    onboardingManager.containerRef.instance.setStep(onboardingManager.steps[onboardingManager.stepIndex]);
    onboardingManager.stepIndex++;
    return;
  }
  next() {
    var _a;
    const onboardingManager = _OnboardingManager.getManager();
    if (!onboardingManager.isStarted) {
      console.warn("Onboarding not started. You should call start method first");
      return;
    }
    if (onboardingManager.stepIndex >= onboardingManager.steps.length) {
      onboardingManager.stop();
      return;
    }
    onboardingManager.steps[onboardingManager.stepIndex].actions.unshift({
      key: "@fs-base-efb,TT:EFB.COMMON.NEXT",
      callback: () => {
        onboardingManager.next();
      }
    });
    (_a = onboardingManager.containerRef) == null ? void 0 : _a.instance.setStep(onboardingManager.steps[onboardingManager.stepIndex]);
    onboardingManager.stepIndex++;
    return;
  }
  stop() {
    var _a, _b;
    const onboardingManager = _OnboardingManager.getManager();
    onboardingManager.isStarted = false;
    onboardingManager.steps = [];
    (_a = onboardingManager.containerRef) == null ? void 0 : _a.instance.hide();
    (_b = onboardingManager.onFinish) == null ? void 0 : _b.call(onboardingManager);
  }
};
_OnboardingManager.INSTANCE = void 0;
let OnboardingManager = _OnboardingManager;
const _EfbCommonSettingsManager = class _EfbCommonSettingsManager extends DefaultUserSettingManager {
  constructor() {
    super(...arguments);
    this.debounceTimer = new DebounceTimer();
    this.ignoredForceSaveSettingKeys = [];
  }
  /** @inheritdoc */
  onSettingValueChanged(entry, value2) {
    super.onSettingValueChanged(entry, value2);
    if (!this.ignoredForceSaveSettingKeys.includes(entry.setting.definition.name)) {
      this.debounceTimer.schedule(() => {
        Coherent.trigger("EFB_PERSISTENT_SETTINGS_SAVE");
      }, _EfbCommonSettingsManager.debounceTimeout);
    }
  }
};
_EfbCommonSettingsManager.debounceTimeout = 5e3;
let EfbCommonSettingsManager = _EfbCommonSettingsManager;
var UnitsNavAngleSettingMode = /* @__PURE__ */ ((UnitsNavAngleSettingMode2) => {
  UnitsNavAngleSettingMode2["Magnetic"] = "magnetic";
  UnitsNavAngleSettingMode2["True"] = "true";
  return UnitsNavAngleSettingMode2;
})(UnitsNavAngleSettingMode || {});
var UnitsSpeedSettingMode = /* @__PURE__ */ ((UnitsSpeedSettingMode2) => {
  UnitsSpeedSettingMode2["Nautical"] = "KTS";
  UnitsSpeedSettingMode2["Metric"] = "KPH";
  return UnitsSpeedSettingMode2;
})(UnitsSpeedSettingMode || {});
var UnitsDistanceSettingMode = /* @__PURE__ */ ((UnitsDistanceSettingMode2) => {
  UnitsDistanceSettingMode2["Nautical"] = "NM";
  UnitsDistanceSettingMode2["Metric"] = "KM";
  return UnitsDistanceSettingMode2;
})(UnitsDistanceSettingMode || {});
var UnitsSmallDistanceSettingMode = /* @__PURE__ */ ((UnitsSmallDistanceSettingMode2) => {
  UnitsSmallDistanceSettingMode2["Feet"] = "FT";
  UnitsSmallDistanceSettingMode2["Meters"] = "M";
  return UnitsSmallDistanceSettingMode2;
})(UnitsSmallDistanceSettingMode || {});
var UnitsAltitudeSettingMode = /* @__PURE__ */ ((UnitsAltitudeSettingMode2) => {
  UnitsAltitudeSettingMode2["Feet"] = "FT";
  UnitsAltitudeSettingMode2["Meters"] = "M";
  return UnitsAltitudeSettingMode2;
})(UnitsAltitudeSettingMode || {});
var UnitsWeightSettingMode = /* @__PURE__ */ ((UnitsWeightSettingMode2) => {
  UnitsWeightSettingMode2["Pounds"] = "LBS";
  UnitsWeightSettingMode2["Kilograms"] = "KG";
  return UnitsWeightSettingMode2;
})(UnitsWeightSettingMode || {});
var UnitsVolumeSettingMode = /* @__PURE__ */ ((UnitsVolumeSettingMode2) => {
  UnitsVolumeSettingMode2["Gallons"] = "GAL US";
  UnitsVolumeSettingMode2["Liters"] = "L";
  return UnitsVolumeSettingMode2;
})(UnitsVolumeSettingMode || {});
var UnitsTemperatureSettingMode = /* @__PURE__ */ ((UnitsTemperatureSettingMode2) => {
  UnitsTemperatureSettingMode2["Fahrenheit"] = "°F";
  UnitsTemperatureSettingMode2["Celsius"] = "°C";
  return UnitsTemperatureSettingMode2;
})(UnitsTemperatureSettingMode || {});
var UnitsTimeSettingMode = /* @__PURE__ */ ((UnitsTimeSettingMode2) => {
  UnitsTimeSettingMode2["Local12"] = "local-12";
  UnitsTimeSettingMode2["Local24"] = "local-24";
  return UnitsTimeSettingMode2;
})(UnitsTimeSettingMode || {});
const _UnitsSettingsManager = class _UnitsSettingsManager extends EfbCommonSettingsManager {
  constructor(bus, settingsDefs) {
    super(bus, settingsDefs, true);
    this.navAngleUnitsSub = Subject.create(_UnitsSettingsManager.MAGNETIC_BEARING);
    this.navAngleUnits = this.navAngleUnitsSub;
    this.timeUnitsSub = Subject.create(
      "local-12"
      /* Local12 */
    );
    this._timeUnits = this.timeUnitsSub;
    this.areSubscribablesInit = false;
    this.areSubscribablesInit = true;
    for (const entry of this.settings.values()) {
      this.updateUnitsSubjects(entry.setting.definition.name, entry.setting.value);
    }
  }
  onSettingValueChanged(entry, value2) {
    if (this.areSubscribablesInit) {
      this.updateUnitsSubjects(entry.setting.definition.name, value2);
    }
    super.onSettingValueChanged(entry, value2);
  }
  /**
   * Checks if the values loaded from the datastorage correspond to the settings types.
   */
  checkLoadedValues() {
    checkUserSetting(this.getSetting("unitsNavAngle"), UnitsNavAngleSettingMode);
    checkUserSetting(this.getSetting("unitsSpeed"), UnitsSpeedSettingMode);
    checkUserSetting(this.getSetting("unitsDistance"), UnitsDistanceSettingMode);
    checkUserSetting(this.getSetting("unitsAltitude"), UnitsAltitudeSettingMode);
    checkUserSetting(this.getSetting("unitsSmallDistance"), UnitsSmallDistanceSettingMode);
    checkUserSetting(this.getSetting("unitsWeight"), UnitsWeightSettingMode);
    checkUserSetting(this.getSetting("unitsVolume"), UnitsVolumeSettingMode);
    checkUserSetting(this.getSetting("unitsTemperature"), UnitsTemperatureSettingMode);
    checkUserSetting(this.getSetting("unitsTime"), UnitsTimeSettingMode);
  }
  updateUnitsSubjects(settingName, value2) {
    switch (settingName) {
      case "unitsNavAngle":
        this.navAngleUnitsSub.set(
          value2 === "true" ? _UnitsSettingsManager.TRUE_BEARING : _UnitsSettingsManager.MAGNETIC_BEARING
        );
        break;
      case "unitsTime":
        this.timeUnitsSub.set(value2);
        break;
      case "unitsDistance":
        this.getSetting("unitsSmallDistance").set(
          value2 === "NM" ? "FT" : "M"
          /* Meters */
        );
    }
  }
  /**
   * Gets a unit type subscribable from a unit setting name
   * @param settingName the name of the unit setting
   * @returns a Subscribable containing the unit type. If the value in the dataStorage is unvalid, it returns the default unitType
   */
  getSettingUnitType(settingName) {
    const setting = this.getSetting(settingName);
    return setting.map(
      (settingValue) => {
        var _a;
        return (_a = UnitTypesMap[settingValue]) != null ? _a : UnitTypesMap[setting.definition.defaultValue];
      }
    );
  }
};
_UnitsSettingsManager.TRUE_BEARING = BasicNavAngleUnit.create(false);
_UnitsSettingsManager.MAGNETIC_BEARING = BasicNavAngleUnit.create(true);
let UnitsSettingsManager = _UnitsSettingsManager;
const UnitTypesMap = {
  /** Mapped speed unit types */
  KTS: UnitType.KNOT,
  KPH: UnitType.KPH,
  /** Mapped distance unit type */
  NM: UnitType.NMILE,
  KM: UnitType.KILOMETER,
  /** Mapped altitude unit type */
  FT: UnitType.FOOT,
  M: UnitType.METER,
  /** Mapped weight unit type */
  LBS: UnitType.POUND,
  KG: UnitType.KILOGRAM,
  /** Mapped volume unit type */
  "GAL US": UnitType.GALLON,
  L: UnitType.LITER,
  /** Mapped temperature unit type */
  "°C": UnitType.CELSIUS,
  "°F": UnitType.FAHRENHEIT
};
class UnitsSettings {
  /**
   * Retrieves a manager for display units settings.
   * @param bus The event bus.
   * @returns a manager for display units settings.
   */
  static getManager(bus) {
    var _a;
    return (_a = UnitsSettings.INSTANCE) != null ? _a : UnitsSettings.INSTANCE = new UnitsSettingsManager(bus, [
      {
        name: "unitsNavAngle",
        defaultValue: UnitsNavAngleSettingMode.Magnetic
      },
      {
        name: "unitsSpeed",
        defaultValue: UnitsSpeedSettingMode.Nautical
      },
      {
        name: "unitsDistance",
        defaultValue: UnitsDistanceSettingMode.Nautical
      },
      {
        name: "unitsSmallDistance",
        defaultValue: UnitsSmallDistanceSettingMode.Feet
      },
      {
        name: "unitsAltitude",
        defaultValue: UnitsAltitudeSettingMode.Feet
      },
      {
        name: "unitsWeight",
        defaultValue: UnitsWeightSettingMode.Pounds
      },
      {
        name: "unitsVolume",
        defaultValue: UnitsVolumeSettingMode.Gallons
      },
      {
        name: "unitsTemperature",
        defaultValue: UnitsTemperatureSettingMode.Celsius
      },
      {
        name: "unitsTime",
        defaultValue: UnitsTimeSettingMode.Local12
      }
    ]);
  }
}
var EfbMode = /* @__PURE__ */ ((EfbMode2) => {
  EfbMode2[EfbMode2["2D"] = 0] = "2D";
  EfbMode2[EfbMode2["3D"] = 1] = "3D";
  return EfbMode2;
})(EfbMode || {});
var EfbSizeSettingMode = /* @__PURE__ */ ((EfbSizeSettingMode2) => {
  EfbSizeSettingMode2[EfbSizeSettingMode2["Small"] = 0] = "Small";
  EfbSizeSettingMode2[EfbSizeSettingMode2["Medium"] = 1] = "Medium";
  EfbSizeSettingMode2[EfbSizeSettingMode2["Large"] = 2] = "Large";
  return EfbSizeSettingMode2;
})(EfbSizeSettingMode || {});
var OrientationSettingMode = /* @__PURE__ */ ((OrientationSettingMode2) => {
  OrientationSettingMode2[OrientationSettingMode2["Vertical"] = 0] = "Vertical";
  OrientationSettingMode2[OrientationSettingMode2["Horizontal"] = 1] = "Horizontal";
  return OrientationSettingMode2;
})(OrientationSettingMode || {});
class EfbSettingsManager extends EfbCommonSettingsManager {
  constructor(bus, settingsDefs) {
    super(bus, settingsDefs);
    this.ignoredForceSaveSettingKeys = ["mode", "autoBrightnessPercentage"];
    let stringToArray = [];
    try {
      stringToArray = JSON.parse(this.getSetting("favoriteApps").get());
    } catch (error) {
      console.error("JSON failed, impossible to parse : ", this.getSetting("favoriteApps").get());
      stringToArray = ["AtlasApp", "AircraftApp", "PilotBookApp", "SettingsApp"];
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
    checkUserSetting(this.getSetting("efbSize"), EfbSizeSettingMode);
    checkUserSetting(this.getSetting("orientationMode"), OrientationSettingMode);
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
    const index2 = this.favoriteApps.indexOf(app.internalName);
    if (index2 !== -1) {
      this.favoriteApps.splice(index2, 1);
    }
    this.onFavoriteAppsUpdated();
    return this;
  }
  /**
   * Update the favoriteSetting and the apps array in the EFB instance in order to rerender when the favorite apps change
   */
  onFavoriteAppsUpdated() {
    const stringSetting = JSON.stringify(this.favoriteApps);
    this.getSetting("favoriteApps").set(stringSetting);
    Efb.apps().getArray().forEach((app) => {
      app.favoriteIndex = this.favoriteApps.indexOf(app.internalName);
    });
    this.bus.pub("favs-update", "");
  }
  updateFavoriteAppsArray(value2) {
    let arraySetting = [];
    try {
      arraySetting = JSON.parse(value2);
    } catch (error) {
      console.error("JSON failed, impossible to parse : ", value2);
      arraySetting = ["AtlasApp", "AircraftApp", "PilotBookApp", "SettingsApp"];
    }
    this.favoriteApps = arraySetting;
  }
  onSettingValueChanged(entry, value2) {
    super.onSettingValueChanged(entry, value2);
    const settingName = entry.setting.definition.name;
    switch (settingName) {
      case "efbSize":
        if (typeof value2 === "string") {
          value2 = EfbSizeSettingMode[value2.toString()];
        }
        Coherent.call("SET_SIZE", value2);
        break;
      case "orientationMode":
        if (typeof value2 === "string") {
          value2 = OrientationSettingMode[value2.toString()];
        }
        Coherent.call("SET_ORIENTATION", value2);
        break;
      case "isBrightnessAuto":
        if (typeof value2 !== "boolean") {
          break;
        }
        if (value2 === true) {
          this.getSetting("manualBrightnessPercentage").set(
            this.getSetting("autoBrightnessPercentage").get()
          );
        }
        Coherent.call("SET_IS_AUTO_BRIGHTNESS", this.getSetting("isBrightnessAuto").get());
        break;
      case "manualBrightnessPercentage":
        if (this.getSetting("isBrightnessAuto").get()) {
          break;
        }
        if (typeof value2 !== "number") {
          break;
        }
        Coherent.call("SET_MANUAL_BRIGHTNESS", value2 / 100);
        break;
      case "favoriteApps":
        this.updateFavoriteAppsArray(value2.toString());
        break;
    }
  }
}
class EfbSettings {
  constructor() {
  }
  static getManager(bus) {
    var _a;
    return (_a = EfbSettings.INSTANCE) != null ? _a : EfbSettings.INSTANCE = new EfbSettingsManager(bus, [
      {
        name: "mode",
        defaultValue: 0
        /* 2D */
      },
      {
        name: "efbSize",
        defaultValue: 0
        /* Small */
      },
      {
        name: "orientationMode",
        defaultValue: 0
        /* Vertical */
      },
      {
        name: "isBrightnessAuto",
        defaultValue: true
      },
      {
        name: "autoBrightnessPercentage",
        defaultValue: 50
      },
      {
        name: "manualBrightnessPercentage",
        defaultValue: 50
      },
      {
        name: "favoriteApps",
        defaultValue: '["AtlasApp","AircraftApp","PilotBookApp","SettingsApp"]'
      },
      {
        name: "defaultApp",
        defaultValue: ""
      }
    ]);
  }
}
class EfbSettingsSaveManager extends UserSettingSaveManager {
  constructor(bus) {
    const settings = [
      ...EfbSettings.getManager(bus).getAllSettings(),
      ...UnitsSettings.getManager(bus).getAllSettings()
    ];
    super(settings, bus);
    this.bus = bus;
    this.prefix = "efb-core-settings.2025-04-01.";
    this.oldPrefixes = ["efb-2024-11-07.", "efb-2024-08-28."];
    this.settings = [];
    this.settings.push(...settings);
  }
  load(key) {
    super.load(`${this.prefix}${key}`);
    UnitsSettings.getManager(this.bus).checkLoadedValues();
    EfbSettings.getManager(this.bus).checkLoadedValues();
    this.save(key);
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
    for (const prefix of this.oldPrefixes) {
      const storage = GetDataStorage().searchData(prefix);
      for (const entry of storage) {
        if (entry.key.startsWith(`persistent-setting.${this.prefix}`)) {
          GetDataStorage().deleteData(entry.key);
        }
      }
    }
  }
}
class ViewStackContainer extends DisplayComponent {
  constructor() {
    super(...arguments);
    this.rootRef = FSComponent.createRef();
  }
  renderView(view) {
    FSComponent.render(view, this.rootRef.instance);
  }
  render() {
    var _a;
    return /* @__PURE__ */ FSComponent.buildComponent(
      "div",
      {
        ref: this.rootRef,
        class: mergeClassProp("view-stack", this.props.class),
        id: (_a = this.props.id) != null ? _a : ""
      }
    );
  }
}
class ViewWrapper extends DisplayComponent {
  constructor() {
    super(...arguments);
    this.rootRef = FSComponent.createRef();
    this.classList = SetSubject.create(["view", "hidden", this.props.viewName]);
    this.subs = [];
  }
  onAfterRender(node) {
    super.onAfterRender(node);
    this.subs.push(
      this.props.isActive.sub((isActive) => {
        if (isActive) {
          this.classList.delete("hidden");
        } else {
          this.classList.add("hidden");
        }
      })
    );
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent("div", { ref: this.rootRef, class: this.classList }, this.props.children);
  }
  destroy() {
    var _a;
    const root = this.rootRef.getOrDefault();
    if (root !== null) {
      (_a = root.parentNode) == null ? void 0 : _a.removeChild(root);
    }
    this.subs.forEach((x) => x.destroy());
    super.destroy();
  }
}
class ViewService {
  // eslint-disable-next-line @typescript-eslint/no-empty-function
  constructor(_viewKey, _appViewService) {
    this.registeredViews = MapSubject.create();
    this.viewTabs = [];
    this.visibleViewTabs = [];
    this.viewTabVisibilitySubscriptions = [];
    this.hasInitialized = false;
    this.activeViewEntry = Subject.create(null);
    this.internalInputStackManager = new InternalInputManager(InternalInputActions);
  }
  openNextVisibleTab(forward = true) {
    if (this.visibleViewTabs.length <= 1) {
      return false;
    }
    const activeTabIndex = this.visibleViewTabs.findIndex((tab) => tab.isActive.get());
    if (activeTabIndex === -1) {
      return false;
    }
    const direction = forward ? 1 : -1;
    const nextTabIndex = (activeTabIndex + this.visibleViewTabs.length + direction) % this.visibleViewTabs.length;
    this.openPage(this.visibleViewTabs[nextTabIndex].key);
    return true;
  }
  getRegisteredViews() {
    return this.registeredViews;
  }
  getViewEntry(key) {
    const viewEntry = this.registeredViews.get(key);
    if (viewEntry === void 0) {
      throw new Error(`View "${key}" doesn't exists`);
    }
    return viewEntry;
  }
  registerView(key, vNodeFactory) {
    if (this.registeredViews.has(key)) {
      throw new Error(`View "${key}" is already used`);
    }
    const isActive = Subject.create(false);
    const isDisabled = Subject.create(false);
    const isTabVisible = Subject.create(true);
    const isInit = false;
    const viewEntry = {
      key,
      render: vNodeFactory,
      vNode: null,
      containerRef: FSComponent.createRef(),
      ref: null,
      isActive,
      isDisabled,
      isTabVisible,
      isInit
    };
    this.initViewEntry(viewEntry);
    this.registeredViews.set(key, viewEntry);
    this.viewTabs.push(viewEntry);
    this.viewTabVisibilitySubscriptions.push(
      isTabVisible.sub(() => {
        this.visibleViewTabs = this.viewTabs.filter(
          (tab) => tab.isTabVisible.get() === true
        );
      })
    );
    return viewEntry;
  }
  onContainerRendered(viewRef) {
    this.viewRef = viewRef;
  }
  initViewEntry(entry) {
    var _a;
    if (entry.isInit) {
      return;
    }
    entry.isInit = true;
    entry.vNode = entry.render();
    entry.ref = entry.vNode.instance;
    (_a = this.viewRef) == null ? void 0 : _a.renderView(
      /* @__PURE__ */ FSComponent.buildComponent(ViewWrapper, { viewName: entry.key, isActive: entry.isActive, ref: entry.containerRef }, entry.vNode)
    );
    entry.ref.onOpen();
    entry.ref.onResume();
    entry.ref.onPause();
  }
  initialize(key) {
    var _a;
    if (this.hasInitialized) {
      return;
    }
    this.hasInitialized = true;
    if (key) {
      const initViewEntry = this.getViewEntry(key);
      (_a = initViewEntry.ref) == null ? void 0 : _a.onResume();
      initViewEntry.isActive.set(true);
      this.activeViewEntry.set(initViewEntry);
    }
  }
  openPage(key) {
    var _a, _b;
    const activeViewEntry = this.activeViewEntry.get();
    if ((activeViewEntry == null ? void 0 : activeViewEntry.key) === key) {
      return activeViewEntry;
    }
    activeViewEntry == null ? void 0 : activeViewEntry.isActive.set(false);
    (_a = activeViewEntry == null ? void 0 : activeViewEntry.ref) == null ? void 0 : _a.onPause();
    const newViewEntry = this.getViewEntry(key);
    newViewEntry.isActive.set(true);
    (_b = newViewEntry.ref) == null ? void 0 : _b.onResume();
    this.activeViewEntry.set(newViewEntry);
    return newViewEntry;
  }
  onPause() {
    var _a, _b, _c, _d;
    (_b = (_a = this.activeViewEntry.get()) == null ? void 0 : _a.ref) == null ? void 0 : _b.onPause();
    (_c = this.prevTabActionDestructor) == null ? void 0 : _c.call(this);
    (_d = this.nextTabActionDestructor) == null ? void 0 : _d.call(this);
  }
  onResume() {
    var _a, _b;
    (_b = (_a = this.activeViewEntry.get()) == null ? void 0 : _a.ref) == null ? void 0 : _b.onResume();
  }
  onUpdate(time) {
    var _a, _b;
    (_b = (_a = this.activeViewEntry.get()) == null ? void 0 : _a.ref) == null ? void 0 : _b.onUpdate(time);
  }
  destroy() {
    this.viewTabVisibilitySubscriptions.forEach((subscription) => subscription.destroy());
  }
}
class ViewServiceContainer extends DisplayComponent {
  constructor() {
    super(...arguments);
    this.stackContainerRef = FSComponent.createRef();
  }
  onAfterRender(node) {
    super.onAfterRender(node);
    this.props.viewService.onContainerRendered(this.stackContainerRef.instance);
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent(ViewStackContainer, { ref: this.stackContainerRef, class: this.props.class, id: this.props.id });
  }
}
class ViewServiceMenu extends GamepadUiComponent {
  constructor() {
    super(...arguments);
    this.tabs = [];
  }
  tabRender(tab) {
    return /* @__PURE__ */ FSComponent.buildComponent(
      TabSelector,
      {
        ref: tab.tabRef,
        tabName: tab.view.ref.tabName,
        callback: () => {
          this.props.viewService.openPage(tab.view.key);
        },
        active: tab.view.isActive,
        disabled: tab.view.isDisabled,
        hidden: tab.view.isTabVisible.map(SubscribableMapFunctions.not())
      }
    );
  }
  render() {
    return /* @__PURE__ */ FSComponent.buildComponent("div", { ref: this.gamepadUiComponentRef, class: mergeClassProp("tabs", this.props.class) });
  }
  onAfterRender(node) {
    super.onAfterRender(node);
    this.viewsSub = this.props.viewService.getRegisteredViews().sub((key, type, view) => {
      switch (type) {
        case SubscribableMapEventType.Added:
          {
            if (view === void 0 || !(view.ref instanceof UiView) || !view.ref.tabName) {
              return;
            }
            const tab = {
              key,
              tabRef: FSComponent.createRef(),
              view
            };
            FSComponent.render(this.tabRender(tab), this.gamepadUiComponentRef.instance);
            this.tabs.push(tab);
          }
          break;
        case SubscribableMapEventType.Removed:
          {
            const index2 = this.tabs.findIndex((el) => el.key === key);
            if (index2 === -1) {
              return;
            }
            const removed = this.tabs.splice(index2, 1)[0];
            removed.tabRef.instance.destroy();
            const child = this.gamepadUiComponentRef.instance.childNodes.item(index2);
            this.gamepadUiComponentRef.instance.removeChild(child);
          }
          break;
      }
    }, true);
  }
  destroy() {
    var _a;
    (_a = this.viewsSub) == null ? void 0 : _a.destroy();
    super.destroy();
  }
}
export {
  AbstractAccordion,
  AbstractButton,
  AirportSize,
  App,
  AppBootMode,
  AppContainer,
  AppSuspendMode,
  AppView,
  AppViewService,
  ArrayFilterSubject,
  BearingDisplay,
  Button,
  CircularProgress,
  ClassicButton,
  Container,
  DateDisplay,
  DropdownButton,
  Efb,
  EfbApiVersion,
  EfbMode,
  EfbSettings,
  EfbSettingsManager,
  EfbSettingsSaveManager,
  EfbSizeSettingMode,
  ElementAccordion,
  FlightPhaseManager,
  FlightPhaseState,
  GameMode,
  GameModeManager,
  GamepadEvents,
  GamepadInputManager,
  GamepadUiComponent,
  GamepadUiParser,
  GamepadUiView,
  GhostListItem,
  IconButton,
  IconElement,
  Incremental,
  InputAction,
  InputActions,
  InputManager,
  InputStackListener,
  InputsListener,
  LargeAirportThresholdFt,
  List,
  MapSubject,
  Marquee,
  MediumAirportThresholdFt,
  MultipleButtons,
  NotificationManager,
  NumberUnitDisplay,
  OnboardingManager,
  OrientationSettingMode,
  PagingList,
  ProgressBar,
  ProgressComponent,
  RunwaySelector,
  ScrollBar,
  SearchBar,
  SearchFacility,
  SelectableButton2 as SelectableButton,
  Slider,
  Stopwatch,
  StopwatchState,
  StringAccordion,
  SubscribableMapEventType,
  Switch,
  TT,
  TTButton,
  TabSelector,
  Tag,
  TextArea,
  TextBox,
  Timer,
  Tooltip,
  TypedButton,
  UiView,
  UnitBox,
  UnitFormatter,
  UnitsAltitudeSettingMode,
  UnitsBox,
  UnitsDistanceSettingMode,
  UnitsNavAngleSettingMode,
  UnitsSettings,
  UnitsSettingsManager,
  UnitsSmallDistanceSettingMode,
  UnitsSpeedSettingMode,
  UnitsTemperatureSettingMode,
  UnitsTimeSettingMode,
  UnitsVolumeSettingMode,
  UnitsWeightSettingMode,
  ViewBootMode,
  ViewService,
  ViewServiceContainer,
  ViewServiceMenu,
  ViewSuspendMode,
  ViewWrapper,
  basicFormatter,
  checkUserSetting,
  chunk,
  createCustomFacility,
  createPermanentNotif,
  createPermanentNotification,
  createTemporaryNotif,
  createTemporaryNotification,
  dayKeys,
  elementOffset,
  extractChunks,
  formatDay,
  getAirportSize,
  getCurrentRunwayName,
  getFacilityIconPath,
  getFacilityName,
  getICAOIdent,
  getLatLonStr,
  getRunwayName,
  getStartOfWeek,
  getWeeksInMonth,
  groupBy,
  isAirportFacility,
  isConstructor,
  isFunction,
  isIApp,
  isNotifPermanent,
  isNotificationPermanent,
  isPromise,
  isSelectedAirportFacility,
  isVNode,
  loadFileAsBlob,
  measure,
  mergeClassProp,
  monthKeys,
  monthShortKeys,
  offsetMousePosition,
  random,
  readMetadata,
  textDecode,
  toArray,
  toClassProp,
  toPromise,
  toString,
  unique,
  value,
  where,
  wrap
};
