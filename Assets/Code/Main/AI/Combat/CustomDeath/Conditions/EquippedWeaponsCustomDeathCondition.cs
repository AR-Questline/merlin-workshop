using System;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Templates;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.CustomDeath.Conditions {
    [Serializable]
    public class EquippedWeaponsCustomDeathCondition : ICustomDeathAnimationConditions {
        [SerializeField] 
        bool requireSpecificMainHand;
        [LabelText("Uses OR logic for each Abstract on the list")]
        [SerializeField, ShowIf(nameof(requireSpecificMainHand)), TemplateType(typeof(ItemTemplate))]
        TemplateReference[] mainHandAbstracts;
        [SerializeField] 
        bool requireSpecificOffHand;
        [LabelText("Uses OR logic for each Abstract on the list")]
        [SerializeField, ShowIf(nameof(requireSpecificOffHand)), TemplateType(typeof(ItemTemplate))]
        TemplateReference[] offHandAbstracts;

        public bool Check(DamageOutcome damageOutcome, bool isValidationCheck) {
            var inventory = damageOutcome.Attacker.Inventory;
            var mainHandItem = inventory.EquippedItem(EquipmentSlotType.MainHand);
            var offHandItem = inventory.EquippedItem(EquipmentSlotType.OffHand);
            if (requireSpecificMainHand) {
                if (mainHandItem == null || !mainHandAbstracts.Any(abstractTemplateReference => mainHandItem.Template.InheritsFrom(abstractTemplateReference.Get<ItemTemplate>()))) {
                    return false;
                }
            }
            if (requireSpecificOffHand) {
                if (offHandItem == null || !offHandAbstracts.Any(abstractTemplateReference => offHandItem.Template.InheritsFrom(abstractTemplateReference.Get<ItemTemplate>()))) {
                    return false;
                }
            }
            return true;
        }
    }
}
