using System;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Attachments.Humanoids;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.FightingStyles {
    [Serializable]
    public class CustomFightingStyle : NpcFightingStyle {
        [SerializeField] AnimationAndBehaviourMappingEntry customs;
        
        public override AnimationAndBehaviourMappingEntry RetrieveCombatData(EnemyBaseClass enemyBaseClass) {
            return customs;
        }

        public override AnimationAndBehaviourMappingEntry RetrieveCombatData(FighterType fighterType) {
            return customs;
        }

#if UNITY_EDITOR
        protected override void ValidateBaseAssets(ref int errorsCount) {
            // --- Ignore
        }
        
        protected override void ValidateAdditionalAssets(ref int errorsCount) {
            ValidateAsset(customs, ref errorsCount, BaseBehaviours);
        }
#endif
    }
}