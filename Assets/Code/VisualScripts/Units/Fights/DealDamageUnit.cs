using System;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.VisualGraphUtils;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.VisualScripts.Units.Fights {
    [UnitCategory("AR/AI_Systems/Combat")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class DealDamageUnit: ARUnit {
        [DoNotSerialize] public ValueInput amount,
            attacker,
            forceDamage,
            ragdollForce,
            poiseDamage,
            canBeCritical,
            critical,
            ignoreArmor,
            inevitable,
            isPrimary,
            isDamageOverTime,
            armorPenetration,
            radius;
        public FallbackValueInput<Item> item;
        public ARValueInput<StatusDamageType> statusDamageType;
        public FallbackValueInput<IAlive> characterHit;
        public FallbackValueInput<Collider> colliderHit;
        public FallbackValueInput<Vector3?> dmgPosition, direction;
        public ARValueInput<DamageType> damageType;
        public ARValueInput<DamageSubType> damageSubType;
        public OptionalValueInput<DamageTypeData> customDamageTypeData;
        public OptionalValueInput<Vector3> dealerPosition;
        public OptionalValueInput<float?> overridenRandomnessModifier;
        public FallbackValueInput<ICharacter> damageDealer;

        protected override void Definition() {
            characterHit = FallbackARValueInput<IAlive>("CharacterHit", _ => null);
            colliderHit = FallbackARValueInput<Collider>("ColliderHit", _ => null);
            amount = ValueInput<float>("DamageAmount");
            attacker = ValueInput<GameObject>("Attacker");
            damageDealer = FallbackARValueInput("DamageDealer", flow => VGUtils.TryGetModel<ICharacter>(flow.GetValue<GameObject>(attacker)));
            attacker.NullMeansSelf();
            poiseDamage = ValueInput<float>("PoiseDamage", 0);
            forceDamage = ValueInput<float>("ForceMagnitude", 0);
            ragdollForce = ValueInput<float>("RagdollForce", 0);
            radius = ValueInput<float>("Radius", 0);
            item = FallbackARValueInput<Item>("Item", _ => null);
            statusDamageType = InlineARValueInput<StatusDamageType>("StatusDamageType", StatusDamageType.Default);
            
            // Optional 
            dmgPosition = FallbackARValueInput<Vector3?>("DamagePosition", _ => null);
            direction = FallbackARValueInput<Vector3?>("DamageDirection", _ => null);
            dealerPosition = OptionalARValueInput<Vector3>(nameof(dealerPosition));
            overridenRandomnessModifier = OptionalARValueInput<float?>(nameof(overridenRandomnessModifier));
            
            // Damage Type Parameters
            canBeCritical = ValueInput<bool>("CanBeCritical", true);
            critical = ValueInput<bool>("IsCritical", false);
            ignoreArmor = ValueInput<bool>("IgnoreArmor", false);
            armorPenetration = ValueInput<float>("BasicArmorPenetration", 0f);
            inevitable = ValueInput<bool>("Inevitable", false);
            isPrimary = ValueInput<bool>("IsPrimary", true);
            isDamageOverTime = ValueInput<bool>("IsDamageOverTime", false);

            damageType = InlineARValueInput(nameof(damageType), DamageType.None);
            damageSubType = InlineARValueInput(nameof(damageSubType), DamageSubType.GenericPhysical);
            customDamageTypeData = OptionalARValueInput<DamageTypeData>(nameof(customDamageTypeData));

            var (enter, _) = DefineSimpleAction("Enter", "Exit", Enter);

            Requirement(amount, enter);
        }
        
        void Enter(Flow flow) {
            IAlive aHit = characterHit.Value(flow);
            Collider cHit = colliderHit.Value(flow);
            float amnt = flow.GetValue<float>(amount);
            ICharacter att = damageDealer.Value(flow);
            float poise = flow.GetValue<float>(poiseDamage);
            float fMag = flow.GetValue<float>(forceDamage);
            float rForce = flow.GetValue<float>(ragdollForce);
            float radi = flow.GetValue<float>(radius);
            Item it = item.Value(flow);
            StatusDamageType sdt = statusDamageType.Value(flow);
            
            Vector3? pos = dmgPosition.Value(flow);
            Vector3? dir = direction.Value(flow);

            bool canCrit = flow.GetValue<bool>(canBeCritical);
            bool crit = flow.GetValue<bool>(critical);
            bool ignArm = flow.GetValue<bool>(ignoreArmor);
            bool inEvit = flow.GetValue<bool>(inevitable);
            bool isPrim = flow.GetValue<bool>(isPrimary);
            bool isDoT = flow.GetValue<bool>(isDamageOverTime);
            float armPen = flow.GetValue<float>(armorPenetration);

            RuntimeDamageTypeData damageTypeData = customDamageTypeData.HasValue ? customDamageTypeData.Value(flow).GetRuntimeData() : 
                new RuntimeDamageTypeData(damageType.Value(flow), damageSubType.Value(flow));

            DamageParameters parameters = DamageParameters.Default;
            parameters.PoiseDamage = poise;
            parameters.ForceDamage = fMag;
            parameters.RagdollForce = rForce;
            parameters.Radius = radi;
            parameters.Position = pos;
            parameters.ForceDirection = dir;
            parameters.Direction = dir;
            parameters.DamageTypeData = damageTypeData;
            parameters.CanBeCritical = canCrit;
            parameters.Critical = crit;
            parameters.IgnoreArmor = ignArm;
            parameters.Inevitable = inEvit;
            parameters.IsPrimary = isPrim;
            parameters.IsDamageOverTime = isDoT;
            parameters.ArmorPenetration = armPen;
            parameters.DealerPosition = dealerPosition.HasValue ? dealerPosition.Value(flow) : null;
            
            float? overridenRandomnessMod = overridenRandomnessModifier.HasValue ? overridenRandomnessModifier.Value(flow) : null;
            
            VGUtils.TryDoDamage(aHit, cHit, amnt, att, ref parameters, it, statusDamageType: sdt, overridenRandomnessModifier: overridenRandomnessMod);
        }
    }
}
