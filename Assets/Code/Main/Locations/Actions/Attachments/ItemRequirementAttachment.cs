using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "Adds item requirement to the location interaction.")]
    public class ItemRequirementAttachment : MonoBehaviour, IAttachmentSpec {
        public ItemRequirementData itemRequirementData;

        public Element SpawnElement() => new ItemRequirement();

        public bool IsMine(Element element) => element is ItemRequirement;
    }
}