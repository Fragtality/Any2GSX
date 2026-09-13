/** @internal */
export declare enum GamepadEvents {
    JOYSTICK_LEFT_X_AXIS = "JOYSTICK_LEFT_X_AXIS",
    JOYSTICK_LEFT_Y_AXIS = "JOYSTICK_LEFT_Y_AXIS",
    JOYSTICK_RIGHT_X_AXIS = "JOYSTICK_RIGHT_X_AXIS",
    JOYSTICK_RIGHT_Y_AXIS = "JOYSTICK_RIGH_Y_AXIS",
    BUTTON_A = "BUTTON_A",
    BUTTON_B = "BUTTON_B",
    BUTTON_Y = "BUTTON_Y",
    BUTTON_X = "BUTTON_X",
    JOYDIR_LEFT = "JOYDIR_LEFT",
    JOYDIR_RIGHT = "JOYDIR_RIGHT",
    JOYDIR_UP = "JOYDIR_UP",
    JOYDIR_DOWN = "JOYDIR_DOWN"
}
type Event<T extends string> = {
    name: T;
    value: boolean | number;
};
type UniqueEventArray<T extends string> = Array<Event<T>>;
/** @internal */
export type LeftJoystickEvents = UniqueEventArray<'joystick_left_x_axis' | 'joystick_left_y_axis' | 'joystick_left_push'>;
/** @internal */
export type RightJoystickEvents = UniqueEventArray<'joystick_right_x_axis' | 'joystick_right_y_axis' | 'joystick_right_push'>;
/** @internal */
export type DirpadEvents = UniqueEventArray<'dirpad_top_push' | 'dirpad_right_push' | 'dirpad_bottom_push' | 'dirpad_left_push'>;
export {};
/**
 * Je veux pouvoir créer des assemblages d'events qui indiquent
 * les genre d'events que je peux recevoir.
 * Donc un set d'event est un la définition de plusieurs
 */
