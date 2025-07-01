using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Unity.Mathematics;

namespace Awaken.TG.Main.Heroes.HUD {
    public class VCHeroHealthBar : VCHeroHUDBar {
        protected override StatType StatType => AliveStatType.Health;
        protected override float Percentage => math.clamp(Target.Health?.Percentage ?? 1f, 0f, 1f);
        protected override float PredictionPercentage => (Target.Health?.ModifiedValue + Target.HealthRegen?.PredictedModification) / Target.Health?.UpperLimit ?? 1f;
    }
}