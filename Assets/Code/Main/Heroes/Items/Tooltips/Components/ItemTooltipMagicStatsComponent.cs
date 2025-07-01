using System;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Utility;
using Awaken.Utility.GameObjects;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Components {
    [Serializable]
    public class ItemTooltipMagicStatsComponent {
        [SerializeField] GameObject statsSection;
        [SerializeField, CanBeNull] GameObject sectionSeparator;
        [SerializeField] MagicStatsSection lightCastStats;
        [SerializeField] MagicStatsSection heavyCastStats;

        public void SetActiveState(bool isActive) {
            if (statsSection) {
                statsSection.SetActiveOptimized(isActive);
            }
        }

        public void SetupCommonSection() {
            sectionSeparator.SetActiveOptimized(true);
        }
        
        public void SetupLightCast(MagicItemTemplateInfo cast, string effectInfo, string costInfo, string description, VisibilityConfig visibilityConfig) {
            SetupStats(lightCastStats, LocTerms.MagicLightCast.Translate(), cast, effectInfo, costInfo, description, visibilityConfig);
        }
        
        public void SetupHeavyCast(MagicItemTemplateInfo cast, string effectInfo, string costInfo, string description, VisibilityConfig visibilityConfig) {
            SetupStats(heavyCastStats, LocTerms.MagicHeavyCast.Translate(), cast, effectInfo, costInfo, description, visibilityConfig);
        }
        
        void SetupStats(MagicStatsSection section, string title, MagicItemTemplateInfo castInfo, string effectInfo, string costInfo, string description, VisibilityConfig visibilityConfig) {
            if (sectionSeparator && visibilityConfig.WholeContentEnabled == false) {
                sectionSeparator.SetActiveOptimized(false);
            }
            
            section.SetupStats(title, castInfo.MagicType.DisplayName, effectInfo, costInfo, description, visibilityConfig);
        }

        [Serializable]
        class MagicStatsSection {
            [SerializeField] GameObject content;
            [SerializeField] TMP_Text castTitleLabel;
            [SerializeField] TMP_Text castTypeLabel;
            [SerializeField] TMP_Text effectLabel;
            [SerializeField] TMP_Text costLabel;
            [SerializeField] TMP_Text descriptionLabel;
            [SerializeField, CanBeNull] GameObject costAndEffectSeparator;

            public void SetupStats(string castTitle, string castType, string effect, string cost, string description, VisibilityConfig visibilityConfig) {
                castTitleLabel.text = castTitle;
                castTypeLabel.text = castType;
                descriptionLabel.TrySetText(description);
                
                content.TrySetActiveOptimized(visibilityConfig.WholeContentEnabled);
                effectLabel.SetActiveAndText(visibilityConfig.EffectEnabled, effect);
                costLabel.SetActiveAndText(visibilityConfig.CostEnabled, cost);
                
                if (costAndEffectSeparator) {
                    costAndEffectSeparator.SetActiveOptimized(visibilityConfig.EffectEnabled && visibilityConfig.CostEnabled);
                }
            }
        }
        
        public class VisibilityConfig {
            public bool WholeContentEnabled { get; set; }
            public bool EffectEnabled { get; set; }
            public bool CostEnabled { get; set; }
        }
    }
}
