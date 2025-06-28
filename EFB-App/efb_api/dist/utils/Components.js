export function isVNode(object) {
    return 'instance' in object && 'props' in object && 'children' in object;
}
export function isIApp(object) {
    return 'internalName' in object && 'BootMode' in object;
}
export function isConstructor(func) {
    return typeof func === 'function' && !!func.prototype && func.prototype.constructor === func;
}
/**
 * Check if argument `fn` is a function.
 * @template T Expected type of `fn`.
 * @param value
 */
export function isFunction(fn) {
    return typeof fn === 'function';
}
export function toArray(list, start = 0) {
    let i = list.length - start;
    const ret = new Array(i);
    while (i--) {
        ret[i] = list[i + start];
    }
    return ret;
}
/**
 * @param classProp Convert {string} to {ToggleableClassNameRecord}
 * @returns Converted ClassProp
 */
export function toClassProp(classProp) {
    if (classProp === undefined) {
        return classProp;
    }
    if (Array.isArray(classProp)) {
        return toClassProp(classProp.join(' '));
    }
    if (typeof classProp !== 'string') {
        return classProp;
    }
    return classProp
        .split(' ')
        .reduce(function (stack, el) {
        return Object.assign(Object.assign({}, stack), { [el]: true });
    }, {});
}
/**
 *
 * @param baseProp Base props to merge
 * @param args Array of ClassProp to merge onto baseProp
 * @returns Merged ClassProps
 */
export function mergeClassProp(baseProp, ...args) {
    const mergedClassProp = Object.assign({}, toClassProp(baseProp));
    for (const arg of args) {
        Object.assign(mergedClassProp, toClassProp(arg));
    }
    return mergedClassProp;
}
