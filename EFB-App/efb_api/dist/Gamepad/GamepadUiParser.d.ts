import { VNode } from '@microsoft/msfs-sdk';

/** @internal */
export declare class GamepadUiParser {
    private currentElement?;
    private gamepadUiViewVNode;
    /** On veut focus le premier élément */
    bindVNodeReference(gamepadUiViewVNode: VNode): void;
    private focusFirstElement;
    goUp(): void;
    goRight(): void;
    goDown(): void;
    goLeft(): void;
    pushButtonA(): void;
    pushButtonB(): void;
    private goDir;
    private parseDOM;
    private findFirstElement;
    /** On réduit la liste dont le top est en dessous du bottom du current node. */
    private findClosestNode;
    private findBestCandidate;
    private isRectPosValid;
    private distances1D;
}
