import { EventBus } from "@microsoft/msfs-sdk";
import { EfbUpdateEvents } from "src/Components/MainPage";

export const RequestTypeRegister = 2;
export const RequestTypeUnregister = 3;
export const RequestTypeUnregisterAll = 4;
export const RequestTypePing = 5;
export const RequestTypeRelay = 6;
export const RequestTypeEfb = 8;
export const EventNameJs = "Any2GSX_RelayToJs";

export const IsJsonString = (str: string): boolean => {
  try {
    JSON.parse(str);
  } catch (e) {
    return false;
  }
  return true;
};

export const CommBusCallback = (evt: string, data: object) => {
  var json: object = {
    event: evt,
    data: data,
  };
  var jsonStr = JSON.stringify(json);

  fetch(`http://localhost:${CommBusService.portNumber}/v1/pushevent`, {
    method: "POST",
    mode: "no-cors",
    body: jsonStr,
    headers: {
      "Content-type": "application/json; charset=UTF-8",
    },
  });
  console.log(`Relayed Event: ${evt}`);
};

export const PingReply = (): void => {
  try {
    fetch(`http://localhost:${CommBusService.portNumber}/v1/ping-reply`, {
      method: "POST",
      mode: "no-cors",
      body: "",
      headers: {
        "Content-type": "application/json; charset=UTF-8",
      },
    });
    console.log(`Ping Reply sent to Port ${CommBusService.portNumber}`);
  } catch (e) {
    console.error("Error while sending Ping Reply");
  }
};

export const PublishEfbUpdate = (json: any): void => {
  try {
    if (json.ConnectionState !== null) {
      CommBusService.efbPublisher.pub("ConnectionState", json.ConnectionState);
    }
    if (json.ProfileName !== null) {
      CommBusService.efbPublisher.pub("ProfileName", json.ProfileName);
    }
    if (json.PhaseStatus !== null) {
      CommBusService.efbPublisher.pub("PhaseStatus", json.PhaseStatus);
    }
    if (json.CouatlVarsValid !== null) {
      CommBusService.efbPublisher.pub("CouatlVarsValid", json.CouatlVarsValid);
    }
    if (json.ProgressLabel !== null) {
      CommBusService.efbPublisher.pub("ProgressLabel", json.ProgressLabel);
    }
    if (json.ProgressInfo !== null) {
      CommBusService.efbPublisher.pub("ProgressInfo", json.ProgressInfo);
    }
    if (json.DepartureServices !== null) {
      CommBusService.efbPublisher.pub(
        "DepartureServices",
        json.DepartureServices
      );
    }
    if (json.SmartCall !== null) {
      CommBusService.efbPublisher.pub("SmartCall", json.SmartCall);
    }
    if (json.MenuTitle !== null) {
      CommBusService.efbPublisher.pub("MenuTitle", json.MenuTitle);
    }
    if (json.MenuLines !== null) {
      CommBusService.efbPublisher.pub("MenuLines", json.MenuLines);
    }
  } catch (e) {
    console.error("Error while publishing EFB Update");
  }
};

export class EventStruct {
  public name: string = "";
  public callback: any;

  public constructor(name: string, func: any) {
    this.name = name;
    this.callback = func;
  }
}

export class CommBusService {
  public static RegisteredEvents: Map<string, EventStruct> = new Map<
    string,
    EventStruct
  >();
  public static wasmListener: ViewListener.ViewListener;
  public static portNumber: string;
  public static efbPublisher: any;

  public constructor(eventBus: EventBus) {
    CommBusService.wasmListener = RegisterViewListener("JS_LISTENER_COMM_BUS");
    if (CommBusService.wasmListener !== undefined) {
      CommBusService.wasmListener.on(EventNameJs, this.AppCallback);
      console.log("CommBus Listener registered");
      CommBusService.efbPublisher = eventBus.getPublisher<EfbUpdateEvents>();
    } else {
      console.error("RegisterViewListener failed");
    }
  }

  public AppCallback(data: string): void {
    if (IsJsonString(data) == true) {
      var request = JSON.parse(data);
      console.log(
        `Received Wasm Request - Type: ${request.type} | Event: ${request.event}`
      );
      if (request.type == RequestTypeRegister) {
        let evtStruct = new EventStruct(request.event, (data: object) =>
          CommBusCallback(request.event, data)
        );
        CommBusService.wasmListener.on(evtStruct.name, evtStruct.callback);
        CommBusService.RegisteredEvents.set(request.event, evtStruct);
        console.log("Event added");
      } else if (request.type == RequestTypeUnregister) {
        let evtStruct = CommBusService.RegisteredEvents.get(request.event);
        if (evtStruct != null) {
          CommBusService.wasmListener.off(evtStruct.name, evtStruct.callback);
          CommBusService.RegisteredEvents.delete(request.event);
          console.log("Event removed");
        }
      } else if (request.type == RequestTypeUnregisterAll) {
        for (let value of CommBusService.RegisteredEvents.values()) {
          if (value != null)
            CommBusService.wasmListener.off(value.name, value.callback);
        }
        CommBusService.RegisteredEvents.clear();
        console.log("All Events removed");
      } else if (request.type == RequestTypePing) {
        CommBusService.portNumber = request.data;
        PingReply();
      } else if (request.type == RequestTypeRelay) {
        CommBusCallback(request.event, request.data);
      } else if (
        request.type == RequestTypeEfb &&
        IsJsonString(request.data) == true
      ) {
        PublishEfbUpdate(JSON.parse(request.data));
        console.log("Received EFB Update");
      } else console.error("Received type from Wasm Module is unknown!");
    } else {
      console.error("Received data from Wasm Module is not a Json!");
      console.log(data);
    }
  }
}
