import { Subject, VNode } from '@microsoft/msfs-sdk';
import { ProgressComponent, ProgressComponentProps } from './Progress';

interface CircularProgressProps extends ProgressComponentProps {
    iconPath?: string;
}
export declare class CircularProgress extends ProgressComponent<CircularProgressProps> {
    protected readonly circularProgressRef: import('@microsoft/msfs-sdk').NodeReference<HTMLDivElement>;
    protected readonly lowerSliceRef: import('@microsoft/msfs-sdk').NodeReference<HTMLDivElement>;
    protected readonly higherSliceRef: import('@microsoft/msfs-sdk').NodeReference<HTMLDivElement>;
    protected readonly sliceClassToggleSub: Subject<boolean>;
    protected readonly sliceClassToggleNotSub: import('@microsoft/msfs-sdk').MappedSubscribable<boolean>;
    private readonly circularProgressStyle;
    private readonly circularProgressPrimaryBackgroundClass;
    private readonly circularProgressSecondaryBackgroundClass;
    private readonly circularProgressSliceClasses;
    private updateProgressByHalf;
    protected updateProgress(progressRatio: number): void;
    render(): VNode;
}
export {};
