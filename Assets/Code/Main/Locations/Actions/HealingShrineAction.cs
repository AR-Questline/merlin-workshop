using Awaken.TG.Assets;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Times;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.VFX;
using World = Awaken.TG.MVC.World;

namespace Awaken.TG.Main.Locations.Actions {
    public partial class HealingShrineAction : AbstractLocationAction, IRefreshedByAttachment<HealingShrineAttachment> {
        public override ushort TypeForSerialization => SavedModels.HealingShrineAction;

        static readonly int ActiveHash = Animator.StringToHash("Active");
        [Saved] ARDateTime _restoreTime;
        HealingShrineAttachment _spec;
        TimedEvent _restoreEvent;
        AnimatorElement _animator;
        VisualEffect _activeVfx;
        
        bool Available => _restoreEvent == null;
        public override string DefaultActionName => LocTerms.Heal.Translate();

        public void InitFromAttachment(HealingShrineAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnInitialize() {
            SetMaterialProperty(true);
        }
        
        protected override void OnRestore() {
            if (!Available) {
                if (_restoreTime > World.Any<GameRealTime>().WeatherTime) {
                    _restoreEvent = new TimedEvent(_restoreTime.Date, RestoreShrine);
                    World.Any<GameTimeEvents>().AddEvent(_restoreEvent);
                    DisableShrineVisualUpdate();
                } else {
                    RestoreShrine();
                    SetMaterialProperty(true);
                }
            }
            base.OnRestore();
        }

        protected override void OnFullyInitialized() {
            _animator = ParentModel.TryGetElement<AnimatorElement>();
        }

        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            if (!Available || hero == null) {
                return;
            }
            UseShrine(hero);
        }

        public override ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            return Available ? base.GetAvailability(hero, interactable) : ActionAvailability.Disabled;
        }
        
        ARDateTime GetNewRestoreTime() {
            var currentTime = World.Any<GameRealTime>().WeatherTime;
            _restoreTime = currentTime + _spec.RestoreTime;
            return _restoreTime;
        }

        void UseShrine(Hero hero) {
            _restoreEvent = new TimedEvent(GetNewRestoreTime().Date, RestoreShrine);
            World.Any<GameTimeEvents>().AddEvent(_restoreEvent);
            var contract = new ContractContext(this, hero, ChangeReason.Exploration);
            if (_spec.HealHealth) {
                ((LimitedStat) hero.Stat(AliveStatType.Health)).SetToFull(contract);
            }
            if (_spec.HealMana) {
                ((LimitedStat) hero.Stat(CharacterStatType.Mana)).SetToFull(contract);
            }
            if (_spec.HealStamina) {
                ((LimitedStat) hero.Stat(CharacterStatType.Stamina)).SetToFull(contract);
            }
            if (_spec.HealWyrdSkill) {
                ((LimitedStat) hero.Stat(HeroStatType.WyrdSkillDuration)).SetToFull(contract);
            }
           
            UseShrineVisualUpdate();
        }
        
        void RestoreShrine() {
            _restoreEvent = null;
            RestoreShrineVisualUpdate();
        }

        void UseShrineVisualUpdate() {
            if (_spec.UseEffectVFX is {IsSet: true} vfx) {
                Transform transform = _spec.transform;
                PrefabPool.InstantiateAndReturn(vfx, transform.position, transform.rotation).Forget();
            }
            DisableShrineVisualUpdate();
        }

        void DisableShrineVisualUpdate() {
            SetAnimatorParameter(false);
            SetVisualEffect(false);
            SetMaterialProperty(false);
        }

        void RestoreShrineVisualUpdate() {
            SetAnimatorParameter(true);
            SetVisualEffect(true);
            SetMaterialProperty(true);
        }

        void SetMaterialProperty(bool active) {
            if (active) {
                _spec.AnimatedPropertiesOverrideController.StartForward();
            } else {
                _spec.AnimatedPropertiesOverrideController.StartBackward();
            }
        }

        void SetVisualEffect(bool active) {
            _activeVfx ??= _spec.GetComponentInChildren<VisualEffect>();
            if (_activeVfx != null) {
                if (active) {
                    _activeVfx.Play();
                } else {
                    _activeVfx.Stop();
                }
            }
        }

        void SetAnimatorParameter(bool active) {
            _animator?.SetParameter(ActiveHash, new SavedAnimatorParameter() {type = AnimatorControllerParameterType.Bool, boolValue = active });
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (_restoreEvent != null) {
                World.Any<GameTimeEvents>()?.RemoveEvent(_restoreEvent);
            }
        }
    }
}