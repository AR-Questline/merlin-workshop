using System;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Magic;
using Awaken.TG.Main.Animations.FSM.Heroes.Utils;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.Utility.Collections;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.Machines {
    public abstract partial class MagicFSM : HeroAnimatorSubstateMachine {
        protected const float MagicEndBlendDuration = 0.05f;
        const float CastingCancelDuration = 2.5f;
        const float CastFailCooldown = 0.75f;

        Item _item;
        IEventListener _cancelCastingListener;
        IEventListener _performMidCastListener;
        IEventListener _overrideEndCastingIndexListener;
        IEventListener _endCastingListener;
        float _castInactiveDuration;
        float _lastFailedCastInform;
        
        public abstract CastingHand CastingHand { get; }
        public bool IsChargingMagic {
            get {
                if (CurrentAnimatorState == null || !IsLayerActive) {
                    return false;
                }

                bool isCharging = CurrentAnimatorState.GeneralType == HeroGeneralStateType.MagicCastHeavy;
                if (CurrentAnimatorState is MagicHeavyEnd heavyEnd) {
                    isCharging = isCharging && !heavyEnd.Performed;
                }
                return isCharging;
            }
        }

        public MagicEndState MagicEndState { get; private set; } = MagicEndState.MagicEnd;

        public bool WasCanceledWhenInMagicLoop { get; private set; }
        public bool SpellAttackHeld => CastingHand == CastingHand.OffHand ? BlockDown || BlockHeld : _attackDown || _attackHeld;

        public Item Item {
            get {
                if (_item is { HasBeenDiscarded: false }) {
                    return _item;
                }

                var equipmentType = CastingHand switch {
                    CastingHand.MainHand => EquipmentSlotType.MainHand,
                    CastingHand.OffHand => EquipmentSlotType.OffHand,
                    CastingHand.BothHands => EquipmentSlotType.MainHand,
                    _ => throw new ArgumentOutOfRangeException(nameof(CastingHand), CastingHand, null),
                };
                return _item = ParentModel.HeroItems.EquippedItem(equipmentType);
            }
        }
        
        public Skill Skill => Item?.CastAbleSkill;
        public bool CanBeCharged => !Item.HasElement<DisableSkillChargeMarker>() && Item.TryGetElement(out ItemEffects itemEffects) && itemEffects.CanBeCharged;
        public int CurrentChargeSteps { get; set; }
        public int MaxChargeSteps => Item.TryGetElement(out ItemEffects itemEffects) ? itemEffects.MaxChargeSteps : 1;
        public bool IsCasting => CurrentAnimatorState?.GeneralType is HeroGeneralStateType.MagicCastLight or HeroGeneralStateType.MagicCastHeavy;

        public override bool UseAlternateState => Item?.TryGetElement<IItemWithCharges>()?.ChargesRemaining <= 0;
        public override bool PreventHidingWeapon => ParentModel.Elements<MagicFSM>().Any(m => m.IsCasting);
        protected float Cost => Skill.Cost.CombinedStatCost(CharacterStatType.Mana);
        protected bool IsSkillAvailable => (Skill?.CanSubmit ?? false) && _castInactiveDuration <= 0;
        protected bool IsMuted => ParentModel.HasElement<MutedMarker>();
        
        // === Events
        public new static class Events {
            public static readonly Event<Item, bool> BeforeLightCastStarted = new(nameof(BeforeLightCastStarted));
            public static readonly Event<Item, bool> BeforeHeavyCastStarted = new(nameof(BeforeHeavyCastStarted));
            public static readonly Event<Item, bool> CancelCasting = new(nameof(CancelCasting));
            public static readonly Event<Item, bool> PerformMidCast = new(nameof(PerformMidCast));
            public static readonly Event<Item, MagicEndState> OverrideEndCastingIndex = new(nameof(OverrideEndCastingIndex));
            public static readonly Event<Item, MagicEndState> EndCasting = new(nameof(EndCasting));
        }

        // === Constructor
        protected MagicFSM(Animator animator, ARHeroAnimancer animancer) : base(animator, animancer) { }
        
        // === Lifecycle
        protected override void OnEnable() {
            _item = null;
            _cancelCastingListener = Item.ListenTo(Events.CancelCasting, CancelCasting, this);
            _performMidCastListener = Item.ListenTo(Events.PerformMidCast, PerformMidCast, this);
            _overrideEndCastingIndexListener = Item.ListenTo(Events.OverrideEndCastingIndex, SetMagicEndIndex, this);
            _endCastingListener = Item.ListenTo(Events.EndCasting, EndCasting, this);
        }
        
        protected override void AfterEnable() {
            HeadLayerIndex?.SetEnable(_cameraShakesEnabled, CameraShakesIntensity);
        }
        
        protected override void OnDisable(bool fromDiscard) {
            HeadLayerIndex?.SetEnable(false);

            ParentModel.VHeroController?.CastingCanceled(CastingHand, _item, IsCasting);
            EndSlowModifier();
            
            World.EventSystem.TryDisposeListener(ref _endCastingListener);
            World.EventSystem.TryDisposeListener(ref _performMidCastListener);
            World.EventSystem.TryDisposeListener(ref _overrideEndCastingIndexListener);
            World.EventSystem.TryDisposeListener(ref _cancelCastingListener);
            MagicEndState = 0;
        }
        
        protected override void OnUpdate(float deltaTime) {
            if (_castInactiveDuration > 0) {
                _castInactiveDuration -= deltaTime;
            }
            OnMagicFSMUpdate(deltaTime);
        }
        protected abstract void OnMagicFSMUpdate(float deltaTime);

        protected override void OnUIStateChanged(UIState state) {
            base.OnUIStateChanged(state);
            if (state.IsMapInteractive || !state.PauseTime) {
                return;
            }

            if (IsCasting) {
                CancelCasting();
            } else if (CurrentAnimatorState?.GeneralType == HeroGeneralStateType.Block) {
                ParentModel.TryGetElement<HeroBlock>()?.Discard();
            }

            SetCurrentState(HeroStateType.Idle);
            EndSlowModifier();
        }

        protected override void OnHideWeapons(bool instant) {
            if (!IsLayerActive) {
                return;
            }

            if (PreventHidingWeapon) {
                CancelCasting();
            } else {
                SetCurrentState(HeroStateType.UnEquipWeapon, instant ? 0 : null);
            }

            EndSlowModifier();
        }

        public virtual void CancelCasting() {
            ResetProlong();
            ParentModel.VHeroController.CastingCanceled(CastingHand);

            WasCanceledWhenInMagicLoop = CurrentAnimatorState?.Type == HeroStateType.MagicHeavyLoop;
            SetCurrentState(CurrentAnimatorState?.GeneralType == HeroGeneralStateType.MagicCastHeavy
                ? HeroStateType.MagicCancelCast
                : HeroStateType.Idle);

            if (Item == null) {
                return;
            }

            bool canBeRefunded = WasCanceledWhenInMagicLoop || CurrentAnimatorState is IMagicLightCast { CanBeRefunded: true };
            if (canBeRefunded) {
                Item.ActiveSkills.ForEach(skill => skill.Refund());
            }
            PlayAudioClip(ItemAudioType.CastCancel.RetrieveFrom(Item));
        }

        void PerformMidCast() {
            if (CurrentAnimatorState?.Type == HeroStateType.MagicHeavyLoop) {
                SetCurrentState(HeroStateType.MagicPerformMidCast, MagicEndBlendDuration);
            }
        }

        void EndCasting(MagicEndState state) {
            SetMagicEndIndex(state);
            if (CurrentAnimatorState?.Type == HeroStateType.MagicHeavyLoop) {
                SetCurrentState(HeroStateType.MagicHeavyEnd, MagicEndBlendDuration);
                _castInactiveDuration = CastingCancelDuration;
            }
        }

        public void SetMagicEndIndex(MagicEndState state) {
            MagicEndState = state;
        }

        public void ResetMagicEndIndex() {
            MagicEndState = MagicEndState.MagicEnd;
        }

        public void PlayAudioClip(EventReference eventReference, params FMODParameter[] eventParams) {
            Item?.PlayAudioClip(eventReference, false, eventParams);
        }

        // === Public API
        public void BeginSlowModifier() {
            ParentModel.Element<AnimatorSharedData>().BeginMagicSlowModifier();
        }

        public void EndSlowModifier() {
            ParentModel.Element<AnimatorSharedData>().EndMagicSlowModifier();
        }
        
        public virtual void ResetProlong() {
            ResetAttackProlong();
        }
        
        // === Helpers
        public void OnPerformCast() {
            ParentModel.VHeroController?.CastingEnded(CastingHand);
            Hero.Current.Trigger(GamepadEffects.Events.TriggerVibrations, new TriggersVibrationData {effects = GameConstants.Get.magicPerformXboxVibrations, handsAffected = CastingHand});
            EndSlowModifier();
        }
        
        protected bool TryEnterMagicCastState(HeroStateType stateToEnter, bool lightCast) {
            Item.Trigger(lightCast ? Events.BeforeLightCastStarted : Events.BeforeHeavyCastStarted, true);

            if (IsSkillAvailable && !IsMuted) {
                SetCurrentState(stateToEnter);
                return true;
            }

            InformCastUseFail();
            return false;
        }
        
        void InformCastUseFail() {
            if (CurrentStateType == HeroStateType.MagicFailedCast) {
                return;
            }

            if (_lastFailedCastInform + CastFailCooldown > Time.time) {
                return;
            }

            _lastFailedCastInform = Time.time;
            SetCurrentState(HeroStateType.MagicFailedCast);
            ParentModel.Trigger(ICharacter.Events.CastingFailed, new CastSpellData { CastingHand = CastingHand, Item = Item });

            if (Skill.HasCost && !Skill.Cost.CanAfford()) {
                ParentModel.Trigger(Hero.Events.StatUseFail, CharacterStatType.Mana);
                ParentModel.Trigger(Hero.Events.NotEnoughMana, Cost);
            } else {
                FMODManager.PlayOneShot(ItemAudioType.FailedCast.RetrieveFrom(Item));
            }

            CharacterMagic characterMagic = _item?.View<CharacterMagic>();
            if (characterMagic != null) {
                characterMagic.OnFailedCast();
            }
        }
    }
}