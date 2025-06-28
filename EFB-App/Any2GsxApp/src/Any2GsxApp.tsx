import {
  App,
  AppBootMode,
  AppInstallProps,
  AppSuspendMode,
  AppView,
  AppViewProps,
  Efb,
  RequiredProps,
  TVNode,
} from "@efb/efb-api";
import { FSComponent, VNode } from "@microsoft/msfs-sdk";
import { CommBusService } from "./Services/CommBus";

import "./Any2GsxApp.scss";
import { MainPage } from "./Components/MainPage";

/**
 * BASE_URL is a global var defined in build.js
 * It points to the dist folder of the app when builded.
 * Mainly used to load assets (icons, fonts, etc)
 */
declare const BASE_URL: string;

class Any2GsxAppView extends AppView<RequiredProps<AppViewProps, "bus">> {
  /**
   * Optional property
   * Default view key to show if using AppViewService
   */
  protected defaultView = "MainPage";
  protected CommBus: CommBusService | undefined;

  /**
   * Optional method
   * Views (page or popup) to register if using AppViewService
   * Default behavior : nothing
   */
  protected registerViews(): void {
    this.appViewService.registerPage("MainPage", () => (
      <MainPage appViewService={this.appViewService} eventBus={this.bus} />
    ));
    this.CommBus = new CommBusService(this.bus);
    console.log("CommBusService registered");
  }

  /**
   * Optional method
   * Method called when AppView is open after it's creation
   * Default behavior : nothing
   */
  public onOpen(): void {
    //
  }

  /**
   * Optional method
   * Method called when AppView is closed
   * Default behavior : nothing
   */
  public onClose(): void {
    //
  }

  /**
   * Optional method
   * Method called when AppView is resumed (equivalent of onOpen but happen every time we go back to this app)
   * Default behavior : nothing
   */
  public onResume(): void {
    //
  }

  /**
   * Optional method
   * Method called when AppView is paused (equivalent of onClose but happen every time we switch to another app)
   * Default behavior : nothing
   */
  public onPause(): void {
    //
  }

  /**
   * Optional method
   * Default behavior is rendering AppContainer which works with AppViewService
   * We usually surround it with <div class="template-app">{super.render}</div>
   * Can render anything (JSX, Component) so it doesn't require to use AppViewService and/or AppContainer
   * @returns VNode
   */
  public render(): VNode {
    return <div class="any2gsx-app">{super.render()}</div>;
  }
}

class Any2GsxApp extends App {
  /**
   * Required getter for friendly app-name.
   * Used by the EFB as App's name shown to the user.
   * @returns string
   */
  public get name(): string {
    return "Any2GSX";
  }

  /**
   * Required getter for app's icon url.
   * Used by the EFB as App's icon shown to the user.
   * @returns string
   */
  public get icon(): string {
    return `${BASE_URL}/Assets/app-icon.png`;
  }

  /**
   * Optional attribute
   * Allow to choose BootMode between COLD / WARM / HOT
   * Default behavior : AppBootMode.COLD
   *
   * COLD : No dom preloaded in memory
   * WARM : App -> AppView are loaded but not rendered into DOM
   * HOT : App -> AppView -> Pages are rendered and injected into DOM
   */
  public BootMode = AppBootMode.HOT;

  /**
   * Optional attribute
   * Allow to choose SuspendMode between SLEEP / TERMINATE
   * Default behavior : AppSuspendMode.SLEEP
   *
   * SLEEP : Default behavior, does nothing, only hiding and sleeping the app if switching to another one
   * TERMINATE : Hiding the app, then killing it by removing it from DOM (BootMode is checked on next frame to reload it and/or to inject it, see BootMode)
   */
  public SuspendMode = AppSuspendMode.SLEEP;

  /**
   * Optional method
   * Allow to resolve some dependencies, install external data, check an api key, etc...
   * @param _props props used when app has been setted up.
   * @returns Promise<void>
   */
  public async install(_props: AppInstallProps): Promise<void> {
    Efb.loadCss(`${BASE_URL}/Any2GsxApp.css`);
    return Promise.resolve();
  }

  /**
   * Optional method
   * Allows to specify an array of compatible ATC MODELS.
   * Your app will be visible but greyed out if the aircraft is not compatible.
   * if undefined or method not implemented, the app will be visible for all aircrafts.
   * @returns string[] | undefined
   */
  public get compatibleAircraftModels(): string[] | undefined {
    return undefined;
  }

  /*
   * @returns {AppView} created above
   */
  public render(): TVNode<Any2GsxAppView> {
    return <Any2GsxAppView bus={this.bus} />;
  }
}

/**
 * App definition to be injected into EFB
 */
Efb.use(Any2GsxApp);
