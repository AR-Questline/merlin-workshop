using System;
using System.Linq;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Templates;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.CustomDeath.Conditions {
    [Serializable]
    public class DamagingItemCustomDeathCondition : ICustomDeathAnimationConditions {
        [LabelText("Uses OR logic for each Abstract on the list")]
        [SerializeField, TemplateType(typeof(ItemTemplate))]
        TemplateReference[] itemAbstracts;

        public bool Check(DamageOutcome damageOutcome, bool isValidationCheck) {
            if (damageOutcome.Damage.Item is not { } item) {
                return false;
            }
            bool success = itemAbstracts.Any(abstractTemplateReference => item.Template.InheritsFrom(abstractTemplateReference.Get<ItemTemplate>()));
            if (!success) {
                return false;
            }
            return true;
        }
    }
}
