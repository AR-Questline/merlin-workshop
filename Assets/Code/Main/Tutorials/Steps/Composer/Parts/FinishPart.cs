using System;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Tutorials.Steps.Composer.Parts {
    [Serializable]
    public class FinishPart : BasePart {
        public override UniTask<bool> OnRun(TutorialContext context) {
            context.Finish();
            return UniTask.FromResult(false);
        }
    }
}