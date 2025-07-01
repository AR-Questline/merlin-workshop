using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Memories;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    public partial class LogicEmitterAction : LogicEmitterActionBase<LogicEmitterAttachment> {
        public override ushort TypeForSerialization => SavedModels.LogicEmitterAction;

        const string ActivatedEvent = "Activated";
        const string NotActivatedEvent = "FailedActivated";
        
        [Saved] bool _currentState;
        [Saved] bool _active;

        public override bool IsIllegal => _attachment.isIllegal;

        protected override void OnInitialize() {
            base.OnInitialize();
            _currentState = _attachment.initialState;
            _active = _attachment.initialActive;
        }
        
        protected override bool IsActive() {
            if (_attachment.useActiveSystem) {
                if (_attachment.activeOnFlag) {
                    GameplayMemory memory = Services.Get<GameplayMemory>();
                    return memory.Context().Get(_attachment.activationFlag, false);
                } else {
                    return _active;
                }
            }
            return true;
        }

        protected override void Interact(bool active, bool? forcedState = null) {
            if (IsIllegal) {
                var crimeSource = new FakeCrimeSource(ParentModel.DefaultOwner, _attachment.bounty);
                Crime.Custom(crimeSource).TryCommitCrime();
            }
            if (active) {
                if (forcedState.HasValue) {
                    _currentState = forcedState.Value;
                } else {
                    _currentState = _attachment.changeStates ? !_currentState : !_attachment.initialState;
                }
            }
            base.Interact(active, forcedState);
        }

        protected override void OnLateInit() {
            foreach (var location in Locations) {
                location.TryGetElement<LogicReceiverAction>()?.ListenTo(LogicReceiverAction.Events.StateChanged, OnReceiverUpdate, this);
            }
        }

        protected override void OnVisualLoaded(Transform parentTransform) {
            base.OnVisualLoaded(parentTransform);
            OnAnimatorSetup();
        }

        protected override void OnAnimatorSetup() {
            if (_animator == null) {
                return;
            }
            
            if (_attachment.changeStates) {
                _animator.SetBool(ActiveHash, _attachment.negateVisualState ? !_currentState : _currentState);
            }
        }

        protected override void OnAnimatorUpdate(bool toggleState) {
            if (_attachment.changeStates && toggleState) {
                _animator.SetBool(ActiveHash, _attachment.negateVisualState ? !_currentState : _currentState);
            } else {
                _animator.SetTrigger(TriggerHash);
            }
        }

        void OnReceiverUpdate(bool state) {
            if (!_attachment.useStates || !_attachment.syncWithReceiver || _currentState == state) {
                return;
            }
            _currentState = state;
            UpdateAnimator(true);
        }

        protected override void SendInteractEventsToLocation(Location location, bool active) {
            if (location == null) {
                return;
            }
            
            if (active) {
                if (_attachment.useInteraction) {
                    HeroInteraction.StartInteraction(null, location, out _);
                }
                if (_attachment.useStates) {
                    location.TriggerVisualScriptingEvent(ActivatedEvent, _currentState);
                    location.TryGetElement<LogicReceiverAction>()?.OnActivation(_currentState);
                }
            } else {
                if (_attachment.useStates) {
                    location.TriggerVisualScriptingEvent(NotActivatedEvent);
                    location.TryGetElement<LogicReceiverAction>()?.OnFailedActivation();
                }
            }
        }
        
        [UnityEngine.Scripting.Preserve]
        public void ChangeActivity(bool newActive) {
            _active = newActive;
        }
    }
}