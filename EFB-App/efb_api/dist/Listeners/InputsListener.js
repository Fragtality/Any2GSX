import { Subject, UUID } from '@microsoft/msfs-sdk';
export class InputsListener extends ViewListener.ViewListener {
    static addInputChangeCallback(context, action, callback) {
        const idWatcher = 'InputWatcher_' + context + '_' + action + '_' + UUID.GenerateUuid();
        InputsListener.inputsListener.trigger('ADD_INPUT_WATCHER', idWatcher, context, action);
        InputsListener.inputsListener.on('InputListener.InputChange', (id, down) => {
            if (id === idWatcher) {
                callback(down);
            }
        });
        return idWatcher;
    }
    static removeInputChangeCallback(id) {
        InputsListener.inputsListener.trigger('REMOVE_INPUT_WATCHER', id);
    }
}
InputsListener.isLoaded = Subject.create(false);
InputsListener.inputsListener = RegisterViewListener('JS_LISTENER_INPUTS', () => InputsListener.isLoaded.set(true));
