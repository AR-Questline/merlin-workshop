using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Unity.Mathematics;

namespace Awaken.TG.Main.Heroes.HUD {
    public class VCHeroManaBar : VCStatBarWithFail {
        protected override StatType StatType => CharacterStatType.Mana;
        protected override float Percentage => math.clamp(Target.Mana?.ModifiedValue / Target.MaxManaWithReservation ?? 1f, 0f, 1f);
        protected override float PredictionPercentage => (Target.Mana?.ModifiedValue + Target.PredictedManaRegen) / Target.MaxManaWithReservation ?? 1f;
    }
}   