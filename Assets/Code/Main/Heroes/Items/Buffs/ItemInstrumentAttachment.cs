using Awaken.TG.Assets;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;
using UnityEngine.Serialization;

namespace Awaken.TG.Main.Heroes.Items.Buffs {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.Rare, "For items that can be played.")]
    public class ItemInstrumentAttachment : MonoBehaviour, IAttachmentSpec {
        [PrefabAssetReference]
        public ShareableARAssetReference instrumentAssetRef;
        
        public Element SpawnElement() {
            return new ItemInstrument();
        }

        public bool IsMine(Element element) => element is ItemInstrument;
    }
}