
class Any2GsxPanel extends TemplateElement {
	constructor() {
		super(...arguments);
	}

	connectedCallback() {
		super.connectedCallback();
		this.ingameUi = this.querySelector('ingame-ui');

		if (this.ingameUi) {
			this.ingameUi.addEventListener("panelActive", (e) => {
				this.ingameUi.style.display = "none";
			});
		}
	}
}

window.customElements.define("any2gsx-panel", Any2GsxCommBusPanel);
checkAutoload();