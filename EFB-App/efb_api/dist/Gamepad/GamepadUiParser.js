import { FSComponent } from '@microsoft/msfs-sdk';
import { GamepadUiComponent } from './GamepadUiComponent';
/** @internal */
export class GamepadUiParser {
    constructor() {
        this.gamepadUiViewVNode = null;
    }
    /** On veut focus le premier élément */
    bindVNodeReference(gamepadUiViewVNode) {
        this.gamepadUiViewVNode = gamepadUiViewVNode;
        this.focusFirstElement();
    }
    focusFirstElement() {
        if ((this.currentElement = this.findFirstElement())) {
            this.currentElement.instance.toggleFocus(true);
        }
    }
    goUp() {
        this.goDir('UP');
    }
    goRight() {
        this.goDir('RIGHT');
    }
    goDown() {
        this.goDir('DOWN');
    }
    goLeft() {
        this.goDir('LEFT');
    }
    pushButtonA() {
        var _a;
        (_a = this.currentElement) === null || _a === void 0 ? void 0 : _a.instance.onButtonAPressed();
    }
    pushButtonB() {
        var _a;
        (_a = this.currentElement) === null || _a === void 0 ? void 0 : _a.instance.onButtonBPressed();
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
            case 'UP':
                candidateIndex = this.findBestCandidate(currentGamepadUiComponent, gamepadUiComponentList, 'bottom', 'top');
                break;
            case 'RIGHT':
                candidateIndex = this.findBestCandidate(currentGamepadUiComponent, gamepadUiComponentList, 'left', 'right');
                break;
            case 'DOWN':
                candidateIndex = this.findBestCandidate(currentGamepadUiComponent, gamepadUiComponentList, 'top', 'bottom');
                break;
            case 'LEFT':
                candidateIndex = this.findBestCandidate(currentGamepadUiComponent, gamepadUiComponentList, 'right', 'left');
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
        gamepadUiComponentList.forEach((htmlElement, index) => {
            const htmlElementRect = htmlElement.instance.getComponentRect();
            if (this.isRectPosValid(htmlElementRect, currentHTMLElementRect, side1, side2, false)) {
                if (!htmlElementCandidates.some((htmlElementCandidate) => this.isRectPosValid(htmlElementRect, htmlElementCandidate.instance.getComponentRect(), side1, side1, true))) {
                    htmlElementCandidates.push(htmlElement);
                    const corner = side1 === 'bottom' || side1 === 'top' ? 'left' : 'top';
                    const distance = this.distances1D(htmlElementRect, currentHTMLElementRect, corner);
                    if (distance <= closestDistance) {
                        closestDistance = distance;
                        candidateIndex = index;
                    }
                }
            }
        });
        return candidateIndex;
    }
    isRectPosValid(rect1, rect2, side1, side2, strict) {
        if (side1 === 'bottom' || side1 === 'right') {
            if (strict) {
                return rect1[side1] < rect2[side2];
            }
            else {
                return rect1[side1] <= rect2[side2];
            }
        }
        else {
            if (strict) {
                return rect1[side1] > rect2[side2];
            }
            else {
                return rect1[side1] >= rect2[side2];
            }
        }
    }
    distances1D(rect1, rect2, side) {
        return Math.abs(rect2[side] - rect1[side]);
    }
}
