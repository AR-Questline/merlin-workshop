using System;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Tutorials.Steps.Composer.Parts {
    [Serializable]
    public class OverlayPart : BasePart {
        public OverlaySettings settings = new OverlaySettings();

        public override bool IsTutorialBlocker => true;

        public override UniTask<bool> OnRun(TutorialContext context) {
            var overlay = settings.Spawn(context.target);
            context.onFinish += overlay.Discard;
            return UniTask.FromResult(true);
        }

        public override void TestRun(TutorialContext context) {
            var overlay = settings.TestSpawn();
            if (overlay != null) {
                context.onFinish += () => GameObjects.DestroySafely(overlay.gameObject);
            }
        }
    }
}