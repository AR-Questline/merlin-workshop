using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.VisualScripts.Units.Fights {
    [UnitCategory("AR/AI_Systems/Combat")]
    [TypeIcon(typeof(FlowGraph))]
    public class DealDamageInSphereOverTimeUnit : ARUnit {
        ControlInput _enter;
        ControlOutput _exit;

        public ValueInput attackOwner, amount, poiseDamage, forceDamage, ragdollForce, duration, endRadius, inevitable, type, damageSubType, origin, layerMask, canBeCritical, critical, ignoreArmor, armorPenetration, isPrimary, isDamageOverTime;
        public FallbackValueInput<Item> item;
        public OptionalValueInput<DamageTypeData> customDamageTypeData;
        public OptionalValueInput<float?> overridenRandomnessModifier;
        
        protected override void Definition() {
            _enter = ControlInput("Enter", Enter);
            _exit = ControlOutput("Exit");

            attackOwner = ValueInput<IAlive>("Owner");
            amount = ValueInput<float>("DamageAmount");
            poiseDamage = ValueInput<float>("PoiseDamage", 0);
            forceDamage = ValueInput("ForceDamage", 0f);
            ragdollForce = ValueInput("RagdollForce", 0f);
            item = FallbackARValueInput<Item>("ItemDealingDamage", _ => null);
            
            // Sphere Damage Parameters
            duration = ValueInput("Duration", 0f);
            endRadius = ValueInput("EndRadius", 0f);
            origin = ValueInput("Origin", Vector3.zero);
            layerMask = ValueInput("LayerMask", ~0);
            
            // Optional
            overridenRandomnessModifier = OptionalARValueInput<float?>("OverridenRandomnessModifier");

            // Damage Type Parameters
            canBeCritical = ValueInput<bool>("CanBeCritical", true);
            critical = ValueInput<bool>("IsCritical", false);
            ignoreArmor = ValueInput<bool>("IgnoreArmor", false);
            armorPenetration = ValueInput<float>("BasicArmorPenetration", 0f);
            inevitable = ValueInput("Inevitable", true);
            isPrimary = ValueInput<bool>("IsPrimary", true);
            isDamageOverTime = ValueInput<bool>("IsDamageOverTime", false);
            
            type = ValueInput("DamageType", DamageType.PhysicalHitSource);
            damageSubType = ValueInput("DamageSubType", DamageSubType.GenericPhysical);
            customDamageTypeData = OptionalARValueInput<DamageTypeData>("CustomDamageTypeData");
            
            Requirement(attackOwner, _enter);
            Requirement(amount, _enter);
            Succession(_enter, _exit);
        }

        protected virtual ControlOutput Enter(Flow flow) {
            IAlive owner = flow.GetValue<IAlive>(attackOwner);
            Vector3 startPoint = flow.GetValue<Vector3>(origin);
            float? overridenRandomnessMod = overridenRandomnessModifier.HasValue ? overridenRandomnessModifier.Value(flow) : null;
            
            RuntimeDamageTypeData damageTypeData = customDamageTypeData.HasValue ? customDamageTypeData.Value(flow).GetRuntimeData() : 
                new RuntimeDamageTypeData(flow.GetValue<DamageType>(type), flow.GetValue<DamageSubType>(damageSubType));
            
            DamageParameters parameters = DamageParameters.Default;
            parameters.PoiseDamage = flow.GetValue<float>(poiseDamage);
            parameters.ForceDamage =  flow.GetValue<float>(forceDamage);
            parameters.RagdollForce =  flow.GetValue<float>(ragdollForce);
            parameters.DamageTypeData = damageTypeData;
            parameters.CanBeCritical = flow.GetValue<bool>(canBeCritical);
            parameters.Critical = flow.GetValue<bool>(critical);
            parameters.IgnoreArmor = flow.GetValue<bool>(ignoreArmor);
            parameters.Inevitable = flow.GetValue<bool>(inevitable);
            parameters.IsPrimary = flow.GetValue<bool>(isPrimary);
            parameters.IsDamageOverTime = flow.GetValue<bool>(isDamageOverTime);
            parameters.ArmorPenetration = flow.GetValue<float>(armorPenetration);
            
            var explosionParams = new SphereDamageParameters {
                rawDamageData = new RawDamageData(flow.GetValue<float>(amount)),
                duration = flow.GetValue<float>(duration),
                endRadius = flow.GetValue<float>(endRadius),
                hitMask = flow.GetValue<LayerMask>(layerMask),
                item = item.Value(flow),
                overridenRandomnessModifier = overridenRandomnessMod,
                baseDamageParameters = parameters
            };

            if (explosionParams.duration <= 0f) {
                DamageUtils.DealDamageInSphereInstantaneous(owner, explosionParams, startPoint);
            } else {
                owner.AddElement(new DealDamageInSphereOverTime(explosionParams, startPoint));
            }

            return _exit;
        }
    }
}