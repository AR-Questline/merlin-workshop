using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterReputation {
    [UsesPrefab("CharacterSheet/Overview/VFactionEntryUI")]
    public class VFactionEntryUI : View<FactionEntryUI> {
        [SerializeField] Image fameBar;
        [SerializeField] Image infamyBar;
        [SerializeField] Image factionIcon;
        [SerializeField] TextMeshProUGUI factionNameText;
        [SerializeField] TextMeshProUGUI reputationKindText;
        [SerializeField] ButtonConfig factionButtonConfig;
        [SerializeField, ListDrawerSettings(IsReadOnly = true)] [UnityEngine.Scripting.Preserve] RectTransform[] infamyThresholds = new RectTransform[3]; 
        [SerializeField, ListDrawerSettings(IsReadOnly = true)] [UnityEngine.Scripting.Preserve] RectTransform[] fameThresholds = new RectTransform[3]; 

        public ARButton Button => factionButtonConfig.button;
        public override Transform DetermineHost() => Target.ParentModel.View<VCharacterReputationUI>().FactionsEntryParent;

        protected override void OnInitialize() {
            SetupReputationThresholds();
            factionButtonConfig.InitializeButton(OnFactionClick);
            factionNameText.SetText(Target.faction.FactionName);
            reputationKindText.SetText(Target.ReputationName);
            Target.faction.FactionIconReference.TryRegisterAndSetup(this, factionIcon);

            float maxReputationPoints = Target.maxReputationPoints;
            if (maxReputationPoints > 0) {
                fameBar.fillAmount = Target.famePoints / maxReputationPoints;
                infamyBar.fillAmount = Target.infamyPoints / maxReputationPoints;
            }
        }

        void OnFactionClick() {
            Target.Trigger(FactionEntryUI.Events.FactionChanged, Target);
        }

        void SetupReputationThresholds() {
            // for (int i = 0; i < Target.faction.FactionReputationThresholds.Count; i++) {
            //     float reputationThreshold = Target.faction.FactionReputationThresholds[i];
            //     infamyThresholds[i].anchorMin = new Vector2(1 - reputationThreshold, 0);
            //     infamyThresholds[i].anchorMax = new Vector2(1 - reputationThreshold, 1);
            //     fameThresholds[i].anchorMin = new Vector2(reputationThreshold, 0);
            //     fameThresholds[i].anchorMax = new Vector2(reputationThreshold, 1);
            //     var anchoredPosition = infamyThresholds[i].anchoredPosition;
            //     anchoredPosition.x = 0;
            //     infamyThresholds[i].anchoredPosition = anchoredPosition;
            //     fameThresholds[i].anchoredPosition = anchoredPosition;
            // }
        }
    }
}