using Awaken.TG.Assets;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Housing {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Housing - Marks Furniture Room.")]
    public class FurnitureRoomAttachment : MonoBehaviour, IAttachmentSpec  {
        public Transform barrierTransform;
        [LocStringCategory(Category.Housing)]
        public LocString displayName;
        [LocStringCategory(Category.Housing)]
        public LocString description;
        public HousingUnlockRequirement unlockRequirement;
        [UIAssetReference]
        public ShareableSpriteReference roomIcon;
        
        public Element SpawnElement() {
            return new FurnitureRoom();
        }

        public bool IsMine(Element element) {
            return element is FurnitureRoom;
        }
    }
}