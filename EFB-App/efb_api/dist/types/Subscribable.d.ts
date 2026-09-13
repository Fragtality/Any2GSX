import { Subscribable } from '@microsoft/msfs-sdk';

export type MaybeSubscribable<T> = T | Subscribable<T>;
