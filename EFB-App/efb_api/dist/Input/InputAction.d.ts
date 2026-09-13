export declare enum InputAction {
    /************/
    /************/
    SECONDARY = 0,// X
    TERTIARY = 1,// Y
    PREV = 2,// LB
    NEXT = 3,// RB
    PREV_SUB = 4,// LT
    NEXT_SUB = 5,// RT
    ZOOM_OUT = 6,// LT
    ZOOM_IN = 7,// RT
    /************/
    /************/
    SCROLL_UP = 8,
    SCROLL_RIGHT = 9,
    SCROLL_DOWN = 10,
    SCROLL_LEFT = 11
}
export declare const InputActions: Record<InputAction, string>;
export declare enum InternalInputAction {
    /************/
    /************/
    VALID = 0,// A
    BACK = 1,// B
    SECONDARY = 2,// X
    TERTIARY = 3,// Y
    PREV_TAB = 4,// LB
    NEXT_TAB = 5,// RB
    ZOOM_OUT = 6,// LT
    ZOOM_IN = 7,// RT
    UP = 8,
    RIGHT = 9,
    DOWN = 10,
    LEFT = 11,
    PREV_APP = 12,// left
    NEXT_APP = 13,// right
    /************/
    /************/
    OPEN_NOTIFICATION_MENU = 14,// Press
    SCROLL_UP = 15,
    SCROLL_RIGHT = 16,
    SCROLL_DOWN = 17,
    SCROLL_LEFT = 18,
    /************/
    /************/
    TEXTBOX_VALID = 19,
    TEXTBOX_UP = 20,
    TEXTBOX_RIGHT = 21,
    TEXTBOX_DOWN = 22,
    TEXTBOX_LEFT = 23
}
export declare const InternalInputActions: Record<InternalInputAction, string>;
