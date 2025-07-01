using System;
using System.Text;
using Awaken.TG.Main.AI.Debugging;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.Utility.Debugging;
using Unity.VisualScripting;
using UnityEngine.AI;

namespace Awaken.TG.Main.AI.Movement.States {
    public abstract class MovementState {
        public MovementState Get => _override ?? this;
        MovementState _parent;
        MovementState _override;
        OverrideMovementStateClash _overrideChangedCallback;

        public abstract VelocityScheme VelocityScheme { get; }
        public NpcElement Npc => Movement?.ParentModel;
        protected NpcMovement Movement { get; private set; }
        protected NpcController Controller => Movement?.Controller;

        public bool IsSetUp => Movement != null;
        public bool PathComplete { get; private set; } = true;
        
        bool _active, _wasPushed;
        public bool ActiveSelf => _active;
        bool ActiveLowestOverride => _override?.ActiveLowestOverride ?? _active;

        public void Enter() {
            NpcHistorian.NotifyMovement(Npc, $"Entered state {this}");
            _active = true;
            PathComplete = true;
            if (!_wasPushed) {
                OnPush();
                _wasPushed = true;
            }
            if (_override == null) {
                OnEnter();
            } else {
                _override.Enter();
            }
        }
        public void Exit(bool isBeingPop = false, bool isMainState = false) {
            NpcHistorian.NotifyMovement(Npc, $"Exited state {this}");
            if (_override == null) {
                OnExit();
            } else {
                _override.Exit(isBeingPop);
            }

            if (isBeingPop && !isMainState) {
                OnPop();
                _wasPushed = false;
            }

            _active = false;
        }

        public void Update(float deltaTime) {
            if (_override == null) {
                OnUpdate(deltaTime);
            } else {
                _override.Update(deltaTime);
                OnUpdateOverriden();
            }
        }

        protected virtual void OnPush() {}
        protected virtual void OnPop() {}
        protected abstract void OnEnter();
        protected abstract void OnExit();
        protected abstract void OnUpdate(float deltaTime);
        protected virtual void OnUpdateOverriden() { }
        [UnityEngine.Scripting.Preserve] public virtual void Performed() { }
        [UnityEngine.Scripting.Preserve] public virtual void Canceled() { }

        public void Setup(NpcMovement movement) {
            Movement = movement;
        }

        public event Action OnEnd;
        protected void End() {
            _parent?.EndOverride(this);
            OnEnd?.Invoke();
        }

        public void SetOverride(MovementState state, OverrideMovementStateClash onOverride, bool isChangingMainState, bool isInterrupted) {
            if (state == _override) {
                return;
            }
            
            bool wasActive = ActiveLowestOverride;
            if (wasActive) {
                Exit(isChangingMainState, isChangingMainState);
            }

            var previousCallback = _overrideChangedCallback;
            var previousOverride = _override;

            _override = state;
            _override._parent = this;
            _override.Movement = Movement;
            _overrideChangedCallback = onOverride;
            
            if (wasActive && !isInterrupted) {
                _override.Enter();
            }

            if (previousOverride != null) {
                previousCallback?.Invoke(this, previousOverride, _override);
            }
        }

        void EndOverride(MovementState state) {
            if (_override == null || _override != state) {
                return;
            }
            _override.Exit(true);
            _override._parent = null;
            _override.Movement = null;
            _override = null;
            _overrideChangedCallback = null;
            Enter();
        }
        
        public void ResetOverride(MovementState stateToReset) {
            if (_override != null && (_override == stateToReset || stateToReset == null)) {
                EndOverride(_override);
                _overrideChangedCallback = null;
            } else if (_override != null && _override != stateToReset) {
#if DEBUG
                if (DebugReferences.LogMovementUnsafeOverrides) {
                    Log.Important?.Error($"[{Npc?.ID}] Tried to remove override {stateToReset} but current override is {_override}");
                }
#endif
            }
        }

        public override string ToString() {
            StringBuilder sb = new();
            sb.Append("[--");
            sb.Append(GetType().Name);
            sb.Append("--]");
            var parent = _parent;
            while (parent != null) {
                sb.Insert(0, "::");
                sb.Insert(0, parent.GetType().Name);
                parent = parent._parent;
            }
            var child = _override;
            while (child != null) {
                sb.Append("::");
                sb.Append(child.GetType().Name);
                child = child._override;
            }
            return sb.ToString();
        }

        // === Helpers
        public delegate void OverrideMovementStateClash(MovementState mainState, MovementState from, MovementState to);
    }
}