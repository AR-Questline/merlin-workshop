using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [RequireComponent(typeof(PortalAttachment))]
    public class DemoPortalHideLocationName : MonoBehaviour {
        void Awake() {
            if (GameMode.IsDemo) {
                GetComponent<PortalAttachment>().isLocationNameHidden = true;
            }
        }
    }
}