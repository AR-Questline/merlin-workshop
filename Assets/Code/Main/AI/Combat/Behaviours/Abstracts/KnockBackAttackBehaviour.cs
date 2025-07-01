using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.BehavioursHelpers;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.RichEnums;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.Abstracts {
    public abstract partial class KnockBackAttackBehaviour : AttackBehaviour<EnemyBaseClass> {
        protected const string BaseCastingGroup = "BaseCasting";
        
        [SerializeField] protected NpcStateType animatorStateType = NpcStateType.LongRange;
        [SerializeField, TemplateType(typeof(StatusTemplate))] TemplateReference onHitStatus;
        [SerializeField, ShowIf(nameof(ShowOnHitStatusBuildup))] int onHitStatusBuildup;
        [BoxGroup(BaseCastingGroup), SerializeField, RichEnumExtends(typeof(EquipmentSlotType)), HideIf(nameof(useAdditionalHand))] 
        RichEnumReference castFromSlot = EquipmentSlotType.MainHand;
        [BoxGroup(BaseCastingGroup), SerializeField] protected bool useAdditionalHand;
        [BoxGroup(BaseCastingGroup), SerializeField, ShowIf(nameof(useAdditionalHand))] protected AdditionalHand additionalHand;
        [BoxGroup(BaseCastingGroup), SerializeField] protected Vector3 castingPointOffset = Vector3.zero;
        
        [SerializeField] NpcDamageData damageData = NpcDamageData.DefaultAttackData;
        [SerializeField] bool customExplosionConeAngle;
        [SerializeField, ShowIf(nameof(customExplosionConeAngle))] float explosionConeAngle = 45;
        [SerializeField] bool disableFriendlyFire;
        [SerializeField] float explosionForceDamage = 5;
        [SerializeField] float explosionDuration = 0.5f;
        [SerializeField] float explosionRange = 9;
      	[SerializeField] float ragdollForce = 5;
  		[SerializeField, InfoBox("If set to nothing will use NpcHitMask from Template")] LayerMask explosionHitMask;
        [SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)]
        ShareableARAssetReference explosionVFX;
        [SerializeField] float explosionVFXDuration = PrefabPool.DefaultVFXLifeTime;
        [SerializeField, RichEnumExtends(typeof(BehaviourVfxRotation))] RichEnumReference vfxRotation = BehaviourVfxRotation.None;
        
        Location Location => ParentModel.ParentModel;
        BehaviourVfxRotation VfxRotation => vfxRotation.EnumAs<BehaviourVfxRotation>();
        bool ShowOnHitStatusBuildup => onHitStatus is { IsSet: true } && (onHitStatus.Get<StatusTemplate>()?.IsBuildupAble ?? false);

        
        protected void SpawnDamageSphere(Vector3? overridePosition = null) {
            Vector3 position;
            Transform hand = useAdditionalHand ? ParentModel.GetAdditionalHand(additionalHand) : null;
            if (overridePosition.HasValue) {
                position = overridePosition.Value;
            } else if (useAdditionalHand && hand != null) {
                position = hand.position;
            }else if (castFromSlot == EquipmentSlotType.MainHand) {
                position = Npc.MainHand.position;
            } else if (castFromSlot == EquipmentSlotType.OffHand) {
                position = Npc.OffHand.position;
            } else {
                position = ParentModel.Coords;
            }
            position += Npc.Rotation * castingPointOffset;
            
            LayerMask mask = explosionHitMask == 0 ? Npc.HitLayerMask : explosionHitMask;

            var parameters = DamageParameters.Default;
            parameters.ForceDamage = explosionForceDamage;
            parameters.RagdollForce = ragdollForce;
            parameters.DamageTypeData = damageData.GetDamageTypeData(Npc).GetRuntimeData();
            parameters.Inevitable = true;
            Quaternion damageRotation = VfxRotation.GetVfxRotation(ParentModel, position);

            SphereDamageParameters sphereDamageParameters = new() {
                rawDamageData = damageData.GetRawDamageData(Npc),
                duration = explosionDuration,
                endRadius = explosionRange,
                hitMask = mask,
                item = ParentModel.StatsItem,
                baseDamageParameters = parameters,
                onHitStatusTemplate = onHitStatus,
                onHitStatusBuildup = onHitStatusBuildup,
                disableFriendlyFire = disableFriendlyFire
            };
            
            if (customExplosionConeAngle) {
                ConeDamageParameters coneDamageParameters = new() {
                    angle = explosionConeAngle,
                    forward = damageRotation * Vector3.forward,
                    sphereDamageParameters = sphereDamageParameters
                };
                DealDamageInConeOverTime cone = new(coneDamageParameters, position, Npc);
                Location.AddElement(cone);   
            } else {
                DealDamageInSphereOverTime sphere = new(sphereDamageParameters, position, Npc);
                Location.AddElement(sphere);
            }
            
            // --- VFX
            position = Ground.SnapToGround(position);

            if (explosionVFX.IsSet) {
                PrefabPool.InstantiateAndReturn(explosionVFX, position, damageRotation, explosionVFXDuration).Forget();
            }
        }
    }
}
