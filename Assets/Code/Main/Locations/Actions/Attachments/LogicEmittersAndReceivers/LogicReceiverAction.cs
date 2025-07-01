using Awaken.Utility;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    public partial class LogicReceiverAction : Element<Location>, IRefreshedByAttachment<LogicReceiverAttachment> {
        public override ushort TypeForSerialization => SavedModels.LogicReceiverAction;

        static readonly int NotActivatedHash = Animator.StringToHash("FailedActive"); //Only Fail Animation
        static readonly int ActivatedHash = Animator.StringToHash("Active"); //True - Open, False - Closed
        static readonly int StateSelectorHash = Animator.StringToHash("StateSelector"); //2 - Open, 0 - Closed
        
        AnimatorElement _animatorElement;
        Animator _animator;
        [Saved] bool _currentState;
        ILogicReceiver[] _additionalReceivers;
        bool _startingState;
        bool _negateStates;
        
        public bool IsActive => _currentState;

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public LogicReceiverAction() {}

        public void InitFromAttachment(LogicReceiverAttachment spec, bool isRestored) {
            // Used without spec, do not assume it exists outside of this method
            _startingState = spec.startingState;
            _negateStates = spec.negateStates;
        }
        
        protected override void OnInitialize() {
            _currentState = _startingState;
            ParentModel.OnVisualLoaded(OnVisualLoaded);
        }
        
        protected override void OnRestore() {
            ParentModel.OnVisualLoaded(OnVisualLoaded);
        }
        
        void OnVisualLoaded(Transform parentTransform) {
            _animator = parentTransform.GetComponentInChildren<Animator>();
            _additionalReceivers = parentTransform.GetComponentsInChildren<ILogicReceiver>();
            ParentModel.TryGetElement(out _animatorElement);
            SetupState(_negateStates ? !_currentState : _currentState);
        }

        public void OnActivation(bool state) {
            ChangeState(state);
        }
        
        public void OnFailedActivation() {
            if (_animatorElement != null) {
                _animatorElement.SetParameter(NotActivatedHash, new SavedAnimatorParameter {type = AnimatorControllerParameterType.Trigger});
            } else if (_animator != null) {
                _animator.SetTrigger(NotActivatedHash);
            }
        }

        void ChangeState(bool state) {
            if (_currentState == state) {
                return;
            }

            this.Trigger(Events.StateChanged, state);
            _currentState = state;
            if (_negateStates) {
                state = !state;
            }
            
            ChangeStateTo(state);
        }

        void SetupState(bool state) {
            UpdateAnimator(state);
            foreach (var receiver in _additionalReceivers) {
                receiver.OnLogicReceiverStateSetup(state);
            }
            foreach (var receiver in ParentModel.Elements<ILogicReceiverElement>()) {
                receiver.OnLogicReceiverStateSetup(state);
            }
        }

        void ChangeStateTo(bool state) {
            UpdateAnimator(state);
            foreach (var receiver in _additionalReceivers) {
                receiver.OnLogicReceiverStateChanged(state);
            }
            foreach (var receiver in ParentModel.Elements<ILogicReceiverElement>()) {
                receiver.OnLogicReceiverStateChanged(state);
            }
        }
        
        void UpdateAnimator(bool state) {
            if (_animatorElement != null) {
                var savedAnimatorParameter = new SavedAnimatorParameter {
                    intValue = state ? 2 : 0,
                    boolValue = state
                };
                _animatorElement.SetParameter(AnimatorControllerParameterType.Bool, ActivatedHash, savedAnimatorParameter);
                _animatorElement.SetParameter(AnimatorControllerParameterType.Int, StateSelectorHash, savedAnimatorParameter);
            } else if (_animator != null) {
                _animator.SetBool(ActivatedHash, state);
                _animator.SetInteger(StateSelectorHash, state ? 2 : 0);
            }
        }
        
        public new static class Events {
            public static readonly Event<LogicReceiverAction, bool> StateChanged = new(nameof(StateChanged));
        }
    }
}