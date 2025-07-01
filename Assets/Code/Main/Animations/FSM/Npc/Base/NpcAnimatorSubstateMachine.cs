using System;
using System.Collections.Generic;
using Animancer;
using Awaken.TG.Main.AI.Debugging;
using Awaken.TG.Main.Animations.FSM.Shared;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Animations.FSM.Npc.Base {
    public abstract partial class NpcAnimatorSubstateMachine : ARAnimatorSubstateMachine<NpcElement> {
        // === Fields
        bool _isEnabled;
        bool _isInitialized;
        Transform _rootTransform;
        readonly Dictionary<NpcStateType, NpcAnimatorState> _states = new();
        
        // === Properties
        public ARNpcAnimancer NpcAnimancer { get; }
        public override string ParentLayerName => string.Empty;
        public abstract NpcFSMType Type { get; }
        public NpcAnimatorState CurrentAnimatorState { get; protected set; }
        public float ExitDurationFromAttackAnimations { get; private set; } = 0.3f;
        public Vector3 RootForward => RootTransform.forward;

        public abstract NpcStateType DefaultState { get; }
        protected abstract bool EnableOnInitialize { get; }
        protected override AvatarMask AvatarMask { get; }
        protected override int LayerIndex { get; }
        
        Transform RootTransform {
            get {
                if (_rootTransform == null) {
                    _rootTransform = ParentModel.Hips.parent;
                }
                return _rootTransform;
            }
        }

        // === Events
        public new static class Events {
            public static readonly Event<NpcElement, NpcAnimatorState> NpcDashBackEnded = new(nameof(NpcDashBackEnded));
        }
        
        // === Constructor
        protected NpcAnimatorSubstateMachine(Animator animator, ARNpcAnimancer animancer, int layerIndex, AvatarMask avatarMask) : base(animator, animancer) {
            NpcAnimancer = animancer;
            AvatarMask = avatarMask;
            LayerIndex = layerIndex;
        }
        
        // === Initialization
        protected override void OnInitialize() {
            base.OnInitialize();
#if UNITY_EDITOR
            AnimancerLayer.SetDebugName(Type.ToString());
#endif
            if (EnableOnInitialize) {
                EnableFSM();
            }
            
            ParentModel.ParentModel.ListenTo(NpcElement.Events.AfterNpcInVisualBand, OnVisualInBand, this);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            DisableFSM(true);
        }

        public void AnimancerEnabled() {
            OnVisualInBand();
        }
        
        void OnVisualInBand() {
            if (!Animator.gameObject.activeInHierarchy) {
                NpcHistorian.NotifyAnimations(this, $"{GetType().Name}.OnVisualInBand - Animator disabled");
                return;
            }
            
            if (_isEnabled) {
                EnableFSM();
            } else {
                NpcHistorian.NotifyAnimations(this, $"{GetType().Name}.OnVisualInBand - _isEnabled is false");
            }
        }
        
        public void OnNpcVisualInitialize() {
            ExitDurationFromAttackAnimations = ParentModel.Controller.exitDurationFromAttackAnimations;
        }

        // === Public API
        public bool HasState(NpcStateType stateType) {
            return _states.ContainsKey(stateType);
        }
        
        public override void EnableFSM() {
            // --- AI Can be spawned and disabled in the same frame. Don't initialize animator in that case but just wait for player to come in range.
            if (Animator.gameObject.activeInHierarchy) {
                // --- If we are already correctly initialized do nothing.
                if (IsLayerActive && _isInitialized && CurrentAnimatorState != null) {
                    if (CurrentAnimatorState.CurrentState is not { IsPlaying: true }) {
                        CurrentAnimatorState.Enter(0, CurrentAnimatorState.EntryTransitionDuration);
                    }
                    NpcHistorian.NotifyAnimations(this, $"{GetType().Name}.EnableFSM - Already enabled");
                    return;
                }
                NpcHistorian.NotifyAnimations(this, $"{GetType().Name}.EnableFSM - Enabling correctly");
                BaseEnableFSM(OnUpdate);
                SetCurrentState(DefaultState, 0);
                AfterEnable();
                _isInitialized = true;
            } else {
                NpcHistorian.NotifyAnimations(this, $"{GetType().Name}.EnableFSM - Animator disabled");
            }

            _isEnabled = true;
        }
        
        public void OnAnimancerVisibilityToggled(bool visible) {
            if (visible && _isEnabled) {
                CurrentAnimatorState?.Enter(0, 0);
            } else if (!visible) {
                CurrentAnimatorState?.OnAnimancerUnload();
            }
        }

        public override void DisableFSM(bool fromDiscard = false) {
            if (!CanBeDisabled) {
                NpcHistorian.NotifyAnimations(this, $"{GetType().Name}.DisableFSM - cannot be disabled");
                return;
            }

            NpcHistorian.NotifyAnimations(this, $"{GetType().Name}.DisableFSM");
            BaseDisableFSM(OnUpdate, fromDiscard);
            OnDisable(fromDiscard);
            _isEnabled = false;
            _isInitialized = false;
        }

        protected override void OnDisable(bool fromDiscard) {
            SetCurrentState(NpcStateType.None, 0f);
        }
        
        public void SetCurrentState(NpcStateType stateType, float? overrideCrossFadeTime = null, bool force = false, Action<ITransition> onNodeLoaded = null) {
            NpcHistorian.NotifyAnimations(this, $"{GetType().Name} requested state change to {stateType.ToString()}");
            if (ParentModel?.HasBeenDiscarded ?? true) {
                NpcHistorian.NotifyAnimations(this, $"{GetType().Name} Changing {stateType.ToString()} - ParentModel discarded");
                return;
            }

            if (!force && CurrentAnimatorState is { CanBeExited: false }) {
                NpcHistorian.NotifyAnimations(this, $"{GetType().Name} Changing {stateType.ToString()} - Current state {CurrentAnimatorState.Type} cannot be exited");
                onNodeLoaded?.Invoke(null);
                return;
            }
            
            if (CurrentAnimatorState != null && CurrentAnimatorState.Type == stateType && !CurrentAnimatorState.CanReEnter) {
                NpcHistorian.NotifyAnimations(this, $"{GetType().Name} Changing {stateType.ToString()} - Current state {CurrentAnimatorState.Type} cannot be reentered");
                onNodeLoaded?.Invoke(null);
                return;
            }

            CurrentAnimatorState?.Exit();
            if (_states.TryGetValue(stateType, out var state)) {
                if (state == null) {
                    NpcHistorian.NotifyAnimations(this, $"{GetType().Name} State of type {stateType.ToString()} is null");
                } else {
                    NpcHistorian.NotifyAnimations(this, $"{GetType().Name} State of type {stateType.ToString()} is not null");
                }
                CurrentAnimatorState = state;
                CurrentAnimatorState?.Enter(0, overrideCrossFadeTime, onNodeLoaded);
                return;
            }

            CurrentAnimatorState = null;
            Log.Important?.Error($"Failed to enter state: {stateType}! No such state exists for FSM: {this}");
            NpcHistorian.NotifyAnimations(this, $"State {stateType.ToString()} of {GetType().Name} does not exist");
        }
        
        void OnUpdate(float deltaTime) {
            Location location = ParentModel.ParentModel;
            if (location == null || location.HasBeenDiscarded) {
                return;
            }
            
            if (location.Interactability == LocationInteractability.Active) {
                CurrentAnimatorState?.Update(deltaTime);
            }
        }
        
        // === States Management
        protected void AddState(NpcAnimatorState state) {
            _states.Add(state.Type, state);
            AddElement(state);
        }
    }
}