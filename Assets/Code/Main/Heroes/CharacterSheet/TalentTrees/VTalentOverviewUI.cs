using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees {
    [UsesPrefab("CharacterSheet/TalentTree/" + nameof(VTalentOverviewUI))]
    public class VTalentOverviewUI : VTabParent<TalentOverviewUI>, IAutoFocusBase {
        [Title("Talent overview")]
        [SerializeField] TMP_Text requiredInfo;
        [SerializeField] TMP_Text treeLevelPoints;
        [SerializeField] TMP_Text treeLevelLabel;
        
        protected override void OnMount() {
            requiredInfo.text = LocTerms.UITalentFireplaceRequired.Translate();
            SetupRequiredInfo(true);
            treeLevelLabel.SetText(LocTerms.UITalentTreeLevel.Translate());
        }
        
        public void UpdateTreeLevel(int level) {
            treeLevelPoints.SetText($"{level}/{Target.CurrentTable.MaxTreeLevel}");
        }
        
        public void SetupRequiredInfo(bool canBeShown) {
            requiredInfo.TrySetActiveOptimized(TalentTree.IsUpgradeAvailable == false && canBeShown);
        }
    } 
}