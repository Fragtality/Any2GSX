#include <MSFS\MSFS.h>
#include <MSFS\MSFS_WindowsTypes.h>
#include <SimConnect.h>
#include <MSFS\Legacy\gauges.h>
#include <MSFS\MSFS_CommBus.h>
#include <MSFS\MSFS_Network.h>
#include <rapidjson/document.h>
#include <rapidjson/prettywriter.h>
#include <rapidjson/stringbuffer.h>
#include <cstdio>
#include <deque>
#include <list>
#include <algorithm>
#include <string>
#include "WASM_Module.h"

typedef long long FsNetworkRequestId;
typedef void (*HttpRequestCallback)(FsNetworkRequestId requestId, int errorCode, void* userData);

const char* VERSION = "0.1";
const char* CLIENTNAME = "Any2GSX_CommBus";

const char* EventNameJs = "Any2GSX_RelayToJs";

struct ReceivedEvent
{
	std::string name;
	std::string data;
};

struct RegisteredEvent
{
	std::string name;
	FsCommBusBroadcastFlags flag;
};

enum RequestType
{
	CALL = 1,
	REGISTER = 2,
	UNREGISTER = 3,
	REMOVEALL = 4,
	PING = 5,
	RELAY = 6,
	CODE = 7,
	EFB = 8,
};

struct RequestMessage
{
	RequestType type;
	const char* event;
	const char* data;
	FsCommBusBroadcastFlags flags;
};

const int SIZE_AREA = 8192;
const SIMCONNECT_CLIENT_DATA_ID CDA_REQUEST = 401;
const char* CDA_NAME_REQUEST = "Any2GSX_CommBus.Request";
const SIMCONNECT_DATA_REQUEST_ID SIMCONNECT_REQUEST_ID_REQUEST = 401;
const SIMCONNECT_CLIENT_DATA_DEFINITION_ID DEF_ID_REQUEST = 401;

HANDLE hSimConnect;
long frameCounter = 0;

static std::list<RegisteredEvent*> RegisteredEvents;

void RegisterDataAreas(bool force)
{
	HRESULT hr = SimConnect_MapClientDataNameToID(hSimConnect, CDA_NAME_REQUEST, CDA_REQUEST);
	if (hr != S_OK && !force)
	{
		fprintf(stderr, "[%s] %s\n", CLIENTNAME, "Could not Map Request CDA Name to ID");
		return;
	}
	hr = SimConnect_CreateClientData(hSimConnect, CDA_REQUEST, SIZE_AREA, SIMCONNECT_CREATE_CLIENT_DATA_FLAG_DEFAULT);
	if (hr != S_OK && !force)
	{
		fprintf(stderr, "[%s] %s\n", CLIENTNAME, "Could not create Request CDA");
		return;
	}
	hr = SimConnect_AddToClientDataDefinition(
		hSimConnect,
		DEF_ID_REQUEST,
		0,
		SIZE_AREA
	);
	if (hr != S_OK && !force)
	{
		fprintf(stderr, "[%s] %s\n", CLIENTNAME, "Could not add Request CDA to DataDefinition");
		return;
	}
	hr = SimConnect_RequestClientData(
		hSimConnect,
		CDA_REQUEST,
		SIMCONNECT_REQUEST_ID_REQUEST,
		DEF_ID_REQUEST,
		SIMCONNECT_CLIENT_DATA_PERIOD_ON_SET,
		SIMCONNECT_CLIENT_DATA_REQUEST_FLAG_DEFAULT,
		0,
		0,
		0);
	if (hr != S_OK && !force)
	{
		fprintf(stderr, "[%s] %s\n", CLIENTNAME, "Could not request Data for Request CDA");
		return;
	}
}

//{ type: int, event: "string", data: "string", flag: int }
RequestMessage ParseRequest(const char* data)
{
	RequestMessage request;
	request.type = (RequestType)0;

	rapidjson::Document document;
	document.Parse(data);
	if (document.HasParseError())
	{
		fprintf(stderr, "[%s] %s\n", CLIENTNAME, "JSON Request has Parse Error");
		return request;
	}

	auto nodeType = document.FindMember("type");
	if (nodeType == document.MemberEnd() || !nodeType->value.IsInt())
	{
		fprintf(stderr, "[%s] %s\n", CLIENTNAME, "JSON Request has invalid Type Field");
		return request;
	}
	request.type = (RequestType)nodeType->value.GetInt();

	auto nodeEvent = document.FindMember("event");
	if (nodeEvent == document.MemberEnd() || !nodeEvent->value.IsString())
	{
		fprintf(stderr, "[%s] %s\n", CLIENTNAME, "JSON Request has invalid Event Field");
		return request;
	}
	request.event = nodeEvent->value.GetString();

	auto nodeData = document.FindMember("data");
	if (nodeData != document.MemberEnd() && nodeData->value.IsString())
	{
		request.data = nodeData->value.GetString();
	}

	auto nodeFlag = document.FindMember("flag");
	if (nodeFlag != document.MemberEnd() && nodeFlag->value.IsInt())
	{
		request.flags = (FsCommBusBroadcastFlags)nodeFlag->value.GetInt();
	}

	return request;
}

//{ type: int, event: "string", data: "string" }
const char* SerializeRequestJs(const char* event, RequestType type, const char* data = nullptr)
{
	rapidjson::StringBuffer buffer;
	rapidjson::PrettyWriter<rapidjson::StringBuffer> writer(buffer);
	writer.StartObject();

	writer.String("type");
	writer.Int((int)type);

	writer.String("event");
	writer.String(event);

	if (data != nullptr)
	{
		writer.String("data");
		writer.String(data);
	}

	writer.EndObject();

	return buffer.GetString();
}

void CommBusCallback(const char* buf, unsigned int bufSize, void* ctx)
{
	auto evtStruct = (RegisteredEvent*)ctx;
	const char* msg = SerializeRequestJs(evtStruct->name.c_str(), RELAY, buf);
	fsCommBusCall(EventNameJs, msg, strlen(msg) + 1, FsCommBusBroadcast_JS);
}

void HandleEventRegisterWasm(const char* event)
{
	if (!std::any_of(RegisteredEvents.begin(), RegisteredEvents.end(), [event](RegisteredEvent* e){ return strcmp(event, e->name.c_str()) == 0; }))
	{
		RegisteredEvent* evtStruct = new RegisteredEvent();
		evtStruct->name = std::string(event, size_t(event));
		evtStruct->flag = FsCommBusBroadcast_Wasm;
		RegisteredEvents.push_back(evtStruct);
		fsCommBusRegister(evtStruct->name.c_str(), CommBusCallback, (void*)evtStruct);
		fprintf(stdout, "[%s] %s %s\n", CLIENTNAME, "Event registered:", evtStruct->name.c_str());
	}
	else
	{
		fprintf(stderr, "[%s] %s %s\n", CLIENTNAME, "The Event is already registered:", event);
	}
}

void HandleEventRegisterJs(const char* event)
{
	if (!std::any_of(RegisteredEvents.begin(), RegisteredEvents.end(), [event](RegisteredEvent* e) { return strcmp(event, e->name.c_str()) == 0; }))
	{
		RegisteredEvent* evtStruct = new RegisteredEvent();
		evtStruct->name = std::string(event, size_t(event));
		evtStruct->flag = FsCommBusBroadcast_JS;
		RegisteredEvents.push_back(evtStruct);
		const char* msg = SerializeRequestJs(evtStruct->name.c_str(), REGISTER);
		fsCommBusCall(EventNameJs, msg, strlen(msg) + 1, FsCommBusBroadcast_JS);
		fprintf(stdout, "[%s] %s %s\n", CLIENTNAME, "Send Registration to InGamePanel:", evtStruct->name.c_str());
	}
	else
	{
		fprintf(stderr, "[%s] %s %s\n", CLIENTNAME, "The Event is already registered:", event);
	}
}

void HandleEventUnregister(const char* event)
{
	auto it = std::find_if(RegisteredEvents.begin(), RegisteredEvents.end(), [event](RegisteredEvent* e) { return strcmp(event, e->name.c_str()) == 0; });
	if (it != RegisteredEvents.end())
	{
		RegisteredEvent* evtStruct = *it;
		if (evtStruct->flag == FsCommBusBroadcast_Wasm)
		{
			RegisteredEvents.remove_if([event](RegisteredEvent* e) { return strcmp(event, e->name.c_str()) == 0; });
			delete evtStruct;
			fprintf(stdout, "[%s] %s %s\n", CLIENTNAME, "Event unregistered:", event);
		}
		else if (evtStruct->flag == FsCommBusBroadcast_JS)
		{
			RegisteredEvents.remove_if([event](RegisteredEvent* e) { return strcmp(event, e->name.c_str()) == 0; });
			const char* msg = SerializeRequestJs(evtStruct->name.c_str(), UNREGISTER);
			fsCommBusCall(EventNameJs, msg, strlen(msg) + 1, FsCommBusBroadcast_JS);
			delete evtStruct;
			fprintf(stdout, "[%s] %s %s\n", CLIENTNAME, "Send Unregistration to InGamePanel:", event);
		}
		else
			fprintf(stderr, "[%s] %s %d\n", CLIENTNAME, "received unknown Register Flag:", evtStruct->flag);
	}
	else
	{
		fprintf(stderr, "[%s] %s %s\n", CLIENTNAME, "The Event is not registered:", event);
	}
}

void HandleUnregisterAll()
{
	const char* msg = SerializeRequestJs("", REMOVEALL);
	fsCommBusCall(EventNameJs, msg, strlen(msg) + 1, FsCommBusBroadcast_JS);

	fsCommBusUnregisterAll();

	auto it = RegisteredEvents.begin();
	while (it != RegisteredEvents.end())
	{
		RegisteredEvent* evtStruct = *it;
		delete evtStruct;
		++it;
	}
	RegisteredEvents.clear();
	
	fprintf(stdout, "[%s] %s\n", CLIENTNAME, "All Events cleared");
}

void HandleEventCode(const char* code)
{
	if (!execute_calculator_code(code, nullptr, nullptr, nullptr))
	{
		fprintf(stderr, "[%s] %s '%s'\n", CLIENTNAME, "failed to execute Code", code);
	}
}

void CALLBACK SimConnectDispatch(SIMCONNECT_RECV* pData, DWORD cbData, void* pContext)
{
	switch (pData->dwID)
	{
		case SIMCONNECT_RECV_ID_EXCEPTION:
		{
			auto ex_data = static_cast<SIMCONNECT_RECV_EXCEPTION*>(pData);
			fprintf(stderr, "[%s] %s %d\n", CLIENTNAME, "received Exception:", ex_data->dwException);

			break;
		}
		case SIMCONNECT_RECV_ID_CLIENT_DATA:
		{
			auto recv_data = static_cast<SIMCONNECT_RECV_CLIENT_DATA*>(pData);
			if (recv_data->dwRequestID == DEF_ID_REQUEST)
			{
				auto data = (const char*)(&recv_data->dwData);
				auto request = ParseRequest(data);
				fprintf(stdout, "[%s] %s %s\n", CLIENTNAME, "received Request from App:", data);

				if (request.type == CALL)
				{
					fsCommBusCall(request.event, request.data, strlen(request.data) + 1, request.flags);
				}
				else if (request.type == REGISTER)
				{
					if (request.flags == FsCommBusBroadcast_Wasm)
						HandleEventRegisterWasm(request.event);
					else if (request.flags == FsCommBusBroadcast_JS)
						HandleEventRegisterJs(request.event);
					else
						fprintf(stderr, "[%s] %s %d\n", CLIENTNAME, "received unknown Register Flag:", request.flags);
				}
				else if (request.type == UNREGISTER)
				{
					HandleEventUnregister(request.event);
				}
				else if (request.type == REMOVEALL)
				{
					HandleUnregisterAll();
				}
				else if (request.type == PING)
				{
					const char* msg = SerializeRequestJs("all", PING, request.data);
					fsCommBusCall(EventNameJs, msg, strlen(msg) + 1, FsCommBusBroadcast_JS);
					fprintf(stdout, "[%s] %s %s\n", CLIENTNAME, "Relayed Ping to JavaScript Module - Port", request.data);
				}
				else if (request.type == CODE)
				{
					HandleEventCode(request.data);
				}
				else if (request.type == EFB)
				{
					const char* msg = SerializeRequestJs("EfbUpdate", EFB, request.data);
					fsCommBusCall(EventNameJs, msg, strlen(msg) + 1, FsCommBusBroadcast_JS);
					fprintf(stdout, "[%s] %s %s\n", CLIENTNAME, "Send Update to EFB App", request.data);
				}
				else
					fprintf(stdout, "[%s] %s %d\n", CLIENTNAME, "received unknown Request Type:", (int)request.type);
			}
			else
			{
				fprintf(stdout, "[%s] %s %d\n", CLIENTNAME, "received unknown RequestID:", recv_data->dwRequestID);
			}

			break;
		}
		case SIMCONNECT_RECV_ID_OPEN:
		{
			RegisterDataAreas(false);
			fprintf(stdout, "[%s] %s\n", CLIENTNAME, "Open Event Received!");

			break;
		}
		default:
		{
			fprintf(stdout, "[%s] %s %d\n", CLIENTNAME, "received unknown dwID:", pData->dwID);
		}
	}
}

extern "C" MSFS_CALLBACK void module_init(void)
{
	hSimConnect = 0;
	HRESULT hr = SimConnect_Open(&hSimConnect, CLIENTNAME, (HWND)NULL, 0, 0, 0);
	if (hr != S_OK)
	{
		fprintf(stderr, "[%s] %s\n", CLIENTNAME, "Could not open SimConnect connection.");
		return;
	}
	else
		fprintf(stdout, "[%s] %s\n", CLIENTNAME, "SimConnect connected.");

	hr = SimConnect_CallDispatch(hSimConnect, SimConnectDispatch, NULL);
	if (hr != S_OK)
	{
		fprintf(stderr, "[%s] %s\n", CLIENTNAME, "Could not set dispatch proc.");
		return;
	}

	fprintf(stdout, "[%s] %s\n", CLIENTNAME, "Module initialized.");
}

extern "C" MSFS_CALLBACK void module_deinit(void)
{
	if (!hSimConnect)
		return;

	HandleUnregisterAll();

	HRESULT hr = SimConnect_Close(hSimConnect);
	if (hr != S_OK)
	{
		fprintf(stderr, "[%s] %s\n", CLIENTNAME, "Could not close SimConnect connection.");
		return;
	}

	fprintf(stdout, "[%s] %s\n", CLIENTNAME, "Module deinitialized.");
}