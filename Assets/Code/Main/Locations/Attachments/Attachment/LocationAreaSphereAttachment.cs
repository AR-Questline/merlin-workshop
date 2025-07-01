using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "Marks area on Map, used for Area Quest Objectives.")]
    public class LocationAreaSphereAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] float radius;

        public float Radius => radius;
        
        public Element SpawnElement() => new LocationAreaSphere();
        public bool IsMine(Element element) => element is LocationAreaSphere;

        void OnDrawGizmosSelected() {
            using var gizmosColor = new GizmosColor(Color.yellow);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}