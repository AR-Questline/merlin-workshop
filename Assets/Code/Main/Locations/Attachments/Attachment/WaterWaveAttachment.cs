using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Utils;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Spawns water wave dealing aoe damage and being blocked by rocks.")]
    public class WaterWaveAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] NpcDamageData damageData = NpcDamageData.DefaultMagicAttackData;
        [SerializeField] SphereDamageParameters sphereDamageParameters;
        public float damageAngle = 360f;
        [SerializeField] KnockdownType knockdownType;
        [SerializeField] float knockdownStrength;
        [SerializeField] float explosionForceDamage = 100;
        [SerializeField] float ragdollForce = 100;
        
        [SerializeField] LocationReference blockerLocationsRef;
        public float blockerRadius = 1f;
        public float blockerDistance = 3f;
        [ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)] public ShareableARAssetReference waveVFX;
        [ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)] public ShareableARAssetReference waveHittingBlockerVFX;
        public float waveVFXLifetime = 10f;
        public int waveMaskTextureHalfSizeInMeters = 16;

        public float cycleInitialDelay = 5f;
        public float cycleDuration = 10f;
        
        public Element SpawnElement() {
            return new WaterWave();
        }

        public bool IsMine(Element element) {
            return element is WaterWave;
        }
        
        public SphereDamageParameters GetParameters() {
            var parameters = DamageParameters.Default;
            parameters.KnockdownType = knockdownType;
            parameters.KnockdownStrength = knockdownStrength;
            parameters.ForceDamage = explosionForceDamage;
            parameters.RagdollForce = ragdollForce;
            parameters.DamageTypeData = damageData.GetDamageTypeData(null).GetRuntimeData();
            parameters.Inevitable = true;
            
            var newSphereDamageParameters = sphereDamageParameters;
            newSphereDamageParameters.baseDamageParameters = parameters;
            newSphereDamageParameters.rawDamageData = damageData.GetRawDamageData(null);

            return newSphereDamageParameters;
        }
        
        public void GetBlockerLocations(ref StructList<WeakModelRef<Location>> blockers) {
            blockers = new StructList<WeakModelRef<Location>>(1);
            foreach (var location in blockerLocationsRef.MatchingLocations(null)) {
                blockers.Add(location);
            }
        }
    }
}