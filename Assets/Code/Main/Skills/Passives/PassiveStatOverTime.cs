using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Skills.Passives {
    public partial class PassiveStatOverTime : Element<Skill>, IPassiveEffect {
        public sealed override bool IsNotSaved => true;

        protected readonly Stat _stat;
        readonly Stat _statModifier;
        readonly float _changePerSecond;

        protected PassiveStatOverTime(Stat stat, float changePerSecond, Stat modifierStat = null) {
            _stat = stat;
            _changePerSecond = changePerSecond;
            _statModifier = modifierStat;
        }

        public static PassiveStatOverTime Create(Stat stat, float changePerSecond, Stat modifierStat = null) =>
            (stat, changePerSecond) switch {
                (LimitedStat, < 0) when stat.Type == AliveStatType.Health => new PassiveDamageOverTime(stat, changePerSecond, modifierStat),
                _ => new PassiveStatOverTime(stat, changePerSecond, modifierStat)
            };

        protected override void OnInitialize() {
            var timeDependent = ParentModel.Owner.GetOrCreateTimeDependent();
            timeDependent.WithUpdate(Update);
            RefreshPrediction();
        }

        void Update(float deltaTime) {
            float valueToIncrease = _changePerSecond * deltaTime;
            if (_statModifier != null) {
                valueToIncrease *= _statModifier / 100f;
            }

            ChangeStatValue(valueToIncrease);
            RefreshPrediction();
        }

        protected virtual void ChangeStatValue(float valueToIncrease) {
            _stat.IncreaseBy(valueToIncrease);
        }

        void RefreshPrediction() {
            if (ParentModel?.ParentModel is Status status) {
                float? duration = status.TimeLeftSeconds;
                if (duration.HasValue) {
                    float toRegen = _changePerSecond * duration.Value;
                    _stat.SetPrediction(this, toRegen);
                }
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            ParentModel.Owner.GetTimeDependent()?.WithoutUpdate(Update);
        }
    }
}