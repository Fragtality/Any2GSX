export type Writeable<T> = {
    -readonly [P in keyof T]: T[P];
};
//# sourceMappingURL=Writeable.d.ts.map