import { FSComponent, Subject } from '@microsoft/msfs-sdk';
import { GamepadEvents, GamepadUiParser, } from '../Gamepad';
import { UiView } from './UiView';
export class GamepadUiView extends UiView {
    constructor() {
        super(...arguments);
        this.gamepadUiViewRef = FSComponent.createRef();
        this.gamepadUiParser = new GamepadUiParser();
        this._nextHandler = Subject.create(undefined);
        this.nextHandler = this._nextHandler;
    }
    onAfterRender(node) {
        super.onAfterRender(node);
        this.gamepadUiParser.bindVNodeReference(node);
    }
    setNextGamepadEventHandler(ref) {
        this._nextHandler.set(ref);
    }
    deletePreviousGamepadEventHandler() {
        this._nextHandler.set(undefined);
    }
    handleGamepadEvent(_gamepadEvent) {
        if (_gamepadEvent === GamepadEvents.BUTTON_B) {
            this.gamepadUiParser.pushButtonB();
        }
        const nextHandler = this._nextHandler.get();
        if (nextHandler !== undefined) {
            return nextHandler.handleGamepadEvent(_gamepadEvent);
        }
        switch (_gamepadEvent) {
            case GamepadEvents.JOYDIR_UP:
                this.gamepadUiParser.goUp();
                break;
            case GamepadEvents.JOYDIR_RIGHT:
                this.gamepadUiParser.goRight();
                break;
            case GamepadEvents.JOYDIR_DOWN:
                this.gamepadUiParser.goDown();
                break;
            case GamepadEvents.JOYDIR_LEFT:
                this.gamepadUiParser.goLeft();
                break;
            case GamepadEvents.BUTTON_A:
                this.gamepadUiParser.pushButtonA();
                break;
        }
    }
}
