using System;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Tutorials.Steps.Composer.Conditions {
    [Serializable]
    public class CharacterSheetConditionPart : BasePart {
        public override UniTask<bool> OnRun(TutorialContext context) {
            return UniTask.FromResult(World.HasAny<CharacterSheetUI>());
        }
    }
}