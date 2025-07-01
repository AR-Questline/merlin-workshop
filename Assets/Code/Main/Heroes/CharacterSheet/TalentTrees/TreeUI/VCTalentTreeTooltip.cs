using System.Linq;
using System.Text;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Maths;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.TreeUI {
    public class VCTalentTreeTooltip : ViewComponent {
        [SerializeField] RectTransform tooltip;
        [SerializeField] TextMeshProUGUI talentNameText;
        [SerializeField] TextMeshProUGUI currentDescriptionText;
        [SerializeField] TextMeshProUGUI nextLevelLabel;
        [SerializeField] TextMeshProUGUI nextDescriptionText;
        [SerializeField] TextMeshProUGUI requirementsText;
        [SerializeField] TextMeshProUGUI keywordsText;

        [SerializeField] GameObject currentDescriptionRoot;
        [SerializeField] GameObject nextDescriptionRoot;
        [SerializeField] GameObject nextSeparatorLine;
        [SerializeField] GameObject requirements;
        [SerializeField] GameObject nameRoot;
        [SerializeField] GameObject keywordsRoot;
        
        [SerializeField] ContentSizeFitter fitter;

        protected override void OnAttach() {
            HideTooltip();
            nextLevelLabel.SetText(LocTerms.UITalentsNextLevel.Translate());
            World.EventSystem.ListenTo(EventSelector.AnySource, Talent.Events.TalentChanged, this, talent => RefreshTooltip(talent).Forget());
        }

        public async UniTaskVoid ShowTooltip(Talent talent) {
            talentNameText.SetText(talent.TalentName);
            nameRoot.SetActive(string.IsNullOrEmpty(talent.TalentName) == false);
            RefreshDescription(talent);
            tooltip.TrySetActiveOptimized(true);

            await UniTask.DelayFrame(1);
            tooltip.RebuildAllBelowInverse();
        }

        public void HideTooltip() {
            tooltip.TrySetActiveOptimized(false);
        }

        async UniTaskVoid RefreshTooltip(Talent talent) {
            RefreshDescription(talent);
            
            await UniTask.DelayFrame(1);

            if (tooltip != null) {
                tooltip.RebuildAllBelowInverse();
            } 
        }

        void RefreshDescription(Talent talent) {
            currentDescriptionRoot.SetActiveOptimized(talent.IsUpgraded);
            currentDescriptionText.SetText(talent.CurrentLevelDescription);
            nextSeparatorLine.SetActiveOptimized(talent.IsUpgraded && talent.MaxLevelReached == false);
            nextDescriptionRoot.SetActiveOptimized(talent.MaxLevelReached == false);
            nextDescriptionText.SetText(talent.NextLevelDescription);

            bool hasKeywords =  talent.TalentKeywords != null;
            keywordsRoot.SetActiveOptimized(hasKeywords);

            if (hasKeywords) {
                var contentToDisplay = talent.TalentKeywords.ElementsToSpawn;
                var keywords = new StringBuilder();
                
                foreach (string entry in contentToDisplay.Select(tooltipValue => tooltipValue.Value as string)) {
                    keywords.AppendLine(entry);
                }
                
                keywordsText.SetText(keywords.ToString());
            } 

            if (talent.RequiredTreeLevelToUnlock > 0 && talent.IsMeetRequirements == false) {
                requirements.SetActive(true);
                requirementsText.SetText(LocTerms.UITalentTreeRequireTreeLevel.Translate(talent.RequiredTreeLevelToUnlock));
            } else {
                requirements.SetActive(false);
            }
        }
    }
}