using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Stories.Conditions {
    /// <summary>
    /// Check if hero has specific reputation kind for faction
    /// </summary>
    [Element("Hero: Has reputation")]
    public class CEditorHasReputation : EditorCondition {
        [TemplateType(typeof(FactionTemplate))]
        public TemplateReference factionRef;
        
        [Space]
        [Range(0,3)] public int infamyIndex;
        [HideLabel] public ComparisonType infamyComparison;
        [Space]
        [Range(0,3)] public int fameIndex;
        [HideLabel] public ComparisonType fameComparison;

        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CHasReputation {
                factionRef = factionRef,
                infamyIndex = infamyIndex,
                infamyComparison = infamyComparison,
                fameIndex = fameIndex,
                fameComparison = fameComparison
            };
        }
    }

    public partial class CHasReputation : StoryCondition {
        public TemplateReference factionRef;
        public int infamyIndex;
        public ComparisonType infamyComparison;
        public int fameIndex;
        public ComparisonType fameComparison;

        public override bool Fulfilled(Story story, StoryStep step) {
            if (factionRef is not {IsSet: true}) {
                Log.Important?.Error($"Missing faction reference in {nameof(CHasReputation)} condition block");
                return false;
            }

            (int currentInfamyIndex, int currentFameIndex) = OwnerReputationUtil.CurrentReputations(factionRef.GUID);
            bool infamyFulfilled = CompareReputationIndex(currentInfamyIndex, infamyIndex, infamyComparison);
            bool fameFulfilled = CompareReputationIndex(currentFameIndex, fameIndex, fameComparison);
            return infamyFulfilled && fameFulfilled;
        }
        
        static bool CompareReputationIndex(int actualReputationIndex, int desiredReputationIndex, ComparisonType comparisonType) {
            return comparisonType switch {
                ComparisonType.Equal => actualReputationIndex == desiredReputationIndex,
                ComparisonType.NotEqual => actualReputationIndex != desiredReputationIndex,
                ComparisonType.Greater => actualReputationIndex > desiredReputationIndex,
                ComparisonType.GreaterOrEqual => actualReputationIndex >= desiredReputationIndex,
                ComparisonType.Less => actualReputationIndex < desiredReputationIndex,
                ComparisonType.LessOrEqual => actualReputationIndex <= desiredReputationIndex,
                _ => false
            };
        }
    }
    
    public enum ComparisonType : byte {
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual
    }
}