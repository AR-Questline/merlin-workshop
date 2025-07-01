using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterStats {
    [UsesPrefab("CharacterSheet/Stats/" + nameof(VCharacterStatsUI))]
    public class VCharacterStatsUI : View<CharacterStatsUI>, IVEntryParentUI, IAutoFocusBase {
        [SerializeField] TextMeshProUGUI statisticsTitleLabel;
        [SerializeField, LocStringCategory(Category.UI)] LocString statisticsTitle;
        [SerializeField] TMP_Text requiredInfo;

        [field: SerializeField] public Transform EntriesParent { get; private set; }
        [field: SerializeField] public Transform StatsSummaryParent { get; private set; }

        protected override void OnInitialize() {
            statisticsTitleLabel.SetText(statisticsTitle);
            FocusFirstItem().Forget();
        }
        
        protected override void OnMount() {
            Target.InitializeStatEntries();
            SetupRequiredInfo();
        }
        
        async UniTaskVoid FocusFirstItem() {
            if (await AsyncUtil.DelayFrame(Target)) {
                var firstEntry = Target.Elements<StatsEntryUI>().FirstOrDefault();
                if (firstEntry) {
                    World.Only<Focus>().Select(firstEntry.View<VStatsEntryUI>().Button);
                }
            }
        }
        
        void SetupRequiredInfo() {
            requiredInfo.SetActiveAndText(TalentTree.IsUpgradeAvailable == false, LocTerms.UITalentFireplaceRequired.Translate());
        }
    }
}