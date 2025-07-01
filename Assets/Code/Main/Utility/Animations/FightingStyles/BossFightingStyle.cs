using System;
using System.Linq;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Attachments.Bosses;
using Awaken.TG.Main.AI.Combat.Attachments.Humanoids;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.FightingStyles {
    [Serializable]
    public class BossFightingStyle : NpcFightingStyle {
        [SerializeField] AnimationAndBehaviourMappingEntry[] phases = Array.Empty<AnimationAndBehaviourMappingEntry>();
        
        public override AnimationAndBehaviourMappingEntry RetrieveCombatData(EnemyBaseClass enemyBaseClass) {
            if (enemyBaseClass is BaseBossCombat bossCombat) {
                int currentPhase = bossCombat.CurrentPhase;
                if (currentPhase >= phases.Length) {
                    currentPhase = phases.Length - 1;
                }

                return phases[currentPhase];
            }
            return phases.FirstOrDefault();
        }

        public override AnimationAndBehaviourMappingEntry RetrieveCombatData(FighterType fighterType) {
            return phases.FirstOrDefault();
        }

#if UNITY_EDITOR
        protected override void ValidateAdditionalAssets(ref int errorsCount) {
            foreach (var phase in phases) {
                ValidateAsset(phase, ref errorsCount);
            }
        }
#endif
    }
}