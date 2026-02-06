using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC;
using DG.Tweening;
using Unity.Mathematics;

namespace Awaken.TG.Main.Heroes.HUD {
    public class VCHeroStaminaBar : VCStatBarWithFail {
        protected override StatType StatType => CharacterStatType.Stamina;
        protected override float Percentage => math.clamp(Target.Stamina?.Percentage ?? 1f, 0f, 1f);
        protected override float PredictionPercentage => (Target.Stamina?.ModifiedValue + Target.StaminaRegen?.PredictedModification) / Target.Stamina?.UpperLimit ?? 1f;

        protected override void HandlePlayAnimationRestriction(out bool canPlayAnimation) {
            HandleHoldAnimationRestriction();
            base.HandlePlayAnimationRestriction(out canPlayAnimation);
        }
    }
}