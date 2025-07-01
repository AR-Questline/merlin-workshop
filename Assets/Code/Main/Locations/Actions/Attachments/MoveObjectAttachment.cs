using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "Moves an object on interaction.")]
    public class MoveObjectAttachment : MonoBehaviour, IAttachmentSpec {
        public Transform objectToMove;
        public Vector3 endPosition;
        public Vector3 endRotation;
        public bool oneUseOnly;
        
        public Element SpawnElement() {
            return new MoveObjectAction();
        }
        
        public bool IsMine(Element element) {
            return element is MoveObjectAction;
        }
    }
}