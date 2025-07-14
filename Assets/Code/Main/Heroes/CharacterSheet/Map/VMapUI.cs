using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Map {
    [UsesPrefab("CharacterSheet/Map/" + nameof(VMapUI))]
    public class VMapUI : VTabParent<MapUI> {
        public const float TooltipFadeDuration = 0.4f;
        [SerializeField] VGenericPromptUI mapTranslateCustomPrompt;
        [SerializeField] CanvasGroup fastTravelPrompt;
        [SerializeField] TextMeshProUGUI fastTravelPromptText;
        [SerializeField] GameObject fastTravelPromptIcon;

        public VGenericPromptUI MapTranslateCustomPrompt => mapTranslateCustomPrompt;
        public CanvasGroup FastTravelPrompt => fastTravelPrompt;
        
        void Awake() {
            fastTravelPrompt.alpha = 0;
        }

        protected override void OnInitialize() {
            SetFastTravelPromptDefault();
            mapTranslateCustomPrompt.SetName(LocTerms.UIMapMove.Translate());
        }

        public void SetFastTravelPromptDefault() {
            fastTravelPromptText.SetText(LocTerms.FastTravelPopupTitle.Translate());
            fastTravelPromptIcon.SetActive(true);
        }

        public void SetFastTravelPromptNotAvailable() {
            fastTravelPromptText.SetText(LocTerms.FastTravelOnlyInBonfire.Translate());
            fastTravelPromptIcon.SetActive(false);
        }
    }
}
