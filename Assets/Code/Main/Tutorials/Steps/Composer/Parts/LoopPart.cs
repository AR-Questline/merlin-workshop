using System;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Tutorials.Steps.Composer.Parts {
    [Serializable]
    public class LoopPart : BasePart {
        public override async UniTask<bool> OnRun(TutorialContext context) {
            while (!context.IsDone) {
                foreach (var part in parts) {
                    await part.Run(context);
                }
            }
            return true;
        }
    }
}