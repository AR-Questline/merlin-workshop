using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public class PortalTrigger : MonoBehaviour {
        VPortal _owner;
        
        public void Init(VPortal owner) {
            _owner = owner;
            if (gameObject.layer is RenderLayers.Default) {
                gameObject.layer = RenderLayers.TriggerVolumes;
            }
        }
        
        void OnTriggerEnter(Collider other) {
            _owner.OnTriggerEnter(other);
        }
    }
}