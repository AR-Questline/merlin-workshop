using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility;
using Awaken.Utility.Maths.Data;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public partial class StoryInteractionFocusOverride : Element<Story> {
        public sealed override bool IsNotSaved => true;

        DelayedVector3 _desiredPosition;
        Transform _createdFocus;
        Transform _originalFocusParent;
        Transform _additionalFocusPoint;
        
        public Transform FocusPoint => _createdFocus;
        public Transform FocusParent => _originalFocusParent;

        public StoryInteractionFocusOverride(Transform focusPoint) {
            SetFocus(focusPoint);
        }

        public void SetFocus(Transform focusParent) {
            _originalFocusParent = focusParent;

            _createdFocus ??= new GameObject("CreatedFocusOverride").transform;
            _createdFocus.SetParent(focusParent);
            _createdFocus.localPosition = Vector3.zero;
            
            _desiredPosition.SetInstant(Vector3.zero);

            if (IsInitialized) {
                UpdateAdditionalFocusPoint();
            }
            
            TriggerChange();
        }

        protected override void OnInitialize() {
            UpdateAdditionalFocusPoint();
        }

        void UpdateAdditionalFocusPoint() {
            _additionalFocusPoint = ParentModel.FocusedLocation?.TryGetElement<DialogueAction>()?.ViewFocus ??
                                    ParentModel.FocusedLocation?.TryGetElement<NpcElement>()?.Head;
            
            if (_additionalFocusPoint == null) {
                ParentModel.GetTimeDependent()?.WithoutUpdate(ProcessUpdate);
            } else {
                ParentModel.GetOrCreateTimeDependent().WithUpdate(ProcessUpdate);
            }
        }

        void ProcessUpdate(float deltaTime) {
            Vector3 direction = _additionalFocusPoint.position - _originalFocusParent.position;
            _desiredPosition.Set(direction.ToHorizontal3().normalized * 0.25f);
            
            _desiredPosition.Update(deltaTime, 0.1f);
            _createdFocus.localPosition = _desiredPosition.Value;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            ParentModel.GetTimeDependent()?.WithoutUpdate(ProcessUpdate);
            if (_createdFocus != null) {
                Object.Destroy(_createdFocus.gameObject);
            }
            _createdFocus = null;
        }
    }
}