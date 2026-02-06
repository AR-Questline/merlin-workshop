using Awaken.TG.Assets;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "NPC can't enter combat or alert and is completely immune to damage and hostile actions")]
    public class CombatPreventionAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)]
        ShareableARAssetReference beingHitOverallVFX;
        [SerializeField, ShowIf(nameof(OverallVFXSetUp))]
        float overallVfxCooldown = 4f;
        [SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)]
        ShareableARAssetReference beingHitPointVFX;
        [SerializeField, ShowIf(nameof(PointVFXSetUp))]
        float pointVfxCooldown = 0.2f;
        
        public ShareableARAssetReference BeingHitOverallVFX => beingHitOverallVFX;
        public ShareableARAssetReference BeingHitPointVFX => beingHitPointVFX;
        public float OverallVfxCooldown => overallVfxCooldown;
        public float PointVfxCooldown => pointVfxCooldown;
        
        bool OverallVFXSetUp => beingHitOverallVFX is { IsSet: true };
        bool PointVFXSetUp => beingHitPointVFX is { IsSet: true };
        
        public Element SpawnElement() => new CombatPreventionElement();

        public bool IsMine(Element element) => element is CombatPreventionElement;
    }
}