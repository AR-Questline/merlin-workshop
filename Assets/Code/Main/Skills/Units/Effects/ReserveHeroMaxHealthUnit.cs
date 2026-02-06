using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class ReserveHeroMaxHealthUnit : ARUnit, ISkillUnit {
        protected override void Definition() {
            var floatInput = RequiredARValueInput<float>("MaxHealthReservePercent");
            var maxHealthTweak = ValueOutput<StatTweak>("maxHealthTweak");
            var maxHealthReserveTweak = ValueOutput<StatTweak>("maxHealthReserveTweak");
            DefineSimpleAction(flow => {
                float maxHealthReservePercent = floatInput.Value(flow);
                Hero hero = Hero.Current;
                
                float maxHealth = hero.MaxHealth.ModifiedValue + hero.MaxHealthReservation.ModifiedValue;
                float desiredMaxHealth = maxHealthReservePercent * maxHealth;
                float maxHealthMultiplier = desiredMaxHealth / hero.MaxHealth.ModifiedValue;
                float reservationAmount = maxHealth - desiredMaxHealth;
                
                var healthTweak = StatTweak.Multi(hero.MaxHealth, maxHealthMultiplier, TweakPriority.Multiply, this.Skill(flow));
                var reserveTweak = StatTweak.Add(hero.MaxHealthReservation, reservationAmount, TweakPriority.Add, this.Skill(flow));
                
                flow.SetValue(maxHealthTweak, healthTweak);
                flow.SetValue(maxHealthReserveTweak, reserveTweak);
            });
        }
    }
}