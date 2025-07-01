using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using System.Collections.Generic;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.Utility.Collections;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;
using XNode;

namespace Awaken.TG.Main.Stories.Steps {
    /// <summary>
    /// This step type is used to represent requirements to take a choice or the probability of success depending on requirement
    /// </summary>
    [Element("Requirement: Stat")]
    public class SEditorStatDependantChoice : EditorStep {
        const string SuccessPortName = "Success";

        public SStatDependantChoice.RequirementType requirementType;

        [LabelText("Stat")] [RichEnumExtends(typeof(StatType))] [Tooltip("The stat to check if this choice should be shown")]
        public RichEnumReference affectedStat = AliveStatType.Health;

        [ShowIf(nameof(IsFlat)), LabelText("Required stat level"), Tooltip("Required value of stat for the option to be visible")]
        public float statValue = 0;

        [ShowIf(nameof(IsFlat)), RichEnumExtends(typeof(ComparisonOperator))]
        public RichEnumReference comparison;
        
        [ShowIf(nameof(IsChance)), LabelText("100% chance at stat level"), Tooltip("Stat value at which the chance of success is 100%")]
        public float certainSuccessAtValue;
        
        [ShowIf(nameof(IsChance)), LabelText("Is requirement visible"), Tooltip("Should requirement be visible to player in dialogue option or should it be hidden")] 
        public bool isVisibleToPlayer = true;
        
        [ShowIf(nameof(IsChanceAndVisible)), LabelText("Is chance value visible"), Tooltip("Should exact percentage value be visible to player or should it be hidden by '?' sign.")]
        public bool isChanceVisible = true;
        
        [LabelText("Override Stat Label"), Tooltip("Override the stat label with a custom one, for example:" +
                                                   "\n\"[Lie]\": 'Perception: 50% ' -> ' [Lie]: 50% '" +
                                                   "\n\"Str\": 'Strength: 7 or more' -> 'Str: 7 or more'")]
        [LocStringCategory(Category.Dialogue)]
        public LocString overrideLabel;
        
        // === Serialized properties
        public ChapterEditorNode SuccessChapter => ConnectedNode(SuccessPortName) as ChapterEditorNode;
        public NodePort SuccessPort => PrivatePort(SuccessPortName, direction: NodePort.IO.Output);
        
        // === Properties
        public StatType AffectedStat => affectedStat.Enum as StatType;
        public ComparisonOperator Comparison => comparison.EnumAs<ComparisonOperator>();
        public bool IsFlat => requirementType == SStatDependantChoice.RequirementType.Flat;
        public bool IsChanceAndVisible => IsChance && isVisibleToPlayer;
        bool IsChance => requirementType == SStatDependantChoice.RequirementType.Chance;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SStatDependantChoice {
                requirementType = requirementType,
                affectedStat = affectedStat,
                statValue = statValue,
                comparison = comparison,
                certainSuccessAtValue = certainSuccessAtValue,
                isVisibleToPlayer = isVisibleToPlayer,
                isChanceVisible = isChanceVisible,
                overrideLabel = overrideLabel,
                successChapter = parser.GetChapter(SuccessChapter),
            };
        }
    }

    public partial class SStatDependantChoice : StoryStep {
        const string SuccessPortName = "Success";

        public RequirementType requirementType;
        public RichEnumReference affectedStat = AliveStatType.Health;
        public float statValue = 0;
        public RichEnumReference comparison;
        public float certainSuccessAtValue;
        public bool isVisibleToPlayer = true;
        public bool isChanceVisible = true;
        public LocString overrideLabel;
        public StoryChapter successChapter;
        
        public StatType AffectedStat => affectedStat.Enum as StatType;
        public ComparisonOperator Comparison => comparison.EnumAs<ComparisonOperator>();
        public bool IsFlat => requirementType == RequirementType.Flat;
        
        public override StepResult Execute(Story story) {
            if (!IsFlat) {
                float chance = ChanceAtAffectedStat(story);
                if (RandomUtil.WithProbability(chance)) {
                    story.JumpTo(successChapter);
                }
            }
            return StepResult.Immediate;
        }

        public override void AppendKnownEffects(Story story, ref StructList<string> effects) {
            base.AppendKnownEffects(story, ref effects);

            if (!IsFlat && !isVisibleToPlayer) {
                return;
            }
            string requirementText = overrideLabel.ToString().IsNullOrWhitespace() ? AffectedStat.DisplayName : overrideLabel;
            if (IsFlat) {
                requirementText += $": {statValue} {Comparison.DisplayName}";
            } else {
                if (isChanceVisible) {
                    var chance = Mathf.CeilToInt(ChanceAtAffectedStat(story) * 100);
                    var clampedChance = Mathf.Clamp(chance, 0, 100);
                    requirementText += $": {clampedChance}%";
                } else {
                    requirementText += ": ?%";
                }
            }
            effects.Add(requirementText);
        }
        
        float ChanceAtAffectedStat(Story story) => ExtractStat(AffectedStat, story) / certainSuccessAtValue;
        
        static float ExtractStat(StatType affectedStat, Story story) {
            IWithStats subject = StoryRole.DefaultForStat(affectedStat).RetrieveFrom<IWithStats>(story);
            Stat extractStat = subject.Stat(affectedStat);

            float result = extractStat;
            return result;
        }
        
        public override StepRequirement GetRequirement() {
            return (api) => !IsFlat || Comparison.Compare(ExtractStat(AffectedStat, api), statValue);
        }
        
        public override string GetKind(Story story) {
            return "Story";
        }
        
        public enum RequirementType : byte {
            Flat,
            Chance
        }
    }
}