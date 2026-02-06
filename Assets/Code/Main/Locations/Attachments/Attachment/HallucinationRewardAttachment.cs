using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    public class HallucinationRewardAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField, TemplateType(typeof(StatusTemplate))] TemplateReference statusTemplateRef;
        [SerializeField, TemplateType(typeof(LocationTemplate))] TemplateReference sporesLocation;
        [SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)] ShareableARAssetReference sporesSpawnVfxReference;
        [SerializeField, TemplateType(typeof(LocationTemplate))] TemplateReference rewardLocation;
        [SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)] ShareableARAssetReference rewardSpawnVfxReference;
        [SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)] ShareableARAssetReference rewardHideVfxReference;
        [SerializeField] Vector2 rewardSpawnRadius = new Vector2(3f, 5f);
        
        public StatusTemplate StatusTemplate => statusTemplateRef.Get<StatusTemplate>();
        public LocationTemplate SporesLocation => sporesLocation.Get<LocationTemplate>();
        public ShareableARAssetReference SporesSpawnVfxReference => sporesSpawnVfxReference;
        public LocationTemplate RewardLocation => rewardLocation.Get<LocationTemplate>();
        public ShareableARAssetReference RewardSpawnVfxReference => rewardSpawnVfxReference;
        public ShareableARAssetReference RewardHideVfxReference => rewardHideVfxReference;
        public Vector2 RewardSpawnRadius => rewardSpawnRadius;
        
        public Element SpawnElement() => new HallucinationReward();
        public bool IsMine(Element element) => element is HallucinationReward;
    }
}