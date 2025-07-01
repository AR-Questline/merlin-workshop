using Awaken.TG.Assets;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Utility.VFX {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Allows logic to spawn VFX at location.")]
    public class VfxSpawnerAttachment : MonoBehaviour, IAttachmentSpec {
        const string VfxGroup = "VFX";
        const string SpawningGroup = "Spawning";
        
        [ARAssetReferenceSettings(new[] { typeof(GameObject) }, true, AddressableGroup.VFX)]
        [FoldoutGroup(VfxGroup)] public ShareableARAssetReference vfxEffect;
        [FoldoutGroup(VfxGroup)] public float vfxLifetime = PrefabPool.DefaultVFXLifeTime;

        [FoldoutGroup(SpawningGroup)] public bool spawnOnInitialize = false;
        [FoldoutGroup(SpawningGroup)] public bool spawnOnLogicEnable = true;
        [FoldoutGroup(SpawningGroup)] public bool spawnOnLogicDisable = false;
        
        public Element SpawnElement() {
            return new VfxSpawner();
        }

        public bool IsMine(Element element) {
            return element is VfxSpawner;
        }
    }
}