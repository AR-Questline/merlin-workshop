using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.FightingStyles {
    [Serializable]
    public class AnimationAndBehaviourMappingEntry {
        [SerializeField, AnimancerAnimationsAssetReference] ShareableARAssetReference[] animations;
        [SerializeField] ARConditionalStateToAnimationMapping[] conditionalAnimations;
        [SerializeField, EnemyBehavioursAssetReference] ShareableARAssetReference[] combatBehaviours;

        public ShareableARAssetReference[] Animations => animations;
        public ShareableARAssetReference[] CombatBehaviours => combatBehaviours;
        public IEnumerable<ShareableARAssetReference> ConditionalAnimations(Item itemUsed) {
            return conditionalAnimations.SelectMany(ca => ca.MatchingAnimations(itemUsed.Template));
        }
        
        // === For Validation Purposes Only
        public Dictionary<int, List<ShareableARAssetReference>> GetAllAnimations() {
            List<ShareableARAssetReference> baseAnimations = new(animations);
            Dictionary<int, List<ShareableARAssetReference>> groupedAnimations = new();

            if (conditionalAnimations.Length <= 0) {
                groupedAnimations[0] = baseAnimations;
                return groupedAnimations;
            }

            foreach (var conditionalAnimation in conditionalAnimations) {
                Dictionary<int, List<ShareableARAssetReference>> groupedAnims = conditionalAnimation.EDITOR_AllAnimationsGroupedByConditions;
                foreach ((int hash, List<ShareableARAssetReference> anims) in groupedAnims) {
                    if (groupedAnimations.TryGetValue(hash, out List<ShareableARAssetReference> existingAnims)) {
                        existingAnims.AddRange(anims);
                    } else {
                        groupedAnimations[hash] = new List<ShareableARAssetReference>(baseAnimations.Concat(anims));
                    }
                }
            }

            return groupedAnimations;
        }
    }
}