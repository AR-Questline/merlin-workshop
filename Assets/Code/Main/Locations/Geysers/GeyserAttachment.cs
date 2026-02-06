using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Geysers {
    public class GeyserAttachment : MonoBehaviour, IAttachmentSpec {
        [Title("Intervals")]
        public float firstUseDelay = 0f;
        public float activeTime = 5f;
        public float inactiveTime = 5f;
        [Title("Activation")]
        public float height;
        public float raiseDuration = 1f;
        public float dropDuration = 1f;

        public Element SpawnElement() {
            return new GeyserElement();
        }
        
        public bool IsMine(Element element) {
            return element is GeyserElement;
        }
    }
}
