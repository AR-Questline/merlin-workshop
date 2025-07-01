using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.VisualGraphUtils;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Fights {
    [UnitCategory("AR/AI_Systems/Combat")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class DealClonedDamageUnit: ARUnit {
        public FallbackValueInput<Item> item;
        public ARValueInput<ICharacter> damageDealer;
        public ARValueInput<IAlive> characterHit;
        public ARValueInput<Damage> damageToClone;
        public OptionalValueInput<float?> damageMultiplier;
        public OptionalValueInput<float?> damageOverride;
        public ARValueInput<bool> isPrimary;
        public ARValueInput<bool> canBeCritical;

        protected override void Definition() {
            item = FallbackARValueInput<Item>("Item", _ => null);
            characterHit = InlineARValueInput<IAlive>("CharacterHit", null);
            damageDealer = InlineARValueInput<ICharacter>("DamageDealer", null);
            damageToClone = InlineARValueInput<Damage>("DamageToClone", null);
            damageMultiplier = OptionalARValueInput<float?>("DamageMultiplier");
            damageOverride = OptionalARValueInput<float?>("DamageOverride");
            isPrimary = InlineARValueInput<bool>("IsPrimary", false);
            canBeCritical = InlineARValueInput<bool>("CanBeCritical", false);
            DefineSimpleAction("Enter", "Exit", Enter);
        }
        
        void Enter(Flow flow) {
            Item it = item.Value(flow);
            IAlive aHit = characterHit.Value(flow);
            ICharacter att = damageDealer.Value(flow);
            Damage dmg = damageToClone.Value(flow);
            float? mult = damageMultiplier.HasValue ? damageMultiplier.Value(flow) : null;
            float? over = damageOverride.HasValue ? damageOverride.Value(flow) : null;
            bool isPrim = isPrimary.Value(flow);
            bool canCrit = canBeCritical.Value(flow);
            
            float amount = over ?? dmg.Amount * (mult ?? 1f);
            
            RuntimeDamageTypeData damageTypeData = new RuntimeDamageTypeData(dmg.Type, dmg.SubTypes);

            DamageParameters parameters = dmg.Parameters;
            parameters.DamageTypeData = damageTypeData;
            parameters.IsPrimary = isPrim;
            parameters.CanBeCritical = canCrit;
            parameters.Direction *= -1;
            parameters.ForceDirection *= -1;
            
            VGUtils.TryDoDamage(aHit, null, amount, att, ref parameters, it, statusDamageType: dmg.StatusDamageType);
        }
    }
}
