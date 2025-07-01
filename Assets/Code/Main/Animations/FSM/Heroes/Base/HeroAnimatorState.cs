using System;
using System.Threading;
using Animancer;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Animations.FSM.Shared;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Controls;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using UnityEngine;
using UniversalProfiling;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Animations.FSM.Heroes.Base {
    public abstract partial class HeroAnimatorState<T> : HeroAnimatorState where T : HeroAnimatorSubstateMachine {
        public new T ParentModel => base.ParentModel as T;
    }
    
    public abstract partial class HeroAnimatorState : ARAnimatorState<Hero, HeroAnimatorSubstateMachine> {
        static readonly UniversalProfilerMarker AfterEnterMarker = new(Color.yellow, $"{nameof(HeroAnimatorState)}.{nameof(AfterEnter)}");

        // === Fields & Properties
        PreventStaminaRegenMarker _preventStaminaRegen;
        CancellationTokenSource _animationLoadToken;

        public override bool IsNotSaved => true;
        public abstract HeroGeneralStateType GeneralType { get; }
        public abstract HeroStateType Type { get; }
        public virtual HeroStateType StateToEnter => Type;
        public override bool CanReEnter => CurrentState is not { IsValid: true };
        public Hero Hero => ParentModel.ParentModel;
        public AnimancerState CurrentState { get; private set; }
        public float TimeElapsedNormalized {
            get {
                if (!Entered) {
                    return 0;
                }
                
                if (CurrentState == null) {
                    return 0;
                }
                
                return CurrentState is { IsValid: true } ? CurrentState.NormalizedTime : 0f;
            }
        }

        public float? HeadLayerWeightOverride => HeadBobbingDependent ? World.Only<HeadBobbingSetting>().Intensity : null;
        public virtual bool UsesActiveLayerMask => false;
        protected virtual bool HeadBobbingDependent => false;
        protected bool UseBlockWithoutShield => ParentModel.UseBlockWithoutShield;
        protected bool UseAlternateState => ParentModel.UseAlternateState;
        protected Animator Animator => ParentModel.Animator;
        protected ARHeroAnimancer HeroAnimancer => ParentModel.HeroAnimancer;
        protected LimitedStat Stamina => ParentModel.Stamina;
        protected HeroStaminaUsedUpEffect StaminaUsedUpEffect => ParentModel.StaminaUsedUpEffect;
        protected CastingHand GetHandForMeleeVibrations => ParentModel is MagicMeleeOffHandFSM ? CastingHand.OffHand : CastingHand.MainHand;
        
        public override void Enter(float previousStateNormalizedTime, float? overrideCrossFadeTime, Action<ITransition> onNodeLoaded = null) {
            if (!BeforeEnter(out var desiredState)) {
                if (desiredState == HeroStateType.Invalid) {
                    Log.Important?.Error($"Tried to enter Invalid State on BeforeEnter to: {Type}!");
                    return;
                }
                ParentModel.SetCurrentState(desiredState, overrideCrossFadeTime, onNodeLoaded);
                return;
            }
            
            Entered = false;
            _animationLoadToken?.Cancel();
            _animationLoadToken = new CancellationTokenSource();
            HeroAnimancer.GetAnimancerNode(ParentModel.LayerType, StateToEnter,
                n => {
                    OnNodeLoaded(previousStateNormalizedTime, n, overrideCrossFadeTime);
                    onNodeLoaded?.Invoke(n);
                },
                OnFailedFindNode, _animationLoadToken.Token);
        }

        public override void Exit(bool restarted = false) {
            Entered = false;
            base.Exit(restarted);
        }

        void OnNodeLoaded(float previousStateNormalizedTime, ITransition node, float? overrideCrossFadeTime) {
            if (ParentModel == null || ParentModel.HasBeenDiscarded || ParentModel.CurrentAnimatorState != this) {
                return;
            }

            float fadeDuration = overrideCrossFadeTime ?? EntryTransitionDuration;
            bool isMixer = node is MixerTransition2D;
            CurrentState = ParentModel.AnimancerLayer.Play(node, fadeDuration, isMixer ? default : FadeMode.FromStart);
            CurrentState.SetNormalizedTimeWithEventsInvoke(OffsetNormalizedTime(previousStateNormalizedTime));
            HeroAnimancer.RebindAnimationRigging();
            AfterEnterMarker.Begin();
            AfterEnter(previousStateNormalizedTime);
            AfterEnterMarker.End();
            
            Entered = true;
        }
        
        protected virtual void OnFailedFindNode() {
            if (!ParentModel.IsLayerActive) {
                return;
            }
            
            if (ParentModel.CurrentAnimatorState != this) {
                return;
            }
            
            Log.Critical?.Error($"Failed to find animator node for state:{StateToEnter} in FSM: {ParentModel}");
        }

        // === Helpers
        protected void PreventStaminaRegen() {
            if (_preventStaminaRegen == null || _preventStaminaRegen.HasBeenDiscarded) {
                _preventStaminaRegen = Hero.AddElement(new PreventStaminaRegenMarker());
            }
        }

        protected void DisableStaminaRegenPrevent() {
            _preventStaminaRegen?.Discard();
            _preventStaminaRegen = null;
        }
        
        
        // === Internal Updates
        protected virtual bool BeforeEnter(out HeroStateType desiredState) {
            desiredState = HeroStateType.Invalid;
            return true;
        }

        // === Discarding
        protected override void OnDiscard(bool fromDomainDrop) {
            DisableStaminaRegenPrevent();
        }
    }
}

