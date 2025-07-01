using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Tutorials.Steps.Composer.Parts {
    [Serializable]
    public class DelayPart : BasePart {
        [InfoBox("All delays are in milliseconds")]
        public NormalizedDelay normalizedDelay = NormalizedDelay.Custom;
        [ShowIf("@normalizedDelay==NormalizedDelay.Custom")]
        public int delay;
        [ShowInInspector, ShowIf("@normalizedDelay!=NormalizedDelay.Custom")]
        public int DelayDuration => MapDelay(normalizedDelay);
        
        public override async UniTask<bool> OnRun(TutorialContext context) {
            int realDelay = normalizedDelay == NormalizedDelay.Custom ? this.delay : MapDelay(normalizedDelay); 
            await UniTask.Delay(realDelay);
            return true;
        }

        public static int MapDelay(NormalizedDelay nd) {
            return nd switch {
                NormalizedDelay.Short => 3000,
                NormalizedDelay.Medium => 6000,
                NormalizedDelay.Long => 10000,
                _ => 1,
            };
        }

        public enum NormalizedDelay {
            Custom = 0,
            Short = 1,
            Medium = 2,
            Long = 3,
        }
    }
}