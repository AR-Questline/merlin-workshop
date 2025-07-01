using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Memories.Journal;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes.Tags;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Discovery {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Common, "Discovers location when Hero is close enough. Can be setup as Fast Travel Point.")]
    public class LocationDiscoveryAttachment : MonoBehaviour, IAttachmentSpec {

        [SerializeField, Tags(TagsCategory.Flag)] string unlockFlag;
        [SerializeField, RichEnumExtends(typeof(DiscoveryExperience))] RichEnumReference experienceGain = DiscoveryExperience.Default;
        [SerializeField] bool isFastTravel;
        [SerializeField, ShowIf(nameof(isFastTravel)), Indent] Transform fastTravelPoint;
        [GuidSelection] public JournalGuid guid;
        
        public string UnlockFlag => unlockFlag;
        public float ExpMulti => experienceGain.EnumAs<DiscoveryExperience>().ExpMulti;
        public bool IsFastTravel => isFastTravel;
        public Vector3 FastTravelLocation => Ground.SnapToGround(fastTravelPoint != null ? fastTravelPoint.position : transform.position);
        
        public Element SpawnElement() => new LocationDiscovery();
        public bool IsMine(Element element) => element is LocationDiscovery;
        
        void AddPrimitive<TPrimitive>(string primitiveName) where TPrimitive : Component, IAreaPrimitiveProvider {
            var go = new GameObject(primitiveName);
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            go.AddComponent<TPrimitive>();
        }
        
        [Button, BoxGroup("Area")] void AddSphere() => AddPrimitive<SpherePrimitiveProvider>("Sphere");
        [Button, BoxGroup("Area")] void AddAxisAlignedBox() => AddPrimitive<AxisAlignedBoxPrimitiveProvider>("AxisAlignedBox");
    }
}