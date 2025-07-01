using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Creates persistent AoE effect.")]
    public class PersistentAoEAttachment : MonoBehaviour, IAttachmentSpec {
        protected const string AdvancedGroupName = "Advanced";
        protected const string StatusGroupName = "Status";
        protected const string DamageGroupName = "Damage";
        
        [SerializeField] protected bool persistent;
        [SerializeField, HideIf(nameof(persistent))] protected float lifeTime = 20f;
        [SerializeField] protected bool onlyOnGrounded;
        [SerializeField, FoldoutGroup(AdvancedGroupName)] protected bool isRemovingOther = true;
        [SerializeField, FoldoutGroup(AdvancedGroupName)] protected bool isRemovable = true;
        [SerializeField, FoldoutGroup(AdvancedGroupName)] protected bool canApplyToSelf = true;
        [SerializeField, FoldoutGroup(AdvancedGroupName)] protected bool discardOnOwnerDeath;
        [SerializeField, FoldoutGroup(AdvancedGroupName)] protected bool discardParentOnEnd = true;
        [SerializeField, ShowIf(nameof(UsesTick))] protected float tickInterval = 0.5f;
        [SerializeField, FoldoutGroup(StatusGroupName), TemplateType(typeof(StatusTemplate))] TemplateReference statusTemplateRef;
        [SerializeField, FoldoutGroup(StatusGroupName), ShowIf(nameof(IsBuildupAble))] protected float buildupStrength;
        [SerializeField] protected bool dealsDamage;
        [SerializeField, FoldoutGroup(DamageGroupName), ShowIf(nameof(dealsDamage))] protected float damagePerSecond;
        [SerializeField, FoldoutGroup(DamageGroupName), ShowIf(nameof(dealsDamage))] protected float poiseDamage;
        [SerializeField, FoldoutGroup(DamageGroupName), ShowIf(nameof(dealsDamage))] protected float forceDamage;
        [SerializeField, FoldoutGroup(DamageGroupName), ShowIf(nameof(dealsDamage))] protected float ragdollForce;
        [SerializeField, FoldoutGroup(DamageGroupName), ShowIf(nameof(dealsDamage))] protected bool inevitable;
        [SerializeField, FoldoutGroup(DamageGroupName), ShowIf(nameof(dealsDamage))] protected DamageType damageType;

        protected StatusTemplate StatusTemplate => statusTemplateRef?.Get<StatusTemplate>();
        protected bool UsesTick => IsBuildupAble || dealsDamage;
        bool IsBuildupAble {
            get {
                if (statusTemplateRef == null || statusTemplateRef.GUID == string.Empty) {
                    return false;
                }
                return statusTemplateRef?.Get<StatusTemplate>()?.IsBuildupAble ?? false;
            }
        }

        public virtual Element SpawnElement() {
            float? tick = UsesTick ? tickInterval : null;
            IDuration duration = persistent ? new UntilDiscarded() : new TimeDuration(lifeTime);
            return new PersistentAoE(tick, duration, StatusTemplate, buildupStrength, null, GetDamageParameters(), onlyOnGrounded, isRemovingOther, isRemovable, canApplyToSelf, discardParentOnEnd, discardOnOwnerDeath);
        }

        public SphereDamageParameters? GetDamageParameters() {
            SphereDamageParameters? sphereDamageParameters = null;
            if (dealsDamage) {
                var parameters = DamageParameters.Default;
                parameters.PoiseDamage = poiseDamage;
                parameters.ForceDamage = forceDamage;
                parameters.RagdollForce = ragdollForce;
                parameters.Inevitable = inevitable;
                parameters.DamageTypeData = new RuntimeDamageTypeData(damageType);
                
                sphereDamageParameters = new SphereDamageParameters {
                    rawDamageData = new RawDamageData(damagePerSecond * tickInterval),
                    baseDamageParameters = parameters
                };
            }

            return sphereDamageParameters ?? null;
        }

        public virtual bool IsMine(Element element) {
            return element is PersistentAoE;
        }
    }
}