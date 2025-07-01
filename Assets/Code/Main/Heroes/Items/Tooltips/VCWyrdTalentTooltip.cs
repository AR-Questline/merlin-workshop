using Awaken.TG.Main.Heroes.Items.Tooltips.Base;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.Utility.GameObjects;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips {
    public class VCWyrdTalentTooltip : View<FloatingTooltipUI> {
        [SerializeField] GameObject titleSection;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text currentLevelDesc;
        [SerializeField] TMP_Text nextLevelDesc;

        public void RefreshContent(string tittle, string currentDesc, string nextDesc) {
            bool titleActive = !string.IsNullOrWhiteSpace(tittle);
            titleSection.SetActiveOptimized(titleActive);
            title.SetActiveAndText(titleActive, tittle);

            bool currentActive = !string.IsNullOrWhiteSpace(currentDesc);
            currentLevelDesc.SetActiveAndText(currentActive, currentDesc);
            
            bool nextActive = !string.IsNullOrWhiteSpace(nextDesc);
            nextLevelDesc.SetActiveAndText(nextActive, nextDesc);
        }
    }
}