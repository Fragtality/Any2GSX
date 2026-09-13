import { ComponentProps, DisplayComponent, VNode } from '@microsoft/msfs-sdk';

/**
 * The ScrollBar props interface
 * @internal
 */
export interface ScrollBarProps extends ComponentProps {
    /** an optionnal custom class */
    class?: string;
}
export declare class ScrollBar extends DisplayComponent<ComponentProps> {
    private readonly svgRef;
    private readonly scrollBarRef;
    private readonly scrollThumbRef;
    private readonly scrollBarContainerRef;
    private scrollableContainer;
    private readonly scrollListener;
    private sizeChangeTimer;
    private readonly arrowPadding;
    private readonly margin;
    private currentScrollHeight;
    private currentThumbAreaHeight;
    private scrollHeightRatio;
    onAfterRender(node: VNode): void;
    /**
     * Adjusts the dimensions of the scrollbar elements.
     */
    private adjustScrollbarDimensions;
    /**
     * Eventhandler called when a scroll event in the scrollable parent container happens.
     */
    private onScroll;
    render(): VNode;
    destroy(): void;
}
