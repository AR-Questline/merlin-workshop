using Awaken.TG.Main.AI.Debugging;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.AI.Movement {
    public partial class NpcMovement : Element<NpcElement> {
        public sealed override bool IsNotSaved => true;

        public NpcController Controller { get; private set; }
        public MovementState CurrentState => _interruptState?.Get ?? _mainState?.Get;

        MovementState _mainState;
        MovementState _interruptState;

        // === Events
        public new static class Events {
            public static readonly Event<NpcElement, MovementState> OnMovementInterrupted = new(nameof(OnMovementInterrupted));
            public static readonly Event<NpcElement, MovementState> OnMovementStoppedInterrupt = new(nameof(OnMovementStoppedInterrupt));
        }

        protected override void OnInitialize() {
            Controller = ParentModel.CharacterView.transform.GetComponentInChildren<NpcController>(true);
            
            _mainState = new NoMove();
            _mainState.Setup(this);
            _mainState.Enter();
            
            ParentModel.GetOrCreateTimeDependent().WithUpdate(Update);
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            ParentModel.GetTimeDependent()?.WithoutUpdate(Update);
            if (_interruptState != null) {
                _interruptState.Exit(true);
            } else {
                _mainState.Exit(true);
            }
            Object.Destroy(Controller);
        }

        public MovementState ChangeMainState(MovementState state, MovementState.OverrideMovementStateClash onOverride = null) {
            NpcHistorian.NotifyMovement(ParentModel, $"ChangeMainState with {state} (_interruptingState: {_interruptState}, _mainState: {_mainState})");
            onOverride ??= DefaultOnOverrideAction;
            _mainState.SetOverride(state, onOverride, true, _interruptState != null);
            return state;
        }
        
        public void ResetMainState(MovementState stateToReset) {
            NpcHistorian.NotifyMovement(ParentModel, $"ChangeMainState with {stateToReset} (_interruptingState: {_interruptState}, _mainState: {_mainState})");
            _mainState.ResetOverride(stateToReset);
        }

        public void InterruptState(MovementState state) {
            NpcHistorian.NotifyMovement(ParentModel, $"InterruptState with {state} (_interruptingState: {_interruptState}, _mainState: {_mainState})");
            ExitCurrentState();
            _interruptState = state;
            _interruptState.Setup(this);
            _interruptState.Enter();
            ParentModel.Trigger(Events.OnMovementInterrupted, state);
        }
        
        public void StopInterrupting() {
            NpcHistorian.NotifyMovement(ParentModel, $"StopInterrupting (_interruptingState: {_interruptState}, _mainState: {_mainState})");
            if (_interruptState == null) {
                return;
            }
            _interruptState.Exit(true);
            _interruptState = null;
            _mainState.Enter();
            ParentModel.Trigger(Events.OnMovementStoppedInterrupt, _interruptState);
        }

        void ExitCurrentState() {
            CurrentState.Exit();
            _interruptState = null;
        }

        void Update(float deltaTime) {
            if (_interruptState != null) {
                _interruptState.Update(deltaTime);
            } else {
                _mainState.Update(deltaTime);
            }
        }

        public static void DefaultOnOverrideAction(MovementState mainState, MovementState from, MovementState to) {
#if DEBUG
            if (!DebugReferences.LogMovementUnsafeOverrides) {
                return;
            }
            Log.Important?.Error($"[{mainState.Npc?.ID}] Movement state {from?.GetType().Name}, was overriden by state {to?.GetType().Name}");
#endif
        }
    }
}