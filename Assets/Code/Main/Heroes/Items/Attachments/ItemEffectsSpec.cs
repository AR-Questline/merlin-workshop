using System.Collections.Generic;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    /// <summary>
    /// Holds group of skills under one item action.
    /// When given action is performed, all skills are activated.
    /// </summary>
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.Common, "Magic things, consumable effects, passive bonuses, etc.")]
    public class ItemEffectsSpec : MonoBehaviour, IItemEffectsSpec {
        [Tooltip("Defines when skill will be activated. It doesn't have any effect on Passive effects of skill.")]
        [SerializeField, RichEnumExtends(typeof(ItemActionType))]
        RichEnumReference actionType;
        [SerializeField, FoldoutGroup("Charge Settings"), EnableIf(nameof(IsCastSpell))] bool chargeAble;
        [SerializeField, FoldoutGroup("Charge Settings"), EnableIf(nameof(CanBeCharged))] bool hasChargeSteps;
        [SerializeField, FoldoutGroup("Charge Settings"), Range(1, 10), ShowIf(nameof(CanSetChargeSteps))] int maxChargeSteps = 1;
        [SerializeField, ShowIf(nameof(CanConsume))] bool consumeOnUse = true;

        [SerializeField]
        List<SkillReference> skills = new();

        public IEnumerable<SkillReference> Skills => skills;
        public ItemActionType ActionType => actionType.EnumAs<ItemActionType>();
        public bool CanBeCharged => IsCastSpell && chargeAble;
        public bool ConsumeOnUse => consumeOnUse && CanConsume();
        public int MaxChargeSteps => CanSetChargeSteps ? maxChargeSteps : 1;
        bool IsCastSpell => actionType?.EnumAs<ItemActionType>() == ItemActionType.CastSpell;
        bool CanSetChargeSteps => IsCastSpell && hasChargeSteps;
        bool CanConsume() => actionType?.EnumAs<ItemActionType>() != ItemActionType.CastSpell;

        public Element SpawnElement() {
            return new ItemEffects();
        }

        public bool IsMine(Element element) {
            return element is ItemEffects;
        }
    }
}