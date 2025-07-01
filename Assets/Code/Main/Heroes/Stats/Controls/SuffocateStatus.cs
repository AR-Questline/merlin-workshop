using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Stats.Controls {
    public partial class SuffocateStatus : Element<Hero> {
        public override ushort TypeForSerialization => SavedModels.SuffocateStatus;

        [Saved] float _tickDelay;
        [Saved] float _tickPercentageDamage;
        [Saved] float _currentTick;

        DamageParameters _suffocateDamageParameters;
        HealthElement HealthElement => ParentModel.HealthElement;

        // === Static Constructor
        public static void AddToHero(Hero hero) {
            var status = new SuffocateStatus() {
                _tickPercentageDamage = hero.Template.heroControllerData.suffocatePercentageDamage,
                _tickDelay = hero.Template.heroControllerData.suffocateTick
            };
            hero.AddElement(status);
        }
        
        // === Constructors
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        SuffocateStatus() {}

        
        // === Initialization
        protected override void OnInitialize() {
            this.GetOrCreateTimeDependent().WithUpdate(OnUpdate);
            ParentModel.Element<HeroOxygenLevel>().ListenTo(HeroOxygenLevel.Events.OxygenLevelChanged, OnOxygenChanging, this);
            SetupDamage();
        }

        void SetupDamage() {
            _suffocateDamageParameters = new DamageParameters() {
                CanBeCritical = true,
                Critical = false,
                IgnoreArmor = true,
                Inevitable = true,
                IsPrimary = false,
                DamageTypeData = new RuntimeDamageTypeData(DamageType.PhysicalHitSource, DamageSubType.Pure),
                IsDamageOverTime = true,
            };
        }

        void OnOxygenChanging(LimitedStat oxygenLevel) {
            if (oxygenLevel.Percentage > 0) {
                Discard();
            }
        }
        
        // === Updating
        void OnUpdate(float deltaTime) {
            _currentTick -= deltaTime;
            if (_currentTick <= 0) {
                _currentTick = _tickDelay;
                var suffocateDamage =
                    new Damage(_suffocateDamageParameters, ParentModel, ParentModel,
                        new RawDamageData(HealthElement.MaxHealth * _tickPercentageDamage)).WithStatusDamageType(StatusDamageType.Breath);
                HealthElement.TakeDamage(suffocateDamage);
            } 
        }
    }
}