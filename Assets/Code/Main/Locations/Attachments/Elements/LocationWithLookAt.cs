using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class LocationWithLookAt : Element<Location>, IRefreshedByAttachment<LocationWithLookAtAttachment>, IWithLookAt {
        public override ushort TypeForSerialization => SavedModels.LocationWithLookAt;

        LocationWithLookAtAttachment _spec;

        Transform _lookAtTarget;
        
        public Transform LookAtTarget => _lookAtTarget;

        public void InitFromAttachment(LocationWithLookAtAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnInitialize() {
            ParentModel.OnVisualLoaded(OnVisualLoaded);
        }

        void OnVisualLoaded(Transform visual) {
            var go = new GameObject("LookAtTarget");
            _lookAtTarget = go.transform;
            _lookAtTarget.SetParent(visual);
            _lookAtTarget.position = visual.position + _spec.LookAtTargetOffset;
        }
    }
}