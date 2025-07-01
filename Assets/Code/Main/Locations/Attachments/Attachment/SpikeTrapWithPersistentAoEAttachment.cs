using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    public class SpikeTrapWithPersistentAoEAttachment : PersistentAoEAttachment {
        [SerializeField] float maxDistanceToHeroToSpawnVFX = 50f;
        [SerializeField] bool hasToBeActivated;
        [SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)] ShareableARAssetReference vfx;
        [SerializeField] Transform vfxSpawnPoint;
        [SerializeField] BoxCollider damageCollider;
        
        public float MaxDistanceToHeroToSpawnVFX => maxDistanceToHeroToSpawnVFX;
        public bool HasToBeActivated => hasToBeActivated;
        public ShareableARAssetReference Vfx => vfx;
        public Transform VfxSpawnPoint => vfxSpawnPoint;
        public BoxCollider DamageCollider => damageCollider;
        
        public override Element SpawnElement() {
            float? tick = UsesTick ? tickInterval : null;
            IDuration duration = persistent ? new UntilDiscarded() : new TimeDuration(lifeTime);
            return new SpikeTrapPersistentAoE(this, tick, duration, StatusTemplate, buildupStrength, 
                null, GetDamageParameters(), onlyOnGrounded, isRemovingOther, isRemovable, canApplyToSelf, discardParentOnEnd, discardOnOwnerDeath);
        }
        
        public override bool IsMine(Element element) {
            return element is SpikeTrapPersistentAoE;
        }
    }
}
