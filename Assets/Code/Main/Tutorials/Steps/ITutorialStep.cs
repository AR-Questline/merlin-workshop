using System;
using Awaken.TG.Main.Tutorials.Steps.Composer;

namespace Awaken.TG.Main.Tutorials.Steps {
    public interface ITutorialStep {
        string Key { get; }
        bool CanBePerformed { get; }
        TutorialContext Perform(Action onFinish);
        void Accompany(TutorialContext context);
    }
}