using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Saving;
using UnityEngine;

namespace Awaken.TG.Main.Skills.Passives {
    public partial class PassiveDamageOverTime : PassiveStatOverTime {
        readonly IAlive _statOwner;

        public PassiveDamageOverTime(Stat stat, float changePerSecond, Stat modifierStat = null) : base(stat, changePerSecond, modifierStat) {
            _statOwner = (IAlive)_stat.Owner;
        }

        protected override void ChangeStatValue(float valueToIncrease) {
            float amount = Mathf.Abs(valueToIncrease);
            Damage damage = new(DamageParameters.PassiveDamageOverTime, null, _statOwner, new RawDamageData(amount));
            _statOwner.HealthElement.TakeDamage(damage);
        }
    }
}