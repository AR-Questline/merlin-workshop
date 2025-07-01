using System;
using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Tutorials.Steps.Composer.Conditions {
    [Serializable]
    public class HasNoEnergyConditionPart : BasePart {
        public override UniTask<bool> OnRun(TutorialContext context) {
            Hero hero = Hero.Current;
            if (hero == null) {
                return UniTask.FromResult(false);
            }

            bool hasNoEnergy = hero.CharacterStats.Stamina <= 0;
            return UniTask.FromResult(hasNoEnergy);
        }
    }
}