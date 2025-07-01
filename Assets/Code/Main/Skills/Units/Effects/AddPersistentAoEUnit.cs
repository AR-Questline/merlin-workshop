using System;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.VisualScripts.Units;
using Awaken.TG.VisualScripts.Units.Typing;
using Awaken.Utility.Extensions;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class AddPersistentAoEUnit : ARUnit, ISkillUnit {
        [Flags]
        public enum AoeDamageType {
            Damage = 1 << 1,
            Status = 1 << 2,
            
            DamageAndStatus = Damage | Status
        }
        
        [Serialize, Inspectable, UnitHeaderInspectable]
        public AoeDamageType AoEKind { get; set; } = AoeDamageType.DamageAndStatus;
        
        RequiredValueInput<float> _inAmount;
        RequiredValueInput<TemplateWrapper<StatusTemplate>> _inStatusTemplate;
        InlineValueInput<float> _inPoiseDamage, _inForceDamage, _inRagdollForce, _inBuildupStrengthIfPossible;
        InlineValueInput<bool> _inInevitable;
        InlineValueInput<bool> _inRemoveExisting;
        InlineValueInput<DamageTypeData> _inDamageType;
        FallbackValueInput<SkillVariablesOverride> _inOverrides;
        
        protected override void Definition() {
            var inLocation = RequiredARValueInput<Location>("location");
            var inTick = OptionalARValueInput<float>("tick");
            var inDuration = RequiredARValueInput<IDuration>("duration");
            var inOnlyOnGrounded = InlineARValueInput("onlyOnGrounded", false);
            var inIsRemovingOther = InlineARValueInput("isRemovingOther", true);
            var inIsRemovable = InlineARValueInput("isRemovable", true);
            var inCanApplyToSelf = InlineARValueInput("canApplyToSelf", true);
            var inDiscardParentOnEnd = InlineARValueInput("discardParentOnEnd", true);
            var inDiscardOnOwnerDeath = InlineARValueInput("discardOnOwnerDeath", false);

            bool dealsDamage = AoEKind.HasFlagFast(AoeDamageType.Damage);
            bool appliesStatus = AoEKind.HasFlagFast(AoeDamageType.Status);
            
            if (appliesStatus) {
                _inStatusTemplate = RequiredARValueInput<TemplateWrapper<StatusTemplate>>("statusTemplate");
                _inBuildupStrengthIfPossible = InlineARValueInput<float>("buildupStrengthIfPossible", 0);
                _inOverrides = FallbackARValueInput<SkillVariablesOverride>("variables", _ => null);
            }
            
            if (dealsDamage) {
                _inAmount = RequiredARValueInput<float>("DamageAmount");
                _inPoiseDamage = InlineARValueInput("poiseDamage", 0f);
                _inForceDamage = InlineARValueInput("ForceDamage", 0f);
                _inRagdollForce = InlineARValueInput("RagdollForce", 0f);
                _inInevitable = InlineARValueInput("Inevitable", false);
                _inDamageType = InlineARValueInput("DamageType", new DamageTypeData(DamageType.PhysicalHitSource));
            }

            var aoeOutput = ValueOutput(typeof(PersistentAoE), "PersistentAoEElement");

            DefineSimpleAction("Enter", "Exit", flow => {
                Location location = inLocation.Value(flow);
                float? tick = inTick.HasValue ? inTick.Value(flow) : null;
                IDuration duration = inDuration.Value(flow);
                bool onlyOnGrounded = inOnlyOnGrounded.Value(flow);
                bool isRemovingOther = inIsRemovingOther.Value(flow);
                bool isRemovable = inIsRemovable.Value(flow);
                bool canApplyToSelf = inCanApplyToSelf.Value(flow);
                bool discardParentOnEnd = inDiscardParentOnEnd.Value(flow);
                bool discardOnOwnerDeath = inDiscardOnOwnerDeath.Value(flow);

                StatusTemplate statusTemplate = null;
                float buildupStrength = 0;
                SkillVariablesOverride overrides = null;
                if (appliesStatus) {
                    statusTemplate = _inStatusTemplate.Value(flow).Template;
                    buildupStrength = _inBuildupStrengthIfPossible.Value(flow);
                    overrides = _inOverrides.Value(flow);
                }

                SphereDamageParameters? sphereDamageParameters = null;
                if (dealsDamage) {
                    float damageAmount = _inAmount.Value(flow);
                    float poiseDamage = _inPoiseDamage.Value(flow);
                    float forceDamage = _inForceDamage.Value(flow);
                    float ragdollForce = _inRagdollForce.Value(flow);
                    bool inevitable = _inInevitable.Value(flow);
                    RuntimeDamageTypeData damageTypeData = _inDamageType.Value(flow)?.GetRuntimeData();
                    var parameters = DamageParameters.Default;
                    parameters.PoiseDamage = poiseDamage;
                    parameters.ForceDamage = forceDamage;
                    parameters.RagdollForce = ragdollForce;
                    parameters.Inevitable = inevitable;
                    parameters.DamageTypeData = damageTypeData;
                    parameters.IsDamageOverTime = true;
                    sphereDamageParameters = new SphereDamageParameters {
                        rawDamageData = new RawDamageData(damageAmount * tick.Value),
                        baseDamageParameters = parameters
                    };
                }

                PersistentAoE aoe = PersistentAoE.AddPersistentAoE(location, tick, duration, statusTemplate, buildupStrength, overrides,
                    sphereDamageParameters, onlyOnGrounded, isRemovingOther, isRemovable, canApplyToSelf,
                    discardParentOnEnd, discardOnOwnerDeath);
                
                flow.SetValue(aoeOutput, aoe);
            });
        }
    }
}