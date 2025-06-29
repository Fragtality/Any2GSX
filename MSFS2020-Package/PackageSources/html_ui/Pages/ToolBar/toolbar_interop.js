class ToolBarInterop {

	constructor() {
		this.registered_listeners = {};
	}

	/**
	 * Get the listener and the names of existing clients
	 * @param {String} name - The name of the listener
	 * @return {Object} - The listener and the names of existing clients
	 */
	get_listener(name) {
		let listener = this.registered_listeners[name];
		if(listener) {
			return {
				listener: listener.view_listener,
				is_refreshing: listener.is_refreshing,
				clients: listener.clients.map(x => x.client_name),
			};
		} else {
			return {
				listener: null,
				is_refreshing: false,
				clients: [],
			};
		}
	}

	/**
	 * Register a new client with the given name
	 * @param {String} name - The name of the listener
	 * @param {String} client_name - The name of the client
	 * @param {Function} callback - The callback to be called with the listener and the names of existing clients
	 * @return {ViewListener} - The listener that was created or found
	 */
	register(name, client_name, callback = null) {

		// Find an existing listener with the given name
		let listener = this.registered_listeners[name];

		// If no listener exists, create a new one
		if(!listener) {
			listener = {
				view_listener: null,
				is_refreshing: false,
				clients: [],
			};
			this.registered_listeners[name] = listener;
		}

		// Find an existing client with the given name
		let client = listener.clients.find((client) => client.client_name === client_name);

		// If no client exists, create a new one
		if(!client) {
			client = {
				client_name: client_name,
			};
			listener.clients.push(client);
		}

		// Register the listener
		if(!listener.view_listener) {
			// Register the listener
			listener.view_listener = RegisterViewListener(name, () => {
				// Return the listener and the name of existing clients
				if(callback) callback(this.get_listener(name));
			})
		} else {
			// Return the listener and the name of existing clients
			if(callback) callback(this.get_listener(name));
		}

		// Return the listener
		return listener.view_listener;
	}

	/**
	 * Refresh the listener by unregistering and re-registering it
	 * @param {String} name - The name of the listener
	 * @param {Function} callback - The callback to be called with the listener and the names of existing clients
	 * @return {ViewListener} - The listener that was re-registered
	 */
	refresh(name, callback = null) {

		// Find an existing listener with the given name
		const listener = this.registered_listeners[name];

		// If no listener exists, return
		if(!listener) return;

		if(listener.is_refreshing) return;
		listener.is_refreshing = true;

		// Copy m_handlers from the old listener
		const m_handlers = [...listener.view_listener.m_handlers];

		// Unregister the listener
		if(listener.view_listener)
			listener.view_listener.unregister();

		window.requestAnimationFrame(() => {
			// Register the listener
			listener.view_listener = RegisterViewListener(name, () => {})

			// Restore m_handlers
			listener.view_listener.m_handlers = m_handlers;

			// reset the global event handlers
			m_handlers.forEach(handler => {
				handler.globalEventHandler = Coherent.on(handler.name, listener.view_listener.onGlobalEvent.bind(listener.view_listener, handler.name))
			});

			// Return the listener and the name of existing clients
			listener.is_refreshing = false;
			if(callback) callback(this.get_listener(name));
		});

	}

	/**
	 * Unregister the client with the given name
	 * @param {String} name - The name of the listener
	 * @param {String} client_name - The name of the client
	 * @return {Number} - How many clients are left after the unregistering. Returns -1 if the listener does not exist, and -2 if the client does not exist.
	 */
	unregister(name, client_name) {

		// Find an existing listener with the given name
		const listener = this.registered_listeners[name];

		// If no listener exists, return
		if(!listener) return -1;

		// Find an existing client with the given name
		const client = listener.clients.find((client) => client.client_name === client_name);

		// If no client exists, return
		if(!client) return -2;

		// Remove the client
		listener.clients = listener.clients.filter((client) => client.client_name !== client_name);

		// If no clients remain, unregister the listener
		if(listener.clients.length === 0)
			if(listener.view_listener)
				listener.view_listener.unregister();

		// We're done, return true
		return listener.clients.length;
	}

}

if(!window.toolbar_interop) {
	window.toolbar_interop = new ToolBarInterop();
	document.dispatchEvent(new Event('toolbar_interop_ready'));
}