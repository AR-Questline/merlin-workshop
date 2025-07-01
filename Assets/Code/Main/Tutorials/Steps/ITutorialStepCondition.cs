namespace Awaken.TG.Main.Tutorials.Steps {
    public interface IUITutorialStepCondition {
        bool CanRun(ITutorialStep step);
    }
}