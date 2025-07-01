using System;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Components {
    [Serializable]
    public class ItemTooltipStatsComponent {
        [SerializeField] public Color sideStatsColor;
        [SerializeField] [CanBeNull] public TextMeshProUGUI primaryStatsText;
        [SerializeField] [CanBeNull] public TextMeshProUGUI secondaryStatsText;
        [SerializeField] [CanBeNull] public TextMeshProUGUI sideStatsText;

        public void SetupStats(string primary = null, string secondary = null, string sideStat = null) {
            SetupLabel(primaryStatsText, primary);
            SetupLabel(secondaryStatsText, secondary);
            SetupLabel(sideStatsText, sideStat);
        }
        
        void SetupLabel (TextMeshProUGUI label, string text) {
            bool hasText = !string.IsNullOrEmpty(text);
            
            if (label != null) {
                if (hasText) {
                    label.text = text;
                }

                label.gameObject.SetActive(hasText);
            }
        }
    }
}
