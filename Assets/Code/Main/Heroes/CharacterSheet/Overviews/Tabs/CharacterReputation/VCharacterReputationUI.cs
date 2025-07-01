using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterReputation {
    [UsesPrefab("CharacterSheet/Overview/VCharacterReputationUI")]
    public class VCharacterReputationUI : View<CharacterReputationUI> {
        [SerializeField] Image factionIcon;
        [SerializeField] TextMeshProUGUI factionNameText;
        [SerializeField] TextMeshProUGUI factionDescriptionText;
        [SerializeField] TextMeshProUGUI reputationNameText;
        [SerializeField] TextMeshProUGUI reputationDescriptionText;
        [SerializeField] TextMeshProUGUI penaltyDescriptionText;
        [SerializeField] TextMeshProUGUI effectsDescriptionText;
        
        [field: SerializeField] public Transform FactionsEntryParent { get; private set; }

        public void RefreshFactionInfo(FactionEntryUI factionEntryUI) {
            factionNameText.SetText(factionEntryUI.faction.FactionName);
            factionDescriptionText.SetText(factionEntryUI.faction.FactionDescription);
            reputationNameText.SetText(factionEntryUI.ReputationName);
            reputationDescriptionText.SetText(factionEntryUI.ReputationDescription);
            effectsDescriptionText.SetText(factionEntryUI.factionEffects);
            factionEntryUI.faction.FactionIconReference.TryRegisterAndSetup(this, factionIcon);
        }
    }
}