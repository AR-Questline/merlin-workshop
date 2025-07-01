using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Deals damage around and spawns VFX on damage taken.")]
    public class ExplodeOnDamageTakenAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] bool killOnExplosion;
        [SerializeField] bool discardElementOnExplosion;
        [SerializeField] float delayExplosion;
        [SerializeField] DamageParametersData damageParametersData;
        [SerializeField] LayerMask explosionHitMask;
        [SerializeField] float explosionDamage = 5;
        [SerializeField] float explosionDuration = 0.5f;
        [SerializeField] float explosionRange = 9;
        [SerializeField, ARAssetReferenceSettings(new[] {typeof(GameObject)}, true)]
        ShareableARAssetReference explodeVFXReference;

        public bool KillOnExplosion => killOnExplosion;
        public bool DiscardElementOnExplosion => discardElementOnExplosion;
        public float DelayExplosion => delayExplosion;
        public SphereDamageParameters SphereDamageParams => new SphereDamageParameters {
            rawDamageData = new RawDamageData(explosionDamage),
            duration = explosionDuration,
            endRadius = explosionRange,
            hitMask = explosionHitMask,
            baseDamageParameters = damageParametersData.Get()
        };
        public ShareableARAssetReference ExplodeVFXReference => explodeVFXReference;

        public Element SpawnElement() => new ExplodeOnDamageTaken();
        public bool IsMine(Element element) => element is ExplodeOnDamageTaken;
    }
}