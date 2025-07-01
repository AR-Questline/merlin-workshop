using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Grounds.CullingGroupSystem {
    public class LocationCullingOverrideAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] LocationRegistreeType typeOverride = LocationRegistreeType.Large;
        
        public LocationRegistreeType TypeOverride => typeOverride;
        
        public Element SpawnElement() => new LocationRegistreeTypeOverride();

        public bool IsMine(Element element) => element is LocationRegistreeTypeOverride;
    }
}