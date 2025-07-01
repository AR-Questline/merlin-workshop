using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes.Tags;
using System;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Clearing {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "Marks Location that can be cleared (changes icon on map) based on provided flags.")]
    public class LocationToClearAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField, Tags(TagsCategory.Flag)] string[] flagsToClear = Array.Empty<string>();

        public Element SpawnElement() => new LocationToClear();
        public bool IsMine(Element element) => element is LocationToClear;

        public string[] FlagsToClear => flagsToClear;
    }
}