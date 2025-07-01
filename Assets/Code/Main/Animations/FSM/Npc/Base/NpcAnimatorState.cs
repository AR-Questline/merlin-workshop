using System;
using Animancer;
using Awaken.CommonInterfaces.Animations;
using Awaken.TG.Main.Animations.FSM.Shared;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.Main.Utility.Animations.ARTransitions;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.Utility.Debugging;
using UnityEngine;
using UniversalProfiling;

namespace Awaken.TG.Main.Animations.FSM.Npc.Base {
    public abstract partial class NpcAnimatorState<T> : NpcAnimatorState where T : NpcAnimatorSubstateMachine {
        protected new T ParentModel { get; private set; }

        protected override void OnInitialize() {
            ParentModel = base.ParentModel as T;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            ParentModel = default;
        }
    }
    
    public abstract partial class NpcAnimatorState : ARAnimatorState<NpcElement, NpcAnimatorSubstateMachine> {
        static readonly UniversalProfilerMarker AfterEnterMarker = new(Color.yellow, $"{nameof(NpcAnimatorState)}.{nameof(AfterEnter)}");

        public abstract NpcStateType Type { get; }
        public virtual bool CanUseMovement => true;
        public virtual bool CanOverrideDestination => true;
        public virtual bool ResetMovementSpeed => false;
        public virtual bool CanBeExited => true;
        public AnimancerState CurrentState { get; protected set; }
        protected AnimancerLayer AnimancerLayer => ParentModel.AnimancerLayer;
        protected NpcElement Npc => ParentModel.ParentModel;
        protected float RemainingDuration {
            get {
                if (!_currentStateIsClipTransition) {
                    return float.MaxValue;
                }

                if (!Entered) {
                    return float.MaxValue;
                }
                
                if (CurrentState == null) {
                    return float.MaxValue;
                }
                
                if (CurrentState.Clip == null) {
                    Log.Important?.Warning($"{LogUtils.GetDebugName(this)} has null Clip!", NpcAnimancer.gameObject);
                    return 0;
                }
                
                return CurrentState.RemainingDuration;
            }
        }
        
        protected float TimeElapsedNormalized {
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

        protected ARNpcAnimancer NpcAnimancer => ParentModel.NpcAnimancer;
        [UnityEngine.Scripting.Preserve] protected bool UseCombatMovement => Npc.UsesCombatMovementAnimations && EquippedItem != null;
        protected NpcMovementState GetCurrentMovementMovementState {
            get {
                if (EquippedItem != null && Npc.UsesCombatMovementAnimations) {
                    return NpcMovementState.Combat;
                }
                if (Npc is { NpcAI: { InAlert: true}, UsesAlertMovementAnimations: true }) {
                    return NpcMovementState.Alert;
                }
                if (Npc is {NpcAI: { InFlee: true } }) {
                    return NpcMovementState.Fear;
                }
                return NpcMovementState.Idle;
            }
        }
        protected Item EquippedItem => Npc.Inventory.EquippedItem(EquipmentSlotType.MainHand)
                                       ?? Npc.Inventory.EquippedItem(EquipmentSlotType.OffHand);
        protected virtual NpcStateType StateToEnter => Type;
        
        bool _currentStateIsClipTransition;
        AnimatorBridge _registeredAnimatorBridge;

        public void OnStatesCollectionChanged(ARNpcAnimancer arNpcAnimancer) {
            if (!arNpcAnimancer.Visible) {
                return;
            }

            if (!Entered) {
                return;
            }

            if (ParentModel.CurrentAnimatorState != this) {
                return;
            }

            if (!_currentStateIsClipTransition) {
                return;
            }

            if (CurrentState == null || CurrentState.Clip == null) {
                Log.Critical?.Error($"Removed clip from animancer that was used by {this}", NpcAnimancer.gameObject);
            }
        }

        public override void Enter(float _, float? overrideCrossFadeTime, Action<ITransition> onNodeLoaded = null) {
            Entered = false;
            NpcAnimancer.GetAnimancerNode(StateToEnter, n => {
                OnNodeLoaded(n, overrideCrossFadeTime);
                onNodeLoaded?.Invoke(n);
            }, OnFailedFindNode);
        }

        public override void Exit(bool restarted = false) {
            Entered = false;
            Npc.Controller.UnmarkTargetRootRotationForState(Type);
            
            if (_registeredAnimatorBridge != null) {
                _registeredAnimatorBridge.UnregisterStateProvider(this as IAnimatorBridgeStateProvider);
                _registeredAnimatorBridge = null;
            }

            CurrentState = null;
            base.Exit(restarted);
        }

        protected virtual void OnNodeLoaded(ITransition node, float? overrideCrossFadeTime) {
            if (ParentModel == null || ParentModel.HasBeenDiscarded || ParentModel.CurrentAnimatorState != this) {
                return;
            }

            float fadeDuration = overrideCrossFadeTime ?? node.FadeDuration;
            FadeMode fadeMode = overrideCrossFadeTime.HasValue ? FadeMode.FixedDuration : node.FadeMode;
                
            if (_currentStateIsClipTransition && CurrentState is { IsPlaying: true } && CurrentState.Key == node.Key) {
                fadeMode = FadeMode.FromStart;
            }
            
            if (node is ClipTransition { IsLooping: false }) {
                fadeMode = FadeMode.FromStart;
            }
            
#if UNITY_EDITOR
            if (node is ClipTransition c && c.Clip == null) {
                Log.Minor?.Error($"Null AnimationClip assigned for state: {StateToEnter}! In npc: {Npc}", Npc.Controller);
            }
#endif
            CurrentState = AnimancerLayer.Play(node, fadeDuration, fadeMode);
            if (fadeMode is FadeMode.FromStart) {
                NpcAnimancer.RebindAnimationRigging();
            }
            _currentStateIsClipTransition = CurrentState is ClipState;
            
            if (node is ARClipTransition clipTransition && clipTransition.RootRotationDelta != 0.0f) {
                Npc.Controller.SetTargetRootRotationFromState(Type, clipTransition.RootRotationDelta);
            }
            
            NpcAnimancer.RefreshUpdateSpeedsForState(CurrentState);
            
            if (this is IAnimatorBridgeStateProvider animatorStateProvider) {
                _registeredAnimatorBridge = AnimatorBridge.GetOrAddDefault(NpcAnimancer.Animator);
                _registeredAnimatorBridge.RegisterStateProvider(animatorStateProvider);
            }
            AfterEnterMarker.Begin();
            AfterEnter(0);
            AfterEnterMarker.End();
            
            Entered = true;
        }
        
        void OnFailedFindNode() {
            if (!NpcAnimancer.Visible) {
                return;
            }

            if (ParentModel.CurrentAnimatorState != this) {
                return;
            }

            if (Type == NpcStateType.Idle) {
                return;
            }
            
            ParentModel.SetCurrentState(ParentModel.DefaultState);
        }

        public virtual void OnAnimancerUnload() {
            CurrentState = null;
        }

        protected enum NpcMovementState {
            Idle,
            Alert,
            Combat,
            Fear,
        }
    }
}