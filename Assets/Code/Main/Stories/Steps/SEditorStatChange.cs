using System;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats.StatConfig;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using Vendor.xNode.Scripts.Attributes;
using ValueType = Awaken.TG.Main.Utility.ValueType;

namespace Awaken.TG.Main.Stories.Steps {
    /// <summary>
    /// This step implements a generic stat change to any stat - from the hero's health,
    /// to the resources available in the realm. These changes can be "known" (shown to the player
    /// beforehand) or not. This step type is also used to represent costs of taking a choice.
    /// </summary>
    [Element("Stat: Change"), NodeSupportsOdin]
    public class SEditorStatChange : EditorStep {
        // === Serialized properties

        public StoryRoleTarget target = StoryRoleTarget.Hero;
        [ShowIf(nameof(ShowLocationRef))] public LocationReference locationRef;

        [LabelText("Stat")] [RichEnumExtends(typeof(StatType))]
        public RichEnumReference affectedStat = AliveStatType.Health;

        [LabelText("Type")] public StatDefinedRange definedRange = StatDefinedRange.Custom;

        [ShowIf("@definedRange == StatDefinedRange.Custom")]
        public StatValue statValue;

        [HorizontalGroup("A", Gap = 0), LabelWidth(100)]
        public bool isKnown, isCost;

        [HorizontalGroup("B", Gap = 0), LabelWidth(100), ShowIf(nameof(isKnown))]
        public bool showStatValue = true;

        [HorizontalGroup("B", Gap = 0), LabelWidth(100), ShowIf(nameof(showStatValue)), LabelText("Show as %")]
        public bool showAsPercentage;

        [Tooltip("To use variable multiplier, you must add directly below this step, step - SVariableMultiply.\n" +
                 "Default multiplier value is 1, after going through this step it will multiply the multiplier by value set in SVariableMultiply.\n" +
                 "For example, StatValue is 20, you set SVariableAdd to 2, so first time the value is 20 * 1 =20,\n" +
                 "The next time the value is 20 * (1 * 2) = 40 then 20 * (2 * 2) = 80 etc.")]
        [InfoBox("You must add any SVariable directly below this step to use variables", InfoMessageType.Error, VisibleIf = nameof(ShowVariableMultiplier))]
        [LabelWidth(130)]
        public bool useVariableMultiplier;

        [LabelWidth(130), LabelText("Suppress Notification")]
        public bool suppressStatNotification;

        bool ShowLocationRef => target == StoryRoleTarget.ChosenLocations;

        bool ShowVariableMultiplier {
            get {
                if (!useVariableMultiplier) return false;

                int index = Parent.elements.IndexOf(this);
                return Parent.elements.Count <= index + 1 || !(Parent.elements[index + 1] is SEditorVariableReference);
            }
        }

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SStatChange {
                target = target,
                locationRef = locationRef,
                affectedStat = affectedStat,
                definedRange = definedRange,
                statValue = statValue,
                isKnown = isKnown,
                isCost = isCost,
                showStatValue = showStatValue,
                showAsPercentage = showAsPercentage,
                useVariableMultiplier = useVariableMultiplier,
                suppressStatNotification = suppressStatNotification
            };
        }
    }

    public partial class SStatChange : StoryStep {
        public StoryRoleTarget target = StoryRoleTarget.Hero;
        public LocationReference locationRef;
        public RichEnumReference affectedStat = AliveStatType.Health;
        public StatDefinedRange definedRange = StatDefinedRange.Custom;
        public StatValue statValue;
        public bool isKnown, isCost;
        public bool showStatValue, showAsPercentage;
        public bool useVariableMultiplier;
        public bool suppressStatNotification;

        public StatType AffectedStat => affectedStat.Enum as StatType;

        SStatChangeSettings? _settings;

        SStatChangeSettings Settings => _settings ??= new SStatChangeSettings {
            step = this,
            target = target,
            locationRef = locationRef,
            affectedStat = AffectedStat,
            definedRange = definedRange,
            statValue = statValue,
            isKnown = isKnown,
            isCost = isCost,
            showStatValue = showStatValue,
            showAsPercentage = showAsPercentage,
            useVariableMultiplier = useVariableMultiplier,
            suppressStatNotification = suppressStatNotification
        };

        public override StepResult Execute(Story story) {
            return Execute(Settings, story);
        }

        public override void AppendKnownEffects(Story story, ref StructList<string> effects) {
            AppendKnownEffects(Settings, story, ref effects);
        }

        public override StepRequirement GetRequirement() {
            return GetRequirement(Settings);
        }

        public static StepResult Execute(SStatChangeSettings settings, Story story) {
            // calculate the value
            StatValue changeValue;
            if (settings.definedRange == StatDefinedRange.Custom) {
                changeValue = settings.statValue;
            } else {
                float value = StatDefinedValuesConfig.GetValue(settings.affectedStat, settings.definedRange, 0f);
                changeValue = new StatValue(value);
            }

            if (settings.useVariableMultiplier) {
                changeValue.value *= GetVariableMultiplier(settings.step, story);
            }

            // execute the effect
            List<StatChangeValue> changes = new();
            changes.Add(StatChangeValue.Direct(ExtractStat(settings, story), changeValue));

            foreach (StatChangeValue statChangeValue in changes) {
                float prev = statChangeValue.AffectedStat;
                statChangeValue.AffectedStat.IncreaseBy(statChangeValue.Change);

                // inform about change
                var stat = statChangeValue.AffectedStat;
                int change = (int)(statChangeValue.AffectedStat - prev);
                if (!settings.suppressStatNotification) {
                    StoryUtils.AnnounceGettingStat(settings.affectedStat, change, story, settings.target == StoryRoleTarget.Hero);
                }

                story.ShowChange(stat, change);
            }

            return StepResult.Immediate;
        }

        public static void AppendKnownEffects(SStatChangeSettings settings, Story api, ref StructList<string> effects) {
            if (!settings.isKnown) {
                return;
            }

            StatValue changeValue = settings.statValue;
            if (settings.useVariableMultiplier) {
                changeValue.value *= GetVariableMultiplier(settings.step, api);
            }

            string statValueText = string.Empty;

            if (settings.showStatValue) {
                bool useUnExtractedPercentStat = settings is { showAsPercentage: true, statValue: { ValueType: ValueType.Percent or ValueType.PercentOfMax } };
                var statValue = useUnExtractedPercentStat ? changeValue.value / 100F : StatChangeValue.Direct(ExtractStat(settings, api), changeValue).Change;
                statValueText = statValue.ToString(settings.showAsPercentage ? "P0" : string.Empty, LocalizationHelper.SelectedCulture)
                    .Replace(' ', LocTerms.NonBreakingSpace);
            }

            if (changeValue.value > 0) {
                statValueText = $"+{statValueText}";
            } else if (changeValue.value < 0) {
                statValueText = $"-{statValueText}";
            }

            effects.Add($"{statValueText} {settings.affectedStat.DisplayName}");
        }

        public static StepRequirement GetRequirement(SStatChangeSettings settings) {
            if (!settings.isCost) {
                return _ => true;
            }

            return story => {
                StatValue changeValue = settings.statValue;
                if (settings.useVariableMultiplier) {
                    changeValue.value *= GetVariableMultiplier(settings.step, story);
                }

                var change = StatChangeValue.Direct(ExtractStat(settings, story), changeValue);
                if (change.AffectedStat.Type == AliveStatType.Health) {
                    return change.AffectedStat.ModifiedValue + change.Change >= 1;
                }

                return change.AffectedStat.ModifiedValue >= -change.Change;
            };
        }

        public override string GetKind(Story story) {
            return "Story";
        }

        static float GetVariableMultiplier(StoryStep step, Story story) {
            int index = Array.IndexOf(step.parentChapter.steps, step);
            if (index + 1 < step.parentChapter.steps.Length) {
                var nextNode = step.parentChapter.steps[index + 1];
                if (nextNode is SVariableReference variableReference) {
                    // override type to custom for this operation, because here variable should always use custom type
                    var oldType = variableReference.var.type;
                    variableReference.var.type = VariableType.Custom;
                    float value = variableReference.var.GetValue(story, variableReference.context, null, 1f);
                    variableReference.var.type = oldType;
                    return value;
                }
            }

            return 1f;
        }

        static Stat ExtractStat(SStatChangeSettings settings, Story story) {
            IWithStats withStats = StoryRole.DefaultForStat(settings.affectedStat, settings.target)
                ?.RetrieveFrom<IWithStats>(story, settings.locationRef)
                ?.FirstOrDefault();
            return withStats?.Stat(settings.affectedStat);
        }

        public struct SStatChangeSettings {
            public StoryStep step;
            public StoryRoleTarget target;
            public LocationReference locationRef;
            public StatType affectedStat;
            public StatDefinedRange definedRange;
            public StatValue statValue;

            public bool isKnown, isCost;
            public bool showStatValue, showAsPercentage;
            public bool useVariableMultiplier;
            public bool suppressStatNotification;
        }
    }
}