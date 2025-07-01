using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    public class SpendManaCostUnit : ARUnit, ISkillUnit {
        protected override void Definition() {
            var additionalMultiplier = FallbackARValueInput("additionalMultiplier", _ => 1f);
            var success = ControlOutput("Success");
            var fail = ControlOutput("Fail");
            
            var spendManaCost = ControlInput("Spend ManaCost", flow => {
                bool manaSpend = SpendLightManaCost(flow, additionalMultiplier.Value(flow));
                return manaSpend ? success : fail;
            });
            var spendHeavyManaCost = ControlInput("Spend HeavyManaCost", flow => {
                bool manaSpend = SpendHeavyManaCost(flow, additionalMultiplier.Value(flow));
                return manaSpend ? success : fail;
            });
            var spendManaCostPerSecond = ControlInput("Spend ManaCostPerSecond", flow => {
                bool manaSpend = SpendManaCostPerSecond(flow, additionalMultiplier.Value(flow));
                return manaSpend ? success : fail;
            });
            
            Succession(spendManaCost, success);
            Succession(spendManaCost, fail);
            Succession(spendHeavyManaCost, success);
            Succession(spendHeavyManaCost, fail);
            Succession(spendManaCostPerSecond, success);
            Succession(spendManaCostPerSecond, fail);
        }

        bool SpendLightManaCost(Flow flow, float additionalMultiplier) {
            return SpendManaCost(flow, this.GetLightManaCost(flow) * additionalMultiplier);
        }
        
        bool SpendHeavyManaCost(Flow flow, float additionalMultiplier) {
            return SpendManaCost(flow, this.GetHeavyManaCost(flow) * additionalMultiplier);
        }

        bool SpendManaCostPerSecond(Flow flow, float additionalMultiplier) {
            return SpendManaCost(flow, this.GetHeavyManaCostPerSecond(flow) * additionalMultiplier);
        }

        bool SpendManaCost(Flow flow, float manaCost) {
            var skill = this.Skill(flow);
            return SpendMana(skill, skill.Owner.Stat(CharacterStatType.Mana), skill.Owner, manaCost, true);
        }
        
        protected virtual bool SpendMana(Skill skill, Stat stat, ICharacter owner, float manaCost, bool perSecond) {
            if (stat == null || !(stat.ModifiedValue >= manaCost)) {
                if (owner is Hero h) {
                    h.Trigger(Hero.Events.StatUseFail, CharacterStatType.Mana);
                    h.Trigger(Hero.Events.NotEnoughMana, manaCost);
                }
                return false;
            }

            float previousValue = stat.BaseValue;
            stat.DecreaseBy(manaCost);
            float currentValue = stat.BaseValue;
            if (owner is Hero hero) {
                hero.Trigger(Hero.Events.ManaSpend, new ManaSpendData(skill, previousValue, currentValue, perSecond));
            }
            return true;
        }

        public readonly struct ManaSpendData {
            [UnityEngine.Scripting.Preserve] public readonly Skill skill;
            public readonly float previousValue;
            public readonly float currentValue;
            [UnityEngine.Scripting.Preserve] public readonly bool perSecond;
            
            [UnityEngine.Scripting.Preserve] public float Amount => previousValue - currentValue;
            
            public ManaSpendData(Skill skill, float previousValue, float currentValue, bool perSecond) {
                this.skill = skill;
                this.previousValue = previousValue;
                this.currentValue = currentValue;
                this.perSecond = perSecond;
            }
        }
    }
}