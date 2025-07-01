using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "Attaches actor to non-NPC location.")]
    public class LocationWithActorAttachment : MonoBehaviour, IAttachmentSpec {
        public ActorRef actorRef;

        public Element SpawnElement() {
            return new LocationWithActor();
        }

        public bool IsMine(Element element) => element is LocationWithActor;
    }
}