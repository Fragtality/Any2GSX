/** Mark properties as required */
export type RequiredProps<T, K extends keyof T = keyof T> = T & Required<Pick<T, K>>;
//# sourceMappingURL=RequiredField.d.ts.map