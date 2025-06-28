export class OnboardingManager {
    static getManager() {
        var _a;
        return ((_a = OnboardingManager.INSTANCE) !== null && _a !== void 0 ? _a : (OnboardingManager.INSTANCE = new OnboardingManager()));
    }
    constructor() {
        this.isStarted = false;
        this.stepIndex = 0;
        this.steps = [];
        // just making constructor private
    }
    bindContainer(containerRef) {
        OnboardingManager.getManager().containerRef = containerRef;
    }
    start(onboarding) {
        const onboardingManager = OnboardingManager.getManager();
        // deep copy of onboarding steps
        onboardingManager.steps = onboarding.steps.map((step) => {
            return Object.assign(Object.assign({}, step), { actions: [...step.actions] });
        });
        if (!onboardingManager.containerRef) {
            console.warn('Onboarding container not bound');
            return;
        }
        if (!onboardingManager.steps.length) {
            console.warn('No onboarding steps provided');
            return;
        }
        if (onboardingManager.isStarted) {
            console.warn('Onboarding already started');
            return;
        }
        onboardingManager.stepIndex = 0;
        onboardingManager.isStarted = true;
        onboardingManager.onFinish = onboarding.onFinish;
        onboardingManager.steps[onboardingManager.stepIndex].actions.unshift({
            key: 'QUICK TOUR AROUND' /** TOTT */,
            callback: () => {
                onboardingManager.next();
            },
        }, {
            key: 'SKIP' /** TOTT */,
            callback: () => {
                onboardingManager.stop();
            },
        });
        onboardingManager.containerRef.instance.show();
        onboardingManager.containerRef.instance.setStep(onboardingManager.steps[onboardingManager.stepIndex]);
        onboardingManager.stepIndex++;
        return;
    }
    next() {
        var _a;
        const onboardingManager = OnboardingManager.getManager();
        if (!onboardingManager.isStarted) {
            console.warn('Onboarding not started. You should call start method first');
            return;
        }
        if (onboardingManager.stepIndex >= onboardingManager.steps.length) {
            onboardingManager.stop();
            return;
        }
        onboardingManager.steps[onboardingManager.stepIndex].actions.unshift({
            key: 'NEXT' /** TOTT */,
            callback: () => {
                onboardingManager.next();
            },
        });
        (_a = onboardingManager.containerRef) === null || _a === void 0 ? void 0 : _a.instance.setStep(onboardingManager.steps[onboardingManager.stepIndex]);
        onboardingManager.stepIndex++;
        return;
    }
    stop() {
        var _a, _b;
        const onboardingManager = OnboardingManager.getManager();
        onboardingManager.isStarted = false;
        onboardingManager.steps = [];
        (_a = onboardingManager.containerRef) === null || _a === void 0 ? void 0 : _a.instance.hide();
        (_b = onboardingManager.onFinish) === null || _b === void 0 ? void 0 : _b.call(onboardingManager);
    }
}
OnboardingManager.INSTANCE = undefined;
