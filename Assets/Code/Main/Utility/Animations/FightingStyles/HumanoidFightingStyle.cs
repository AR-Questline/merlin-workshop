using System;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Attachments.Humanoids;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.FightingStyles {
    [Serializable]
    public class HumanoidFightingStyle : NpcFightingStyle {
        [SerializeField] AnimationAndBehaviourMappingEntry fists, oneHanded, daggerOneHanded, ranged, twoHanded, dualWielding, heavyDualWielding;

        public override AnimationAndBehaviourMappingEntry RetrieveCombatData(EnemyBaseClass enemyBaseClass) {
            return fists;
        }

        public override AnimationAndBehaviourMappingEntry RetrieveCombatData(FighterType fighterType) {
            if (fighterType == FighterType.OneHanded) {
                return oneHanded;
            }

            if (fighterType == FighterType.OneHandedDagger) {
                if (daggerOneHanded == null || (daggerOneHanded.Animations.Length <= 0 && daggerOneHanded.CombatBehaviours.Length <= 0)) {
                    return oneHanded;
                }
                return daggerOneHanded;
            }

            if (fighterType == FighterType.TwoHanded) {
                return twoHanded;
            }

            if (fighterType == FighterType.Fists) {
                return fists;
            }

            if (fighterType == FighterType.Ranged) {
                return ranged;
            }

            if (fighterType == FighterType.DualWielding) {
                return dualWielding;
            }

            if (fighterType == FighterType.HeavyDualWielding) {
                return heavyDualWielding ?? dualWielding;
            }

            return null;
        }
        
#if UNITY_EDITOR
        protected override void ValidateAdditionalAssets(ref int errorsCount) {
            ValidateAsset(oneHanded, ref errorsCount);
            ValidateAsset(daggerOneHanded, ref errorsCount);
            ValidateAsset(twoHanded, ref errorsCount);
            ValidateAsset(fists, ref errorsCount);
            ValidateAsset(ranged, ref errorsCount);
        }
#endif
    }
}