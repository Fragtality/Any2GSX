import { ObjectSubject, StyleRecord, Subscribable, SubscribableMap, ToggleableClassNameRecord } from '@microsoft/msfs-sdk';

export type ClassProp = undefined | string | string[] | ToggleableClassNameRecord;
export type StyleProp = undefined | string | Subscribable<string> | SubscribableMap<string, string> | ObjectSubject<any> | StyleRecord;
