using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.Housing.FurnitureSlotOverview {
    [UsesPrefab("UI/Housing/" + nameof(VFurnitureSlotOverviewUI))]
    public class VFurnitureSlotOverviewUI : View<FurnitureSlotOverviewUI>, IAutoFocusBase {
        const int ColumnCount = 5;
        const int RowCount = 4;
        
        [SerializeField] TextMeshProUGUI furnitureSlotText;
        [SerializeField] TextMeshProUGUI furnitureChoiceText;
        [SerializeField] TextMeshProUGUI furnitureSlotInfoText;
        [SerializeField] VCListAdjuster listAdjuster;
        
        [field: SerializeField] public Transform FurnitureChoicesHost { get; private set; }
        [field: SerializeField] public Transform PromptsHost { get; private set; }
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            listAdjuster.FullAdjustWithCollectionRefresh(RowCount, ColumnCount).Forget();
            
            bool slotNameSet = !string.IsNullOrEmpty(Target.FurnitureSlot.DisplayName);
            furnitureSlotText.SetActiveAndText(slotNameSet, Target.FurnitureSlot.DisplayName);
            furnitureChoiceText.SetText(LocTerms.Empty.Translate());
            furnitureSlotInfoText.SetText(LocTerms.HousingFurnitureInUse.Translate());
            furnitureSlotInfoText.gameObject.SetActive(false);
            
            World.EventSystem.ListenTo(EventSelector.AnySource, FurnitureChoiceUI.Events.OnFurnitureVariantHoverStarted, this, Refresh);
            World.EventSystem.ListenTo(EventSelector.AnySource, FurnitureChoiceUI.Events.OnFurnitureVariantChanged, this, Refresh);
        }

        void Refresh(FurnitureChoiceUI choice) {
            bool choiceNameSet = !string.IsNullOrEmpty(choice.DisplayName);
            furnitureChoiceText.SetActiveAndText(choiceNameSet, choice.DisplayName);
            furnitureSlotInfoText.gameObject.SetActive(choice.IsVariantUsed());
        }
    }
}