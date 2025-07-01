using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Skills.Passives {
    public partial class PassiveStatModifier : Element<Skill>, IPassiveEffect, ITweaker {
        public sealed override bool IsNotSaved => true;

        readonly Stat _stat;
        readonly TweakPriority _type;
        readonly float _value;
        
        public float Value => _value;
        public Stat Stat => _stat;
        
        public OperationType OperationType { get; private set; }

        public PassiveStatModifier(Stat stat, TweakPriority type, float value) {
            _stat = stat;
            _type = type;
            _value = value;

            OperationType = OperationType.GetDefaultOperationTypeFor(type);
        }

        protected override void OnInitialize() {
            World.Services.Get<TweakSystem>().Tweak(_stat, this, _type);
            
            if (ParentModel?.ParentModel is Status) {
                var timeDependent = ParentModel.Owner.GetOrCreateTimeDependent();
                timeDependent.WithUpdate(Update);
                RefreshPrediction();
            }
        }

        public float TweakFn(float original, Tweak _) => OperationType.Calculate(original, _value);

        public void RestoreStatWhenResting(int gameTimeInMinutes) {
            if (ParentModel?.ParentModel is Status status) {
                float realTimeChangeInSeconds = gameTimeInMinutes * 60f / World.Only<GameRealTime>().WeatherSecondsPerRealSecond;
                var timeLeftSeconds = status.TimeLeftSeconds;
                bool hasDuration = timeLeftSeconds != null;
                float timeWhenPassiveIsAppliedDuringRest = hasDuration ? Mathf.Min(timeLeftSeconds.Value, realTimeChangeInSeconds) : realTimeChangeInSeconds;
                
                if (timeWhenPassiveIsAppliedDuringRest > 0) {
                    if (_stat == Hero.Current.HealthRegen) {
                        Hero.Current.Health.IncreaseBy(_value * timeWhenPassiveIsAppliedDuringRest);
                    } else if (_stat == Hero.Current.CharacterStats.ManaRegen) {
                        Hero.Current.Mana.IncreaseBy(_value * timeWhenPassiveIsAppliedDuringRest);
                    } else if (_stat == Hero.Current.CharacterStats.ManaRegenPercentage) {
                        Hero.Current.Mana.IncreaseBy((_value / 100f) * Hero.Current.MaxMana.ModifiedValue * timeWhenPassiveIsAppliedDuringRest);
                    } else if (_stat == Hero.Current.StaminaRegen) {
                        Hero.Current.Stamina.IncreaseBy(_value * timeWhenPassiveIsAppliedDuringRest);
                    }
                }
            }
        }
        
        void Update(float deltaTime) {
            RefreshPrediction();
        }
        
        void RefreshPrediction() {
            if (ParentModel?.ParentModel is Status status) {
                float? duration = status.TimeLeftSeconds;
                if (duration.HasValue) {
                    float toRegen = _value * duration.Value;
                    _stat.SetPrediction(this, toRegen);
                }
            }
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            ParentModel.Owner?.GetTimeDependent()?.WithoutUpdate(Update);
        }
    }
}