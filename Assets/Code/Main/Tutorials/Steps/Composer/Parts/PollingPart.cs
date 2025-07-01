using System;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Tutorials.Steps.Composer.Parts {
    [Serializable]
    public class PollingPart : BasePart {
        public int interval = 500;
        
        public override async UniTask<bool> OnRun(TutorialContext context) {
            while (!context.IsDone && context.vc != null) {
                foreach (var part in parts) {
                    if (context.vc == null) {
                        return false;
                    }
                    await part.Run(context);
                }
                await UniTask.Delay(interval);
            }
            return true;
        }
    }
}