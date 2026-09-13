import { ComponentProps, DisplayComponent } from '@microsoft/msfs-sdk';
import { TTButtonProps } from '../../Components';
import { Nullable, TVNode } from '../../types';

export type OnboardingStepType = 'floating' | 'centered' | 'initial';
export type OnboardingStepPlacement = 'top' | 'bottom' | 'left' | 'right';
export interface OnboardingStep {
    title?: string;
    targetElementId?: string;
    type: OnboardingStepType;
    actions: TTButtonProps[];
    openPageOnStepSetProps?: [appKey: string, pageKey: Nullable<string>, args: Nullable<string[]>];
    placement?: OnboardingStepPlacement;
    description: string | string[];
}
export interface CenteredOnboardingStep extends OnboardingStep {
    title: string;
    type: 'centered';
}
export interface FloatingOnboardingStep extends OnboardingStep {
    title: string;
    type: 'floating';
    targetElementId: string;
    placement: OnboardingStepPlacement;
}
export interface InitialOnboardingStep extends OnboardingStep {
    type: 'floating';
    targetElementId: string;
    placement: OnboardingStepPlacement;
}
export interface Onboarding {
    key: string;
    steps: OnboardingStep[];
    onFinish?: () => void;
}
export interface OnboardingContainerComponent extends DisplayComponent<ComponentProps> {
    show(): void;
    hide(): void;
    setStep(step: OnboardingStep): void;
}
export declare class OnboardingManager {
    private static INSTANCE;
    static getManager(): OnboardingManager;
    private isStarted;
    private stepIndex;
    private steps;
    private containerRef?;
    private onFinish?;
    private constructor();
    bindContainer(containerRef: TVNode<OnboardingContainerComponent>): void;
    start(onboarding: Onboarding): void;
    private next;
    private stop;
}
