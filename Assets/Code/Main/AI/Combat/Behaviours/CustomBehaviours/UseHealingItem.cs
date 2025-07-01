using System;
using Awaken.TG.Main.Saving;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.CustomBehaviours {
    [Serializable]
    public partial class UseHealingItem : UseItemBehaviour {
        [SerializeField, Range(0, 1), FoldoutGroup("Invoke Conditions")] float invokeWhenHpBelowPercent = 0.5f;
        [SerializeField, FoldoutGroup("Effect"), HideIf(nameof(UseSkill))] HealType healType = HealType.FlatValue;
        [SerializeField, FoldoutGroup("Effect"), HideIf(nameof(UseSkill)), HideIf(nameof(HealPercent))]
        float healHpFlat = 50;
        [SerializeField, FoldoutGroup("Effect"), HideIf(nameof(UseSkill)), HideIf(nameof(HealFlat)), Range(0.01f, 1f)]
        float healHpPercentOfMax = 0.25f;

        protected override bool CanBeUsed => ParentModel.NpcElement.Health.Percentage <= invokeWhenHpBelowPercent;
        public override bool IsPeaceful => true;

        protected override void UseItem() {
            if (UseSkill) {
                base.UseItem();
                return;
            }

            if (HealPercent) {
                float healValue = ParentModel.NpcElement.MaxHealth * healHpPercentOfMax;
                ParentModel.NpcElement.Health.IncreaseBy(healValue);
            } else {
                ParentModel.NpcElement.Health.IncreaseBy(healHpFlat);
            }
            SpawnVFX();
        }
        
        // === Helpers
        bool HealFlat => healType == HealType.FlatValue;
        bool HealPercent => healType == HealType.PercentOfMax;

        enum HealType : byte {
            FlatValue = 0,
            PercentOfMax = 1
        }
    }
}