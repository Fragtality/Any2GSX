(function () {
	function load_js(path, callback) {
		// Create script element
		var module = document.createElement('script');
		module.src = path;
		module.async = false;

		// Check if already exists
		if (document.head.querySelector('script[src="' + path + '"]')) {
			if (typeof callback !== 'undefined') {
				callback();
			}
			return;
		}

		// Add to head
		document.head.appendChild(module);
		module.onload = () => {
			if (typeof callback !== 'undefined') {
				callback();
			}
		}
	}

	// Example of what you can do with the toolbar interop.
	function initialize() {
		// Find the Toolbar (MSFS 2024)
		let toolbar = document.querySelector('ui-panel');

		if(toolbar) {
			const toolbar_button = toolbar.querySelector('ui-resource-element[icon="coui://html_ui/vfs/html_ui/icons/toolbar/ANY2GSX_COMMBUS_ICON.svg"]');
			toolbar_button.style.display = 'none';
		}
		else {
			// Find the Toolbar (MSFS 2020)
			toolbar = document.querySelector('tool-bar');
			const toolbar_button = toolbar.querySelector('toolbar-button[panel-id="PANEL_ANY2GSX_COMMBUS"]');
			toolbar_button.style.display = 'none';
		}
		
		const RequestTypeRegister = 2;
		const RequestTypeUnregister = 3;
		const RequestTypeUnregisterAll = 4;
		const RequestTypePing = 5;
		const RequestTypeRelay = 6;
		const EventNameJs = "Any2GSX_RelayToJs";
		
		let wasmListener = window.toolbar_interop.register('JS_LISTENER_COMM_BUS', 'fragtality-commbus-js');
		let RegisteredEvents = {};
		let portNumber = 0;

		if(wasmListener) {
			let isJsonString = (str) => {
				try {
					JSON.parse(str);
				}
				catch (e) {
					return false;
				}
				return true;
			};
			
			let CommBusCallback = (evt, data) => {
				var json = {};
				json["event"] = evt;
				json["data"] = data;
				var jsonStr = JSON.stringify(json);
				
				fetch(`http://localhost:${portNumber}/v1/pushevent`, {
					method: "POST",
					mode: "no-cors",
					port: portNumber,
					body: jsonStr,
					headers: {
						"Content-type": "application/json; charset=UTF-8"
					}
				});
				console.log(`Relayed Event: ${evt}`);
			};

			let PingReply = () => {
				fetch(`http://localhost:${portNumber}/v1/ping-reply`, {
						method: "POST",
						mode: "no-cors",
						port: portNumber,
						body: "",
						headers: {
							"Content-type": "application/json; charset=UTF-8"
						}
				});
				console.log(`Ping Reply sent to Port ${portNumber}`);
			};
			
			let AppCallback = (data) => {
				if (isJsonString(data)) {
					var request = JSON.parse(data);
					console.log(`Received Wasm Request - Type: ${request.type} | Event: ${request.event}`)
					if (request.type == RequestTypeRegister) {
						var evtStruct = {
							"name": request.event,
							"callback": (data) => CommBusCallback(request.event, data),
						};
						wasmListener.on(evtStruct.name, evtStruct.callback);
						RegisteredEvents[request.event] = evtStruct;
						console.log("Event added");
					}
					else if (request.type == RequestTypeUnregister) {
						var evtStruct = RegisteredEvents[request.event];
						if (evtStruct != null) {
							wasmListener.off(evtStruct.name, evtStruct.callback);
							RegisteredEvents[request.event] = null;
							console.log("Event removed");
						}
					}
					else if (request.type == RequestTypeUnregisterAll) {
						Object.entries(RegisteredEvents).forEach(([key, value]) => {
							if (value != null)
								wasmListener.off(value.name, value.callback);
						});
						RegisteredEvents = {};
						console.log("All Events removed");
					}
					else if (request.type == RequestTypePing) {
						portNumber = request.data;
						PingReply();
					}
					else if (request.type == RequestTypeRelay) {
						CommBusCallback(request.event, request.data);
					}
					else
						console.error("Received type from Wasm Module is unknown!");
				}
				else
					console.error("Received data from Wasm Module is not a Json!");
			};
			
			wasmListener.on(EventNameJs, AppCallback);
			console.log("CommBus Listener registered");
			//PingReply();
		}
	}

	load_js("/pages/ToolBar/toolbar_interop.js", () => {
		if (window.toolbar_interop) {
			initialize();
		} else {
			document.addEventListener('toolbar_interop_ready', () => {
				initialize();
			});
		}
	});	
})();