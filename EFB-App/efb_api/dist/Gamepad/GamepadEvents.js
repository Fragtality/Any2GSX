/** @internal */
export var GamepadEvents;
(function (GamepadEvents) {
    GamepadEvents["JOYSTICK_LEFT_X_AXIS"] = "JOYSTICK_LEFT_X_AXIS";
    GamepadEvents["JOYSTICK_LEFT_Y_AXIS"] = "JOYSTICK_LEFT_Y_AXIS";
    GamepadEvents["JOYSTICK_RIGHT_X_AXIS"] = "JOYSTICK_RIGHT_X_AXIS";
    GamepadEvents["JOYSTICK_RIGHT_Y_AXIS"] = "JOYSTICK_RIGH_Y_AXIS";
    GamepadEvents["BUTTON_A"] = "BUTTON_A";
    GamepadEvents["BUTTON_B"] = "BUTTON_B";
    GamepadEvents["BUTTON_Y"] = "BUTTON_Y";
    GamepadEvents["BUTTON_X"] = "BUTTON_X";
    GamepadEvents["JOYDIR_LEFT"] = "JOYDIR_LEFT";
    GamepadEvents["JOYDIR_RIGHT"] = "JOYDIR_RIGHT";
    GamepadEvents["JOYDIR_UP"] = "JOYDIR_UP";
    GamepadEvents["JOYDIR_DOWN"] = "JOYDIR_DOWN";
})(GamepadEvents || (GamepadEvents = {}));
/**
 * Je veux pouvoir créer des assemblages d'events qui indiquent
 * les genre d'events que je peux recevoir.
 * Donc un set d'event est un la définition de plusieurs
 */
