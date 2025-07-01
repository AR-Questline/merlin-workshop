using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats.StatConfig;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Utility;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Hero/Hero: Change currency"), NodeSupportsOdin]
    public class SEditorChangeCurrency : EditorStep {
        [LabelWidth(125)]
        [SerializeField] SChangeCurrency.CurrencyType currencyType;
        [LabelWidth(125)]
        public int amount;
        [LabelWidth(125), ShowIf(nameof(IsCost)), Tooltip("If player doesn't have required amount, should they be able to choose this branch?")]
        public bool ignoreRequirements;
        [LabelWidth(125), Tooltip("With this, the choice leading to this action will show how much of the currency the player gains or loses. Usually referred as 'Is Known' flag.")]
        public bool showInChoice;
        [LabelWidth(125), Tooltip("If true, the player will see a notification about the change in currency.")]
        public bool notificationUI = true;
        
        bool IsCost => amount < 0;
        
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SChangeCurrency {
                currencyType = currencyType,
                amount = amount,
                ignoreRequirements = ignoreRequirements,
                showInChoice = showInChoice,
                notificationUI = notificationUI
            };
        }
    }

    public partial class SChangeCurrency : StoryStep {
        public CurrencyType currencyType;
        public int amount;
        public bool ignoreRequirements;
        public bool showInChoice;
        public bool notificationUI = true;
        
        bool IsCost => amount < 0;
        
        SStatChange.SStatChangeSettings? _settings;
        SStatChange.SStatChangeSettings Settings {
            get {
                _settings ??= GetSStatChangeSettings();
                return _settings.Value;
            }
        }
        
        public override StepResult Execute(Story story) {
            return SStatChange.Execute(Settings, story);
        }

        public override void AppendKnownEffects(Story story, ref StructList<string> effects) {
            SStatChange.AppendKnownEffects(Settings, story, ref effects);
        }
        
        public override StepRequirement GetRequirement() {
            return SStatChange.GetRequirement(Settings);
        }

        SStatChange.SStatChangeSettings GetSStatChangeSettings() {
            return new SStatChange.SStatChangeSettings {
                step = this,
                target = StoryRoleTarget.Hero,
                locationRef = null,
                affectedStat = currencyType == CurrencyType.Wealth ? CurrencyStatType.Wealth : CurrencyStatType.Cobweb,
                definedRange = StatDefinedRange.Custom,
                statValue = new StatValue(amount),
                isKnown = showInChoice,
                isCost = IsCost && !ignoreRequirements,
                showStatValue = true,
                showAsPercentage = false,
                useVariableMultiplier = false,
                suppressStatNotification = !notificationUI
            };
        }
        
        public enum CurrencyType : byte {
            Wealth,
            [UnityEngine.Scripting.Preserve] Cobweb
        }
    }
}