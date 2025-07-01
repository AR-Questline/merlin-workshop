using Awaken.TG.Main.Heroes.Items.Tooltips.Base;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips {
    public class VCSimpleTooltip : View<FloatingTooltipUI> {
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text description;

        public void RefreshContent(string tittle, string desc) {
            title.SetActiveAndText(!string.IsNullOrEmpty(tittle), tittle);
            description.SetActiveAndText(!string.IsNullOrEmpty(desc), desc);
        }
    }
}