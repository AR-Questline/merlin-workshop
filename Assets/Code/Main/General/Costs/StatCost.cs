using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Skills.Units.Effects;
using Awaken.TG.MVC;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.General.Costs {
    /// <summary>
    /// Model of cost that consists of stat it changes and amount.
    /// </summary>
    public class StatCost : ICost {
        // === Properties

        float _amount;

        public float Amount {
            get => Reason == ChangeReason.Trade ? _amount : Mathf.Max(0, _amount);
            private set => _amount = value;
        }

        public Stat Stat { get; private set; }
        ContractContext Context { get; set; }
        Skill Skill { get; }

        ChangeReason Reason => Context?.reason ?? ChangeReason.Skill;

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        StatCost() { }

        public StatCost(Stat stat, float amount, ContractContext context = null, Skill skill = null) {
            Stat = stat;
            Amount = amount;
            Context = context;
            Skill = skill;
        }

        // === Operations

        public bool CanAfford() {
            return Stat >= Amount;
        }

        [UnityEngine.Scripting.Preserve] public void ChangeAmount(float amount) => Amount = amount;
        [UnityEngine.Scripting.Preserve] public void ChangeContext(ContractContext context) => Context = context;

        public void Pay() {
            if (Stat.Type == CharacterStatType.Mana && Stat.Owner is Hero hero) {
                float previousValue = Stat.BaseValue;
                Stat.DecreaseBy(Amount, Context);
                float currentValue = Stat.BaseValue;
                hero.Trigger(Hero.Events.ManaSpend, new SpendManaCostUnit.ManaSpendData(Skill, previousValue, currentValue, false));
            } else {
                Stat.DecreaseBy(Amount, Context);
            }
        }

        public void Refund() {
            Stat.IncreaseBy(Amount, Context);
        }

        public bool TryStack(ICost cost) {
            if (cost is StatCost statCost && statCost.Stat == Stat) {
                _amount += statCost._amount;
                return true;
            }

            return false;
        }

        public ICost Clone() {
            return new StatCost(Stat, _amount, Context, Skill);
        }

        public float CombinedStatCost(StatType statType) {
            return Stat.Type == statType ? Amount : 0;
        }

        public override string ToString() {
            return $"{Amount}{Stat.Type.IconTag}";
        }
    }
}