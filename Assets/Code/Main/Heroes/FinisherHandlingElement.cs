using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Finishers;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Heroes {
    public partial class FinisherHandlingElement : Element<Hero> {
        const float MaxDistanceSqr = 3.5f * 3.5f;
        
        public sealed override bool IsNotSaved => true;
        
        NpcElement _npcPointingTowards;
        FinisherExecutionAction _executionAction;
        
        protected override void OnInitialize() {
            ParentModel.ListenTo(VCHeroRaycaster.Events.PointsTowardsIWithHealthBar, OnPointingTowardsLocation, this);
            ParentModel.ListenTo(VCHeroRaycaster.Events.StoppedPointingTowardsLocation, OnPointingTowardsLocationStopped, this);
        }

        void OnPointingTowardsLocation(Location location) {
            location.TryGetElement(out _npcPointingTowards);
            TryAddFinisherAction();
        }

        void OnPointingTowardsLocationStopped() {
            _npcPointingTowards = null;
        }

        public bool TryTriggerFinisherBeforeAttack() {
            if (TryFindFinisher(FinisherTrigger.AttackTriesToStart, out var finisherData, out var damageOutcome, out _, out _)) {
                finisherData.PlayAnimations(damageOutcome, _npcPointingTowards, ParentModel);
                return true;
            }
            
            return false;
        }

        void TryAddFinisherAction() {
            if (_npcPointingTowards == null) {
                return;
            }
            
            if (_executionAction != null) {
                if (_executionAction.ParentModel == _npcPointingTowards.ParentModel) {
                    return;
                }
                _executionAction.Discard();
            }
            
            _executionAction = new FinisherExecutionAction(this);
            _npcPointingTowards.ParentModel.AddElement(_executionAction);
        }

        internal bool TryFindFinisher(FinisherTrigger trigger, out FinisherData finisher, out DamageOutcome damageOutcome, out FinishersList usedFinisherList, out float usedDmg) {
            bool finisherBlocked = _npcPointingTowards is not { HasBeenDiscarded: false, IsAlive: true, CanUseExternalCustomDeath: true };
            finisherBlocked = finisherBlocked || KillPreventionDispatcher.HasActivePrevention(_npcPointingTowards);
            finisherBlocked = finisherBlocked || (_npcPointingTowards.Coords - ParentModel.Coords).sqrMagnitude > MaxDistanceSqr;
            
            if (finisherBlocked) {
                usedFinisherList = null;
                finisher = null;
                damageOutcome = default;
                usedDmg = 0f;
                return false;
            }

            var mainHandItem = ParentModel.MainHandItem;
            if (mainHandItem is not { IsMelee: true }) {
                mainHandItem = null;
            }
            var offHandItem = ParentModel.OffHandItem;
            if (offHandItem is not { IsMelee: true }) {
                offHandItem = null;
            }
            
            if (mainHandItem == null && offHandItem == null) {
                usedFinisherList = null;
                finisher = null;
                damageOutcome = default;
                usedDmg = 0f;
                return false;
            }
            
            var dmgParams = DamageParameters.Default;
            dmgParams.Direction = ParentModel.Coords - _npcPointingTowards.Coords;
            Damage mainHandDmg = mainHandItem != null ? Damage.CalculateDamageDealt(ParentModel, _npcPointingTowards, dmgParams, mainHandItem) : null;
            Damage offHandDmg = offHandItem != null ? Damage.CalculateDamageDealt(ParentModel, _npcPointingTowards, dmgParams, offHandItem) : null;
            
            if (mainHandDmg != null && offHandDmg != null) {
                usedDmg = mainHandDmg.Amount > offHandDmg.Amount ? mainHandDmg.Amount : offHandDmg.Amount;
            } else if (mainHandDmg != null) {
                usedDmg = mainHandDmg.Amount;
            } else if (offHandDmg != null) {
                usedDmg = offHandDmg.Amount;
            } else {
                usedFinisherList = null;
                finisher = null;
                damageOutcome = default;
                usedDmg = 0f;
                return false;
            }
            
            float npcHp = _npcPointingTowards.Health.ModifiedValue;
            if (ParentModel.MainHandWeapon is CharacterWeapon mainHandWeapon) {
                damageOutcome = FakeDmgOutcome(mainHandDmg, npcHp);
                if (TryFindFinisher(mainHandWeapon, trigger, damageOutcome, usedDmg, out finisher, out usedFinisherList)) {
                    return true;
                }
            }
            if (ParentModel.OffHandWeapon is CharacterWeapon offHandWeapon) {
                damageOutcome = FakeDmgOutcome(offHandDmg, npcHp);
                if (TryFindFinisher(offHandWeapon, trigger, damageOutcome, usedDmg, out finisher, out usedFinisherList)) {
                    return true;
                }
            }

            usedFinisherList = null;
            finisher = null;
            damageOutcome = default;
            usedDmg = 0f;
            return false;
        }

        bool TryFindFinisher(CharacterWeapon weapon, FinisherTrigger trigger, DamageOutcome damageOutcome, float usedDmg, out FinisherData finisher, out FinishersList usedFinisherList) {
            usedFinisherList = trigger == FinisherTrigger.AttackTriesToStart ? weapon.FinishersList : weapon.ExecutionsList;
            finisher = null;
            switch (trigger) {
                case FinisherTrigger.AttackTriesToStart when usedFinisherList?.TryFindFirstValidFinisher(damageOutcome, usedDmg, out finisher) ?? false:
                case FinisherTrigger.Interaction when usedFinisherList?.TryFindRandomValidFinisher(damageOutcome, usedDmg, out finisher) ?? false:
                    return true;
                default:
                    return false;
            }
        }

        public DamageOutcome FakeDmgOutcome(Damage damage, float enemyHp) {
            return new DamageOutcome(damage, ParentModel.Coords, new DamageModifiersInfo(0, 0, 0, 0, true), enemyHp);
        }
        
        public enum FinisherTrigger {
            AttackTriesToStart,
            Interaction,
        }
    }
    
    internal partial class FinisherExecutionAction : AbstractHeroAction<Location> {
        const float ActivationDelay = 0.6f;
        
        public sealed override bool IsNotSaved => true;
        
        readonly FinisherHandlingElement _finisherHandlingElement;
        FinishersList _cachedFinisherList;
        FinisherData _cachedData;
        DamageOutcome _cachedDamageOutcome;
        float _cachedDmg;
        float? _activationTime;

        public override string DefaultActionName => LocTerms.KillUnconscious.Translate();
        bool IsActiveTime => ActivationTime < Time.time;
        float ActivationTime => _activationTime ??= Time.time + ActivationDelay;
        
        internal FinisherExecutionAction(FinisherHandlingElement finisherHandlingElement) {
            _finisherHandlingElement = finisherHandlingElement;
        }
        
        public override ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            return CanBeTriggered()
                ? ActionAvailability.Available
                : ActionAvailability.Disabled;
        }

        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            _cachedData.PlayAnimations(_cachedDamageOutcome, ParentModel.Element<NpcElement>(), hero);
        }

        bool CanBeTriggered() {
            if (ParentModel.TryGetElement<NpcElement>() is not { IsAlive: true }) {
                return false;
            }
            if (IsCachedDataValid() || _finisherHandlingElement.TryFindFinisher(FinisherHandlingElement.FinisherTrigger.Interaction,
                    out _cachedData, out _cachedDamageOutcome, out _cachedFinisherList, out _cachedDmg)) {
                return IsActiveTime;
            }
            
            _activationTime = null;
            return false;
        }

        bool IsCachedDataValid() {
            if (_cachedData == null) {
                return false;
            }

            if (_cachedDamageOutcome.Damage.Item is not { IsEquipped: true }) {
                ClearCaches();
                return false;
            }

            if (!_cachedData.CheckConditions(_cachedDamageOutcome, _cachedDmg, _cachedFinisherList.CheckDefaultHpCondition(_cachedDamageOutcome, _cachedDmg), true)) {
                ClearCaches();
                return false;
            }
            
            if (!_cachedFinisherList.CheckGlobalConditions(_cachedDamageOutcome)) {
                ClearCaches();
                return false;
            }

            return true;
        }
        
        void ClearCaches() {
            _cachedFinisherList = null;
            _cachedData = null;
            _cachedDamageOutcome = default;
            _cachedDmg = 0;
        }
    }
}
