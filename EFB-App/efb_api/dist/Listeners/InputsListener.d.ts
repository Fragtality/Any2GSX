import { Subject } from '@microsoft/msfs-sdk';

export declare class InputsListener extends ViewListener.ViewListener {
    static isLoaded: Subject<boolean>;
    private static inputsListener;
    static addInputChangeCallback(context: string, action: string, callback: (down: boolean) => void): string;
    static removeInputChangeCallback(id: string): void;
}
//# sourceMappingURL=InputsListener.d.ts.map