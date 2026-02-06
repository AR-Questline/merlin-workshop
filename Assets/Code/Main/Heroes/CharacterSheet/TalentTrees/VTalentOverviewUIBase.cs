using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees {
    public abstract class VTalentOverviewUIBase : VTabParent<ITalentOverview>, IVTalentOverview, IAutoFocusBase {
        [Title("Talent overview")]
        [SerializeField] TMP_Text requiredInfo;
        [SerializeField] TMP_Text treeLevelPoints;
        [SerializeField] TMP_Text treeLevelLabel;

        protected virtual string RequiredInfoText => LocTerms.UITalentFireplaceRequired.Translate();
        
        protected override void OnMount() {
            requiredInfo.text = RequiredInfoText;
            SetupRequiredInfo(true);
            treeLevelLabel.TrySetText(LocTerms.UITalentTreeLevel.Translate());
        }
        
        public void UpdateTreeLevel(int level) {
            treeLevelPoints.TrySetText($"{level}/{Target.CurrentTable.MaxTreeLevel}");
        }
        
        public void SetupRequiredInfo(bool canBeShown) {
            requiredInfo.TrySetActiveOptimized(!TalentTree.IsUpgradeAvailable && canBeShown);
        }
    }
}