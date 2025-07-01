using System;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Attaches NPC interactions to location (it prevents NPC from sleeping in the same bed as Hero).")]
    public class LocationWithNpcInteractionAttachment : MonoBehaviour, IAttachmentSpec {
        
        [SerializeField] NpcInteraction[] interactions = Array.Empty<NpcInteraction>();
        
        public NpcInteraction[] Interactions => interactions;
        
        public Element SpawnElement() {
            return new LocationWithNpcInteractionElement();
        }
        
        public bool IsMine(Element element) {
            return element is LocationWithNpcInteractionElement;
        }
    }
}