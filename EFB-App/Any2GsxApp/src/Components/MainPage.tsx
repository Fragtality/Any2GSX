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

    //APP State
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

    subscriber
      .on("AppConnectionState")
      .whenChanged()
      .handle((state) => {
        this.SetConnectionState(state, this.AppConnectionStateClass);
        if (typeof state !== null && state == "Disconnected") {
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

    subscriber
      .on("AircraftConnectionState")
      .whenChanged()
      .handle((state) => {
        this.SetConnectionState(state, this.AircraftConnectionStateClass);
      });

    subscriber
      .on("SmartCall")
      .whenChanged()
      .handle((call) =>
        typeof call !== null && call != ""
          ? this.SmartCallVisibility.set(StyleVisible)
          : this.SmartCallVisibility.set(StyleHidden),
      );

    subscriber
      .on("DepartureServices")
      .whenChanged()
      .handle((text) =>
        typeof text !== null && text != ""
          ? this.DepartureServicesVisibility.set(StyleVisible)
          : this.DepartureServicesVisibility.set(StyleHidden),
      );

    subscriber
      .on("ProgressInfo")
      .whenChanged()
      .handle((text) =>
        typeof text !== null && text != ""
          ? this.ProgressVisibility.set(StyleVisible)
          : this.ProgressVisibility.set(StyleHidden),
      );

    //GSX MENU
    this.MenuTitleDisplay = Subject.create<string>(this.MenuOpenText);
    this.MenuTitleVisibility = Subject.create<string>(StyleLineHidden);
    subscriber
      .on("MenuTitle")
      .whenChanged()
      .handle((text) => this.SetMenuTitle(text));

    let i = 0;
    this.MenuLinesDisplay = [];
    this.MenuLinesVisibility = [];
    while (i < 10) {
      this.MenuLinesDisplay[i] = Subject.create<string>("");
      this.MenuLinesVisibility[i] = Subject.create<string>(StyleLineHidden);
      i++;
    }
    subscriber
      .on("MenuLines")
      .whenChanged()
      .handle((menuLines) => {
        let index = 0;
        this.MenuTitleVisibility.set(StyleVisible);
        menuLines.forEach((line) => {
          this.MenuLinesDisplay[index].set(line);
          this.MenuLinesVisibility[index].set(
            typeof line !== null && line != "" ? StyleVisible : StyleLineHidden,
          );
          index++;
        });
      });

    subscriber
      .on("CouatlVarsValid")
      .whenChanged()
      .handle((state) => {
        if (typeof state != null && state == "true") {
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

  public SetConnectionState(
    state: string,
    classProperty: Subject<string>,
  ): void {
    typeof state !== null && state == "Disconnected"
      ? classProperty.set("property-red")
      : classProperty.set("property-green");
  }

  public SetMenuTitle(text: string): void {
    typeof text !== null && text != ""
      ? this.MenuTitleDisplay.set(text)
      : this.MenuTitleDisplay.set(this.MenuOpenText);
    if (typeof text !== null && text != "") {
      this.MenuTitleVisibility.set(StyleVisible);
    }
  }

  public SmartButtonRequest(): void {
    console.log("Setting SmartButton Request");
    SimVar.SetSimVarValue("L:ANY2GSX_SMARTBUTTON_REQ", "number", 1);
  }

  public OpenMenu(): void {
    console.log("Open GSX Menu");
    if (this.MenuTitleDisplay.get() == this.MenuOpenText) {
      SimVar.SetSimVarValue("L:FSDT_GSX_MENU_OPEN", "number", 1);
    } else {
      SimVar.SetSimVarValue("L:FSDT_GSX_MENU_CHOICE", "number", -1);
    }
  }

  public SelectMenu(index: number): void {
    console.log(`Select GSX Index ${index}`);
    SimVar.SetSimVarValue("L:FSDT_GSX_MENU_CHOICE", "number", index);
  }

  public render(): TVNode<HTMLDivElement> {
    return (
      <div ref={this.gamepadUiViewRef} class="main-page">
        <div class="content">
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
    );
  }
}
