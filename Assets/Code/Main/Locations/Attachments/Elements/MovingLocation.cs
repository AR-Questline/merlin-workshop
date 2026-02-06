using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class MovingLocation : Element<Location>, UnityUpdateProvider.IWithUpdateGeneric {
        public override ushort TypeForSerialization => SavedModels.MovingLocation;

        Vector3 _initialPosition;
        [Saved] Vector3 _finalPosition;
        float _duration;
        float _elapsed;

        public MovingLocation(Vector3 initialPosition, Vector3 finalPosition, float duration) {
            _initialPosition = initialPosition;
            _finalPosition = finalPosition;
            _duration = duration;
            _elapsed = 0f;
        }

        protected override void OnInitialize() {
            UnityUpdateProvider.GetOrCreate().RegisterGeneric(this);
        }

        protected override void OnRestore() {
            ParentModel.MoveAndRotateTo(_finalPosition, ParentModel.Rotation);
            Discard();
        }

        public void UnityUpdate() {
            if (ParentModel.HasBeenDiscarded) {
                return;
            }

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            
            var lerpedPosition = Vector3.Lerp(_initialPosition, _finalPosition, t);
            ParentModel.MoveAndRotateTo(lerpedPosition, ParentModel.Rotation);

            if (_elapsed >= _duration) {
                ParentModel.MoveAndRotateTo(_finalPosition, ParentModel.Rotation);
                Discard();
            }
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            UnityUpdateProvider.TryGet()?.UnregisterGeneric(this);
            base.OnDiscard(fromDomainDrop);
        }
    }
}