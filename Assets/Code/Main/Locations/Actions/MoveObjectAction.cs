using Awaken.TG.Main.Fights.Factions;
using UnityEngine;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.GameObjects;
using DG.Tweening;

namespace Awaken.TG.Main.Locations.Actions {
    public partial class MoveObjectAction : AbstractLocationAction, IRefreshedByAttachment<MoveObjectAttachment> {
        public override ushort TypeForSerialization => SavedModels.MoveObjectAction;

        [Saved] bool _used;
        
        string _objectToMove;
        bool _oneTimeUse;
        Vector3 _startPosition, _startRotation, _endPosition, _endRotation;
        
        bool _initialized;

        GameObject _baseGameObject;
        Transform _toMove;

        // === Properties
        Transform ObjectToMove {
            get {
                if (_toMove == null) {
                    _toMove = _baseGameObject.FindChildRecursively(_objectToMove);
                }
                return _toMove;
            }
        }

        // === Constructors
        public void InitFromAttachment(MoveObjectAttachment spec, bool isRestored) {
            _objectToMove = spec.objectToMove.name;
            _startPosition = spec.objectToMove.localPosition;
            _startRotation = spec.objectToMove.localRotation.eulerAngles;
            _endPosition = spec.endPosition;
            _endRotation = spec.endRotation;
            _oneTimeUse = spec.oneUseOnly;
        }

        // === Execution
        protected override void OnInitialize() {
            ParentModel.OnVisualLoaded(Init);
        }

        protected override void OnRestore() {
            ParentModel.OnVisualLoaded(Init);
        }

        void Init(Transform parentTransform) {
            _baseGameObject = parentTransform.gameObject;
            UpdateVisuals(true);
            _initialized = true;
        }

        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            if (!_initialized || (_used && _oneTimeUse)) {
                return;
            }
            _used = !_used;

            UpdateVisuals();
        }

        void UpdateVisuals(bool instant = false) {
            ObjectToMove.DOLocalMove(_used ? _endPosition : _startPosition, instant ? 0 : 0.5f).SetUpdate(instant);
            ObjectToMove.DOLocalRotate(_used ? _endRotation : _startRotation, instant ? 0 : 0.5f).SetUpdate(instant);
        }
    }
}
