using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    public class DiscardOverDistanceBandAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] uint band = 3;
        
        public uint Band => band;
        
        public Element SpawnElement() => new DiscardOverDistanceBand();
        public bool IsMine(Element element) => element is DiscardOverDistanceBand;
    }
}