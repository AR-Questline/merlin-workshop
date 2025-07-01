using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Tutorials.Steps.Composer {
    public interface IStepPart {
        string Name { get; } // must be here because otherwise there are errors in inspector
        bool IsConcurrent { get; }
        UniTask<bool> Run(TutorialContext context);
        void TestRun(TutorialContext context);
        bool IsTutorialBlocker { get; }
    }
}