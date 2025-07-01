using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class LocationStatesElement : Element<Location>, IRefreshedByAttachment<LocationStatesAttachment> {
        public override ushort TypeForSerialization => SavedModels.LocationStatesElement;

        static readonly int StateSelectorHash = Animator.StringToHash("State");
        
        [Saved] int _state;
        bool _restored;
        LocationStatesAttachment _spec;

        public void InitFromAttachment(LocationStatesAttachment spec, bool isRestored) {
            _spec = spec;
            _restored = isRestored;
            if (!isRestored) {
                _state = _spec.StartingState;
            }
        }

        protected override void OnInitialize() {
            ParentModel.OnVisualLoaded(OnVisualLoaded);
        }
        
        void OnVisualLoaded(Transform transform) {
            int newState = _state;
            _state = -1;
            ChangeState(newState, _restored);
        }

        public void NextState() {
            ChangeState((_state + 1) % _spec.States.Length);
        }

        [UnityEngine.Scripting.Preserve]
        public void PreviousState() {
            ChangeState((_state - 1 + _spec.States.Length) % _spec.States.Length);
        }

        public void ChangeState(int newState, bool fromRestore = false) {
            if (_state == newState) {
                return;
            }

            if (_state >= 0) {
                ParentModel.DisableGroup(_spec.States[_state].name);
            }
            _state = newState;
            
            if (ParentModel.TryGetElement<AnimatorElement>(out var animatorElement)) {
                var savedAnimatorParameter = new SavedAnimatorParameter {
                    type = AnimatorControllerParameterType.Int,
                    intValue = _state
                };
                animatorElement.SetParameter(StateSelectorHash, savedAnimatorParameter);
            }
            
            ParentModel.EnableGroup(_spec.States[_state].name);
            if (_spec.States[_state].interactOnStart && !fromRestore) {
                HeroInteraction.StartInteraction(Hero.Current, ParentModel, out _);
            }
        }
    }
}
