using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Maths;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.ARAnimator {
    [CreateAssetMenu(menuName = "TG/Conditional StateToAnimationMapping", order = 0)]
    public class ARConditionalStateToAnimationMapping : ScriptableObject {
        [SerializeField] ConditionalStateToAnimationEntry[] conditionalAnimations = Array.Empty<ConditionalStateToAnimationEntry>();
        
        public IEnumerable<ShareableARAssetReference> MatchingAnimations(ItemTemplate itemTemplate) {
            return conditionalAnimations.Where(ca => ca.FulfillsConditions(itemTemplate)).Select(ca => ca.Animations);
        }
        
        // === For Validation Purposes Only
        public Dictionary<int, List<ShareableARAssetReference>> EDITOR_AllAnimationsGroupedByConditions {
            get {
                Dictionary<int, List<ShareableARAssetReference>> groupedAnimations = new();
                foreach (var conditionalAnimation in conditionalAnimations) {
                    int hash = conditionalAnimation.GetHash();
                    if (groupedAnimations.TryGetValue(hash, out var anims)) {
                        anims.Add(conditionalAnimation.Animations);
                    } else {
                        groupedAnimations[hash] = new List<ShareableARAssetReference> { conditionalAnimation.Animations };
                    }
                }
                
                return groupedAnimations;
            }
        }

        // === Helpers
        [Serializable]
        struct ConditionalStateToAnimationEntry {
            [SerializeField, TemplateType(typeof(ItemTemplate))] TemplateReference[] itemConditions;
            [SerializeField, AnimancerAnimationsAssetReference] ShareableARAssetReference animations;
        
            public ShareableARAssetReference Animations => animations;
            // --- This is not null when Serializable :(
            ItemTemplate[] _itemConditions;
            ItemTemplate[] ItemConditions {
                get {
                    if (_itemConditions == null || _itemConditions.Length != itemConditions.Length) {
                        _itemConditions = itemConditions.Select(i => i.Get<ItemTemplate>()).ToArray();
                    }
                    return _itemConditions;
                }
            }
            
            public bool FulfillsConditions(ItemTemplate itemTemplate) {
                return ItemConditions.All(itemTemplate.InheritsFrom);
            }

            public int GetHash() {
                int hash = 0;
                foreach (TemplateReference templateReference in itemConditions.OrderBy(i => i.GUID)) {
                    hash = DHash.Combine(hash, templateReference.GUID.GetHashCode(StringComparison.InvariantCultureIgnoreCase));
                }
                return hash;
            }
        }
    }
}