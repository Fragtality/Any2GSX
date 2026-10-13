import {
  Button,
  GamepadUiView,
  RequiredProps,
  TVNode,
  UiViewProps,
} from "@efb/efb-api";
import {
  ConsumerSubject,
  EventBus,
  FSComponent,
  Subject,
  Subscribable,
} from "@microsoft/msfs-sdk";
import "./MainPage.scss";

interface MainPageProps extends RequiredProps<UiViewProps, "appViewService"> {
  eventBus: EventBus;
}

export interface EfbUpdateEvents {
  AppConnectionState: string;
  AircraftConnectionState: string;
  ExternalControlState: string;
  ProfileName: string;
  FlightPhase: string;
  PhaseStatus: string;
  CouatlVarsValid: string;
  DepartureServices: string;
  ProgressLabel: string;
  ProgressInfo: string;
  SmartCall: string;
  MenuTitle: string;
  MenuLines: string[];
}

const StyleVisible = "visibility:visible; display:flex;";
const StyleHidden = "visibility:collapse; display:none;";
const StyleLineHidden = "visibility:collapse;";

export class MainPage extends GamepadUiView<HTMLDivElement, MainPageProps> {
  public readonly tabName = MainPage.name;
  protected props: MainPageProps;

  protected readonly AppConnectionState: Subscribable<string>;
  protected readonly AppConnectionStateClass: Subject<string>;

  protected readonly AircraftConnectionState: Subscribable<string>;
  protected readonly AircraftConnectionStateClass: Subject<string>;

  protected readonly ExternalControlState: Subscribable<string>;
  protected readonly ProfileName: Subscribable<string>;
  protected readonly FlightPhase: Subscribable<string>;
  protected readonly PhaseStatus: Subscribable<string>;

  protected readonly DepartureServices: Subscribable<string>;
  protected readonly DepartureServicesVisibility: Subject<string>;

  protected readonly ProgressLabel: Subscribable<string>;
  protected readonly ProgressInfo: Subscribable<string>;
  protected readonly ProgressVisibility: Subject<string>;

  protected readonly SmartCall: Subscribable<string>;
  protected readonly SmartCallVisibility: Subject<string>;

  protected readonly MenuTitleDisplay: Subject<string>;
  protected readonly MenuTitleVisibility: Subject<string>;

  protected readonly MenuLinesDisplay: Subject<string>[];
  protected readonly MenuLinesVisibility: Subject<string>[];

  protected readonly MenuOpenText: string = "Open Menu ...";

  constructor(props: MainPageProps) {
    super();

    this.props = props;

    const subscriber = this.props.eventBus.getSubscriber<EfbUpdateEvents>();

    // -------------------------------------------------------------------------
    // APP STATE
    // -------------------------------------------------------------------------

    this.AppConnectionState = ConsumerSubject.create(
      subscriber.on("AppConnectionState"),
      "Disconnected",
    );

    this.AppConnectionStateClass = Subject.create<string>("property-red");

    this.AircraftConnectionState = ConsumerSubject.create(
      subscriber.on("AircraftConnectionState"),
      "Disconnected",
    );

    this.AircraftConnectionStateClass = Subject.create<string>("property-red");

    this.ExternalControlState = ConsumerSubject.create(
      subscriber.on("ExternalControlState"),
      "Off",
    );

    this.ProfileName = ConsumerSubject.create(subscriber.on("ProfileName"), "");

    this.FlightPhase = ConsumerSubject.create(subscriber.on("FlightPhase"), "");

    this.PhaseStatus = ConsumerSubject.create(subscriber.on("PhaseStatus"), "");

    this.DepartureServices = ConsumerSubject.create(
      subscriber.on("DepartureServices"),
      "",
    );

    this.DepartureServicesVisibility = Subject.create<string>(StyleHidden);

    this.ProgressLabel = ConsumerSubject.create(
      subscriber.on("ProgressLabel"),
      "",
    );

    this.ProgressInfo = ConsumerSubject.create(
      subscriber.on("ProgressInfo"),
      "",
    );

    this.ProgressVisibility = Subject.create<string>(StyleHidden);

    this.SmartCall = ConsumerSubject.create(subscriber.on("SmartCall"), "");

    this.SmartCallVisibility = Subject.create<string>(StyleHidden);

    // -------------------------------------------------------------------------
    // APP CONNECTION STATE
    // -------------------------------------------------------------------------

    subscriber
      .on("AppConnectionState")
      .whenChanged()
      .handle((state) => {
        this.SetConnectionState(state, this.AppConnectionStateClass);

        if (state === "Disconnected") {
          this.DepartureServicesVisibility.set(StyleHidden);
          this.SmartCallVisibility.set(StyleHidden);
          this.ProgressVisibility.set(StyleHidden);

          this.MenuTitleVisibility.set(StyleLineHidden);

          this.MenuLinesVisibility.forEach((line) => {
            line.set(StyleLineHidden);
          });

          this.props.eventBus
            .getPublisher<EfbUpdateEvents>()
            .pub("PhaseStatus", "");
        }
      });

    // -------------------------------------------------------------------------
    // AIRCRAFT CONNECTION STATE
    // -------------------------------------------------------------------------

    subscriber
      .on("AircraftConnectionState")
      .whenChanged()
      .handle((state) => {
        this.SetConnectionState(state, this.AircraftConnectionStateClass);
      });

    // -------------------------------------------------------------------------
    // SMART CALL
    // -------------------------------------------------------------------------

    subscriber
      .on("SmartCall")
      .whenChanged()
      .handle((call) => {
        if (call !== null && call !== "") {
          this.SmartCallVisibility.set(StyleVisible);
        } else {
          this.SmartCallVisibility.set(StyleHidden);
        }
      });

    // -------------------------------------------------------------------------
    // DEPARTURE SERVICES
    // -------------------------------------------------------------------------

    subscriber
      .on("DepartureServices")
      .whenChanged()
      .handle((text) => {
        if (text !== null && text !== "") {
          this.DepartureServicesVisibility.set(StyleVisible);
        } else {
          this.DepartureServicesVisibility.set(StyleHidden);
        }
      });

    // -------------------------------------------------------------------------
    // PROGRESS
    // -------------------------------------------------------------------------

    subscriber
      .on("ProgressInfo")
      .whenChanged()
      .handle((text) => {
        if (text !== null && text !== "") {
          this.ProgressVisibility.set(StyleVisible);
        } else {
          this.ProgressVisibility.set(StyleHidden);
        }
      });

    // -------------------------------------------------------------------------
    // GSX MENU
    // -------------------------------------------------------------------------

    this.MenuTitleDisplay = Subject.create<string>(this.MenuOpenText);

    this.MenuTitleVisibility = Subject.create<string>(StyleLineHidden);

    subscriber
      .on("MenuTitle")
      .whenChanged()
      .handle((text) => {
        this.SetMenuTitle(text);
      });

    this.MenuLinesDisplay = [];
    this.MenuLinesVisibility = [];

    for (let i = 0; i < 10; i++) {
      this.MenuLinesDisplay[i] = Subject.create<string>("");
      this.MenuLinesVisibility[i] = Subject.create<string>(StyleLineHidden);
    }

    subscriber
      .on("MenuLines")
      .whenChanged()
      .handle((menuLines) => {
        let index = 0;

        this.MenuTitleVisibility.set(StyleVisible);

        menuLines.forEach((line) => {
          if (index >= this.MenuLinesDisplay.length) {
            return;
          }

          this.MenuLinesDisplay[index].set(line);

          this.MenuLinesVisibility[index].set(
            line !== null && line !== "" ? StyleVisible : StyleLineHidden,
          );

          index++;
        });

        // Hide any unused menu lines from a previous menu.
        for (let i = index; i < this.MenuLinesVisibility.length; i++) {
          this.MenuLinesDisplay[i].set("");
          this.MenuLinesVisibility[i].set(StyleLineHidden);
        }
      });

    // -------------------------------------------------------------------------
    // COUATL VARIABLES
    // -------------------------------------------------------------------------

    subscriber
      .on("CouatlVarsValid")
      .whenChanged()
      .handle((state) => {
        if (state === "true") {
          this.SetMenuTitle(this.MenuOpenText);

          this.MenuLinesVisibility.forEach((line) => {
            line.set(StyleLineHidden);
          });
        } else {
          this.MenuTitleVisibility.set(StyleLineHidden);

          this.MenuLinesVisibility.forEach((line) => {
            line.set(StyleLineHidden);
          });
        }
      });
  }

  // ---------------------------------------------------------------------------
  // CONNECTION STATE
  // ---------------------------------------------------------------------------

  public SetConnectionState(
    state: string,
    classProperty: Subject<string>,
  ): void {
    if (state === "Disconnected") {
      classProperty.set("property-red");
    } else {
      classProperty.set("property-green");
    }
  }

  // ---------------------------------------------------------------------------
  // GSX MENU TITLE
  // ---------------------------------------------------------------------------

  public SetMenuTitle(text: string): void {
    if (text !== null && text !== "") {
      this.MenuTitleDisplay.set(text);
      this.MenuTitleVisibility.set(StyleVisible);
    } else {
      this.MenuTitleDisplay.set(this.MenuOpenText);
    }
  }

  // ---------------------------------------------------------------------------
  // SMART BUTTON
  // ---------------------------------------------------------------------------

  public SmartButtonRequest(): void {
    console.log("Setting SmartButton Request");

    SimVar.SetSimVarValue("L:ANY2GSX_SMARTBUTTON_REQ", "number", 1);
  }

  // ---------------------------------------------------------------------------
  // OPEN GSX MENU
  // ---------------------------------------------------------------------------

  public OpenMenu(): void {
    console.log("Open GSX Menu");

    if (this.MenuTitleDisplay.get() === this.MenuOpenText) {
      SimVar.SetSimVarValue("L:FSDT_GSX_MENU_OPEN", "number", 1);
    } else {
      SimVar.SetSimVarValue("L:FSDT_GSX_MENU_CHOICE", "number", -1);
    }
  }

  // ---------------------------------------------------------------------------
  // SELECT GSX MENU ITEM
  // ---------------------------------------------------------------------------

  public SelectMenu(index: number): void {
    console.log(`Select GSX Index ${index}`);

    SimVar.SetSimVarValue("L:FSDT_GSX_MENU_CHOICE", "number", index);
  }

  // ---------------------------------------------------------------------------
  // RENDER
  // ---------------------------------------------------------------------------

  public render(): TVNode<HTMLDivElement> {
    return (
      <div ref={this.gamepadUiViewRef} class="main-page">
        <div class="content">
          {/* ================================================================
              APP STATE
              ================================================================ */}

          <div class="appState">
            <div class="top-header">App State</div>

            <div class="row">
              <span class="label">App Connection</span>

              <span class={this.AppConnectionStateClass}>
                {this.AppConnectionState}
              </span>
            </div>

            <div class="row">
              <span class="label">Aircraft Connection</span>

              <span class={this.AircraftConnectionStateClass}>
                {this.AircraftConnectionState}
              </span>
            </div>

            <div class="row">
              <span class="label">External Control</span>

              <span class="property">{this.ExternalControlState}</span>
            </div>

            <div class="row">
              <span class="label">Profile Name</span>

              <span class="property">{this.ProfileName}</span>
            </div>

            <div class="row">
              <span class="label">Flight Phase</span>

              <span class="property">{this.FlightPhase}</span>
            </div>

            <div class="row">
              <span class="label">Status</span>

              <span class="property">{this.PhaseStatus}</span>
            </div>

            <div class="row" style={this.DepartureServicesVisibility}>
              <span class="label">Departure Services</span>

              <span class="property">{this.DepartureServices}</span>
            </div>

            <div class="row" style={this.ProgressVisibility}>
              <span class="label">{this.ProgressLabel}</span>

              <span class="property">{this.ProgressInfo}</span>
            </div>

            <div class="row" style={this.SmartCallVisibility}>
              <span class="label">SmartButton Call</span>

              <Button class="button" callback={() => this.SmartButtonRequest()}>
                {this.SmartCall}
              </Button>
            </div>
          </div>

          {/* ================================================================
              GSX MENU
              ================================================================ */}

          <div class="gsxState">
            <div class="header">GSX Menu</div>

            <div class="menuContent">
              <Button
                class="menuTitle"
                callback={() => this.OpenMenu()}
                style={this.MenuTitleVisibility}
              >
                <span class="buttonContent">{this.MenuTitleDisplay}</span>
              </Button>

              <Button
                class="menuLine"
                style={this.MenuLinesVisibility[0]}
                callback={() => this.SelectMenu(0)}
              >
                {this.MenuLinesDisplay[0]}
              </Button>

              <Button
                class="menuLine"
                style={this.MenuLinesVisibility[1]}
                callback={() => this.SelectMenu(1)}
              >
                {this.MenuLinesDisplay[1]}
              </Button>

              <Button
                class="menuLine"
                style={this.MenuLinesVisibility[2]}
                callback={() => this.SelectMenu(2)}
              >
                {this.MenuLinesDisplay[2]}
              </Button>

              <Button
                class="menuLine"
                style={this.MenuLinesVisibility[3]}
                callback={() => this.SelectMenu(3)}
              >
                {this.MenuLinesDisplay[3]}
              </Button>

              <Button
                class="menuLine"
                style={this.MenuLinesVisibility[4]}
                callback={() => this.SelectMenu(4)}
              >
                {this.MenuLinesDisplay[4]}
              </Button>

              <Button
                class="menuLine"
                style={this.MenuLinesVisibility[5]}
                callback={() => this.SelectMenu(5)}
              >
                {this.MenuLinesDisplay[5]}
              </Button>

              <Button
                class="menuLine"
                style={this.MenuLinesVisibility[6]}
                callback={() => this.SelectMenu(6)}
              >
                {this.MenuLinesDisplay[6]}
              </Button>

              <Button
                class="menuLine"
                style={this.MenuLinesVisibility[7]}
                callback={() => this.SelectMenu(7)}
              >
                {this.MenuLinesDisplay[7]}
              </Button>

              <Button
                class="menuLine"
                style={this.MenuLinesVisibility[8]}
                callback={() => this.SelectMenu(8)}
              >
                {this.MenuLinesDisplay[8]}
              </Button>

              <Button
                class="menuLine"
                style={this.MenuLinesVisibility[9]}
                callback={() => this.SelectMenu(9)}
              >
                {this.MenuLinesDisplay[9]}
              </Button>
            </div>
          </div>
        </div>
      </div>
    );
  }
}
