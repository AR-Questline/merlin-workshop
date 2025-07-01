using Awaken.TG.Graphics.Animations;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Animations.FSM.Heroes.Modifiers;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Shared;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Animations;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments.Audio;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Animations.HitStops;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.Main.Utility.VFX;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using FMODUnity;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.Base {
    public abstract partial class MeleeAttackAnimatorState : HeroAnimatorState<MeleeFSM>, IStateWithModifierAttackSpeed {
        [UnityEngine.Scripting.Preserve] static AnimationCurve s_defaultCurve = AnimationCurve.Linear(0, 1, 1, 1);
        
        public override bool CanPerformNewAction => ParentModel.CanPerformAction && CanPerform;
        public abstract bool IsUsingMainHand { get; }
        public float AttackSpeed => ParentModel.GetAttackSpeed(IsHeavy);
        protected abstract bool IsHeavy { get; }
        protected abstract bool CanPerform { get; }
        protected abstract HitStopData HitStopData { get; }
        protected HitStopsAsset HitStopsAsset => ParentModel.StatsItem.View<CharacterWeapon>()?.HitStopsAssetOverride ?? Hero.Data.hitStopsAsset;
        
        IEventListener _hitsListener, _environmentHitListener;
        int _hits;

        protected override bool BeforeEnter(out HeroStateType desiredState) {
            ParentModel.TryGetElement<MeleeHitStop>()?.Discard();
            return base.BeforeEnter(out desiredState);
        }

        protected override void AfterEnter(float previousStateNormalizedTime) {
            AttackBegun();
            PreventStaminaRegen();
            OnAfterEnter(previousStateNormalizedTime);
            
            Hero.Current.Trigger(GamepadEffects.Events.TriggerVibrations, new TriggersVibrationData{effects = GameConstants.Get.meleeAttackEnterXboxVibrations, handsAffected = GetHandForMeleeVibrations});
            
            _hitsListener = Hero.ListenTo(ICharacter.Events.HitAliveWithMeleeWeapon, OnAliveHit, this);
            _environmentHitListener = Hero.ListenTo(ICharacter.Events.HitEnvironment, OnEnvironmentHit, this);
        }

        protected override void OnExit(bool restarted) {
            AttackEnded();
            DisableStaminaRegenPrevent();
            OnAfterExit();
            RemoveHitsListener();
        }

        protected virtual void OnAfterEnter(float previousStateNormalizedTime) { }
        protected virtual void OnAfterExit() { }

        protected override void OnUpdate(float deltaTime) {
            if (CurrentState != null) {
                CurrentState.Speed = AttackSpeed;
            }
            base.OnUpdate(deltaTime);
        }

        // === Enabling & Disabling animation curves
        void AttackBegun() {
            _hits = 0;
            
            VHeroController heroController = Hero.VHeroController;
            if (heroController != null) {
                CustomEvent.Trigger(heroController.gameObject, "HeroAttacked");
            }
            
            WeaponRestriction restriction = ParentModel switch {
                DualHandedFSM fsm => fsm.Restriction,
                MagicMeleeOffHandFSM => WeaponRestriction.OffHand,
                _ => WeaponRestriction.MainHand
            };
            
            AnimatorUtils.StartProcessingAnimationSpeed(ParentModel.HeroAnimancer, ParentModel.AnimancerLayer, ParentModel.LayerType, StateToEnter, IsHeavy, restriction);
            
            Hero.Trigger(Hero.Events.HeroAttacked, true);
        }

        void AttackEnded() {
            Hero.Trigger(Hero.Events.StopProcessingAnimationSpeed, true);
        }
        
        // === HitStops
        void OnAliveHit(bool isHeavyAttack) {
            Hero.Current.Trigger(GamepadEffects.Events.TriggerVibrations, new TriggersVibrationData {effects = GameConstants.Get.meleeNpcHitXboxVibrations, handsAffected = GetHandForMeleeVibrations});
            
            int? hitsRequiredToHitStop = ParentModel.StatsItem.HitsToHitStop.HitsRequired;
            if (!isHeavyAttack && !hitsRequiredToHitStop.HasValue) {
                return;
            }
            
            _hits++;
            if (isHeavyAttack || _hits >= hitsRequiredToHitStop.Value) {
                ParentModel.HitStop(HitStopData);
            }
        }

        void OnEnvironmentHit(EnvironmentHitData hitData) {
            ParentModel.HitStop(Hero.Data.environmentHitStopsData);
            
            // --- Add Force to Rigidbody
            if (hitData.Rigidbody != null) {
                hitData.Rigidbody.AddForce(hitData.Direction * hitData.RagdollForce, ForceMode.Impulse);
            }
            // --- VFX
            Item item = hitData.Item;
            NpcDummy npcDummy = hitData.Location != null ? hitData.Location.Target?.TryGetElement<NpcDummy>() : null;
            SurfaceType surfaceType = npcDummy != null ? npcDummy.Template.SurfaceType : SurfaceType.HitStone;
            VFXManager.SpawnCombatVFX(item.DamageSurfaceType, surfaceType, hitData.Position, hitData.Direction, null, null);
            // --- Audio
            ItemAudio itemAudio = item.TryGetElement<ItemAudio>();
            EventReference eventReference = ItemAudioType.MeleeHit.RetrieveFrom(item);
            if (itemAudio?.AudioContainer is { audioType: ItemAudioContainer.AudioType.Magic }) {
                eventReference = ItemAudioType.MagicHit.RetrieveFrom(item);
            }
            SurfaceType audioSurfaceType = npcDummy != null ? npcDummy.Template.SurfaceType : SurfaceType.HitGround;
            
            FMODParameter[] parameters = { audioSurfaceType, new("Heavy", IsHeavy) };
            item.PlayAudioClip(eventReference, true, parameters);
            Hero.Current.Trigger(GamepadEffects.Events.TriggerVibrations, new TriggersVibrationData {effects = GameConstants.Get.meleeEnviroHitFirstXboxVibrations, handsAffected = GetHandForMeleeVibrations});
        }

        void RemoveHitsListener() {
            if (_hitsListener != null) {
                World.EventSystem.RemoveListener(_hitsListener);
                _hitsListener = null;
            }

            if (_environmentHitListener != null) {
                World.EventSystem.RemoveListener(_environmentHitListener);
                _environmentHitListener = null;
            }
        }
    }
}