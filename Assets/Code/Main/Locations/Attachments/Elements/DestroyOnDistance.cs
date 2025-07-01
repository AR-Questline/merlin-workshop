using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class DestroyOnDistance : Element<Location>, IRefreshedByAttachment<DestroyOnDistanceAttachment> {
        public override ushort TypeForSerialization => SavedModels.DestroyOnDistance;

        int DestroyDistance { get; set; }
        Hero Hero => Hero.Current;
        
        Transform _locationTransform;
        bool _initialized;

        public void InitFromAttachment(DestroyOnDistanceAttachment spec, bool isRestored) {
            DestroyDistance = spec.destroyDistance;
        }

        protected override void OnInitialize() {
            this.GetOrCreateTimeDependent().WithUpdate(OnUpdate);
            ParentModel.OnVisualLoaded(OnVisualLoaded);
        }

        void OnVisualLoaded(Transform parentTransform) {
            _locationTransform = parentTransform;
            _initialized = true;
        }

        void OnUpdate(float deltaTime) {
            if (!_initialized || Hero == null) {
                return;
            }
            Vector3 offset = Hero.Coords - _locationTransform.position;
            float sqrLen = offset.sqrMagnitude;
            if (sqrLen > DestroyDistance * DestroyDistance) {
                ParentModel.Discard();
            }
        }
    }
}
