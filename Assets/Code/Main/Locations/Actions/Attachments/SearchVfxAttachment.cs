using Awaken.TG.Assets;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    public class SearchVfxAttachment : MonoBehaviour, IAttachmentSpec {
        [ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)]
        public ShareableARAssetReference vfx;
        
        public Element SpawnElement() => new SearchActionVfx();
        public bool IsMine(Element element) => element is SearchActionVfx;
    }
}
