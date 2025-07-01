using System.Threading;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.Main.Utility.Animations.HitStops;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Enums;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.Modifiers {
    public partial class MeleeHitStop : Element<MeleeFSM> {
        public sealed override bool IsNotSaved => true;

        // === Fields
        CancellationTokenSource _hitStopToken;
        bool _isExiting;
        
        // === Properties
        public bool CanPerformAction { get; private set; }
        Hero Hero => ParentModel.ParentModel;
        ARHeroAnimancer Animancer => ParentModel.HeroAnimancer;
        // --- HitStop Data
        AnimationCurve HitStopCurve => HitStopData.HitStopCurve;
        float DelayBeforeEnterHitStop => HitStopData.DelayBeforeEnterHitStop;
        float HitStopDuration => HitStopData.HitStopDuration;
        float HitStopBlendToIdleDuration => HitStopData.HitStopBlendToIdleDuration;
        CameraShakeType CameraShakeType => HitStopData.CameraShakeType;
        HitStopData HitStopData { get; }

        // === Events
        public new static class Events {
            public static readonly Event<MeleeFSM, MeleeHitStop> MeleeHitStopStarted = new(nameof(MeleeHitStopStarted));
        }
        // === Constructor
        public MeleeHitStop(HitStopData hitStopData) {
            HitStopData = hitStopData;
        }

        // === Initialization
        protected override void OnInitialize() {
            HitStop().Forget();
            ParentModel.Trigger(Events.MeleeHitStopStarted, this);
            Hero.Trigger(CameraShakesFSM.Events.ShakeHeroCamera, CameraShakeType);
        }
        
        // === Public API
        public void ExitHitStop(bool instant) {
            if (instant) {
                RestoreAttackSpeeds();
                Discard();
                return;
            }
            
            ExitHitStopInternal().Forget();
        }

        // === HitStop
        async UniTaskVoid HitStop() {
            _hitStopToken = new CancellationTokenSource();
            // --- HitStop can trigger so early that it will automatically trigger next attack from the same input, that's why we reset it.
            ParentModel.ResetAttackProlong();
            // --- Prevent player to perform next action when in HitStop.
            CanPerformAction = false;
            // --- Trigger AttackEnd events to disable weapon colliders
            Hero.Trigger(Hero.Events.StopProcessingAnimationSpeed, true);

            // --- Attack speed fastens animation, so all delays and durations needs to be shortened.
            float attackSpeedModifier = Hero.CharacterStats.AttackSpeed;
            float durationModifier = 1 / attackSpeedModifier;
            
            // --- Initial delay so that weapon moves a little bit further (strictly for better visual effect)
            float delayBeforeEnterHitStop = DelayBeforeEnterHitStop * durationModifier;
            bool success = await AsyncUtil.DelayTime(this, delayBeforeEnterHitStop, source: _hitStopToken);
            if (!success) {
                ExitHitStopInternal().Forget();
                return;
            }

            // --- Reverse attack animation to simulate "hit"
            float duration = 0;
            float hitStopDuration = HitStopDuration * durationModifier;

            do {
                SetAttackSpeeds(HitStopCurve.Evaluate(duration) * attackSpeedModifier);
                duration += Hero.GetDeltaTime();
                success = await AsyncUtil.DelayFrame(this, cancellationToken: _hitStopToken.Token);
            } while (duration <= hitStopDuration && success && Animancer != null);

            if (!success) {
                ExitHitStopInternal().Forget();
                return;
            }

            // --- Allow player to perform next action
            CanPerformAction = true;
            
            // --- Prolong hitStop for additional flat value to prevent too high attack speed bugs.
            hitStopDuration += GameConstants.Get.additionalFlatHitStopDurationThatAllowsNextAttack;
            do {
                SetAttackSpeeds(HitStopCurve.Evaluate(duration) * attackSpeedModifier);
                duration += Hero.GetDeltaTime();
                success = await AsyncUtil.DelayFrame(this, cancellationToken: _hitStopToken.Token);
            } while (duration <= hitStopDuration && success && Animancer != null);

            if (!success) {
                ExitHitStopInternal().Forget();
                return;
            }
            
            // --- Blend back to idle
            float hitStopBlendToIdleDuration = HitStopBlendToIdleDuration * durationModifier;
            
            ParentModel.SetCurrentState(HeroStateType.Idle, hitStopBlendToIdleDuration);
            do {
                SetAttackSpeeds(HitStopCurve.Evaluate(duration) * attackSpeedModifier);
                duration += Hero.GetDeltaTime();
                success = await AsyncUtil.DelayFrame(this, cancellationToken: _hitStopToken.Token);
            } while (duration <= hitStopDuration + hitStopBlendToIdleDuration && success);

            ExitHitStopInternal(false).Forget();
        }
        
        async UniTaskVoid ExitHitStopInternal(bool enterTPose = true) {
            if (_isExiting) return;
            _isExiting = true;
            
            _hitStopToken?.Cancel();
            _hitStopToken = null;
            
            RestoreAttackSpeeds();

            if (HasBeenDiscarded) return;

            CancellationTokenSource tPoseCancellationToken = new();
            if (enterTPose && !Hero.TppActive) {
                EnterTPoseOverride(tPoseCancellationToken).Forget();
            }

            float entryTime = Time.time;
            if (await AsyncUtil.WaitWhile(this, () => ParentModel.AnimancerLayer.IsInTransition() && Time.time < entryTime + 0.01f)) {
                tPoseCancellationToken.Cancel();
                Discard();
            }
        }

        async UniTaskVoid EnterTPoseOverride(CancellationTokenSource cancellationToken) {
            HeroOverridesFSM heroOverridesFSM = Hero.Element<HeroOverridesFSM>();
            heroOverridesFSM.SetCurrentState(HeroStateType.TPose, 0f);
            if (!await AsyncUtil.UntilCancelled(heroOverridesFSM, cancellationToken)) {
                return;
            }
            heroOverridesFSM.SetCurrentState(HeroStateType.None, 0f);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            ExitHitStopInternal().Forget();
            base.OnDiscard(fromDomainDrop);
        }

        // === Helpers
        void SetAttackSpeeds(float value) {
            foreach (var attackSpeed in RichEnum.AllValuesOfType<AnimancerAttackSpeed>()) {
                attackSpeed.SetAttackSpeed(Animancer, value);
            }
        }

        void RestoreAttackSpeeds() {
            if (ParentModel == null || ParentModel.HasBeenDiscarded || Hero.HasBeenDiscarded) {
                return;
            }
            CharacterStats stats = Hero.CharacterStats;
            AnimancerAttackSpeed.HeavyAttackMult1H.SetAttackSpeed(Animancer, stats.OneHandedHeavyAttackSpeed);
            AnimancerAttackSpeed.LightAttackMult1H.SetAttackSpeed(Animancer, stats.OneHandedLightAttackSpeed);
            
            AnimancerAttackSpeed.HeavyAttackMult2H.SetAttackSpeed(Animancer, stats.TwoHandedHeavyAttackSpeed);
            AnimancerAttackSpeed.LightAttackMult2H.SetAttackSpeed(Animancer, stats.TwoHandedLightAttackSpeed);
            
            AnimancerAttackSpeed.HeavyAttackMult1H.SetAttackSpeed(Animancer, stats.OneHandedHeavyAttackSpeed);
            AnimancerAttackSpeed.HeavyAttackMult1H.SetAttackSpeed(Animancer, stats.OneHandedHeavyAttackSpeed);
        }
    }
}