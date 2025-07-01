using System;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.States;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Tutorials.Steps.Composer.Conditions {
    [Serializable]
    public class MapInteractiveConditionPart : BasePart {
        public override UniTask<bool> OnRun(TutorialContext context) {
            return UniTask.FromResult(UIStateStack.Instance.State.IsMapInteractive);
        }
    }
}