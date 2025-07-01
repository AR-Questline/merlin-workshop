using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Tutorials.Steps.Conditions {
    public class TSConditionIsEnemyHUD : MonoBehaviour, IUITutorialStepCondition {
        public bool CanRun(ITutorialStep step) {
            return step is ViewComponent {GenericTarget: NpcElement _};
        }
    }
}