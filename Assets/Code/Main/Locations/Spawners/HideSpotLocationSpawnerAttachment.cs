using Awaken.TG.Assets;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Spawners {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "Spawns a single NPC from a hiding spot on interaction or proximity.")]
    public class HideSpotLocationSpawnerAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField, TemplateType(typeof(LocationTemplate))]
        TemplateReference locationToSpawn;
        [Header("VFX")]
        [ARAssetReferenceSettings(new[] { typeof(GameObject) }, group: AddressableGroup.VFX)]
        public ShareableARAssetReference spawnVFX;
        public float vfxDuration = 5f;
        
        [Header("Story")]
        public StoryBookmark storyOnKilled;
        
        [Header("Spawned location properties")]
        public bool hideLocationOutsideVisualBand = true;
        
        [Header("Activation properties")]
        public float fallbackActivationDistance = 5f;
        [SerializeField, TemplateType(typeof(LocationTemplate))]
        TemplateReference pickableActivatorTemplate;
        
        public LocationTemplate LocationToSpawn => locationToSpawn.Get<LocationTemplate>();
        public LocationTemplate PickableActivatorTemplate => pickableActivatorTemplate.Get<LocationTemplate>();
        
        public Element SpawnElement() => new HideSpotLocationSpawner();
        public bool IsMine(Element element) => element is HideSpotLocationSpawner;
    }
}