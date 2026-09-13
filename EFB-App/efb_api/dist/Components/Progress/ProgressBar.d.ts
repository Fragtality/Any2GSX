import { VNode } from '@microsoft/msfs-sdk';
import { ProgressComponent } from './Progress';

export declare class ProgressBar extends ProgressComponent {
    private readonly circularProgressStyle;
    protected updateProgress(progressRatio: number): void;
    render(): VNode;
}
