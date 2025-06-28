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
  ConnectionState: string;
  ProfileName: string;
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
  protected readonly ConnectionState: Subscribable<string>;
  protected readonly ConnectionStateClass: Subject<string>;
  protected readonly ProfileName: Subscribable<string>;
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

  constructor(props: MainPageProps) {
    super();
    this.props = props;
    const subscriber = this.props.eventBus.getSubscriber<EfbUpdateEvents>();

    //APP State
    this.ConnectionState = ConsumerSubject.create(
      subscriber.on("ConnectionState"),
      "Disconnected"
    );
    this.ConnectionStateClass = Subject.create<string>("property-red");
    this.ProfileName = ConsumerSubject.create(subscriber.on("ProfileName"), "");
    this.PhaseStatus = ConsumerSubject.create(subscriber.on("PhaseStatus"), "");
    this.DepartureServices = ConsumerSubject.create(
      subscriber.on("DepartureServices"),
      ""
    );
    this.DepartureServicesVisibility = Subject.create<string>(StyleHidden);
    this.ProgressLabel = ConsumerSubject.create(
      subscriber.on("ProgressLabel"),
      ""
    );
    this.ProgressInfo = ConsumerSubject.create(
      subscriber.on("ProgressInfo"),
      ""
    );
    this.ProgressVisibility = Subject.create<string>(StyleHidden);
    this.SmartCall = ConsumerSubject.create(subscriber.on("SmartCall"), "");
    this.SmartCallVisibility = Subject.create<string>(StyleHidden);

    subscriber
      .on("ConnectionState")
      .whenChanged()
      .handle((state) => {
        typeof state !== null && state == "Disconnected"
          ? this.ConnectionStateClass.set("property-red")
          : this.ConnectionStateClass.set("property-green");
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
      .on("SmartCall")
      .whenChanged()
      .handle((call) =>
        typeof call !== null && call != ""
          ? this.SmartCallVisibility.set(StyleVisible)
          : this.SmartCallVisibility.set(StyleHidden)
      );

    subscriber
      .on("DepartureServices")
      .whenChanged()
      .handle((text) =>
        typeof text !== null && text != ""
          ? this.DepartureServicesVisibility.set(StyleVisible)
          : this.DepartureServicesVisibility.set(StyleHidden)
      );

    subscriber
      .on("ProgressInfo")
      .whenChanged()
      .handle((text) =>
        typeof text !== null && text != ""
          ? this.ProgressVisibility.set(StyleVisible)
          : this.ProgressVisibility.set(StyleHidden)
      );

    //GSX MENU
    this.MenuTitleDisplay = Subject.create<string>("Open Menu ...");
    this.MenuTitleVisibility = Subject.create<string>(StyleLineHidden);
    subscriber
      .on("MenuTitle")
      .whenChanged()
      .handle((text) => {
        typeof text !== null && text != ""
          ? this.MenuTitleDisplay.set(text)
          : this.MenuTitleDisplay.set("Open Menu ...");
        if (typeof text !== null && text != "") {
          this.MenuTitleVisibility.set(StyleVisible);
        }
      });

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
            typeof line !== null && line != "" ? StyleVisible : StyleLineHidden
          );
          index++;
        });
      });

    subscriber
      .on("CouatlVarsValid")
      .whenChanged()
      .handle((state) => {
        if (typeof state != null && state == "true") {
          this.MenuTitleVisibility.set(StyleVisible);
          this.MenuLinesVisibility.forEach((line) => {
            line.set(StyleVisible);
          });
        } else {
          this.MenuTitleVisibility.set(StyleLineHidden);
          this.MenuLinesVisibility.forEach((line) => {
            line.set(StyleLineHidden);
          });
        }
      });
  }

  public SmartButtonRequest(): void {
    console.log("Setting SmartButton Request");
    SimVar.SetSimVarValue("L:ANY2GSX_SMARTBUTTON_REQ", "number", 1);
  }

  public OpenMenu(): void {
    console.log("Open GSX Menu");
    SimVar.SetSimVarValue("L:FSDT_GSX_MENU_OPEN", "number", 1);
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
            <span class="label">Connection State</span>
            <span class={this.ConnectionStateClass}>
              {this.ConnectionState}
            </span>
          </div>

          <div class="row">
            <span class="label">Profile Name</span>
            <span class="property">{this.ProfileName}</span>
          </div>

          <div class="row">
            <span class="label">Flight Phase &amp; Status</span>
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
