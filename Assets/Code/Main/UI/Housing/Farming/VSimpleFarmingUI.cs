using Awaken.TG.Main.Heroes.Housing;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.Housing.Farming {
    [UsesPrefab("UI/Housing/Farming/" + nameof(VSimpleFarmingUI))]
    public class VSimpleFarmingUI : View<SimpleFarmingUI>, IAutoFocusBase {
        [SerializeField] GameObject seedStatsObject;
        [SerializeField] TextMeshProUGUI seedNameText;
        [SerializeField] TextMeshProUGUI flowerpotNameText;
        [SerializeField] TextMeshProUGUI growthTimeText;
        [SerializeField] TextMeshProUGUI timeText;
        [SerializeField] TextMeshProUGUI plantSizeText;
        [SerializeField] TextMeshProUGUI sizeText;

        string _hours;
        
        [field: SerializeField] public Transform ItemsHost { get; private set; }
        [field: SerializeField] public Transform PromptsHost { get; private set; }
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            seedStatsObject.SetActive(false);
            _hours = LocTerms.Hours.Translate();
            seedNameText.SetText(string.Empty);
            flowerpotNameText.SetText(LocTerms.FarmingFlowerbed.Translate());
            growthTimeText.SetText(LocTerms.FarmingGrowthTime.Translate());
            plantSizeText.SetText(LocTerms.FarmingRequiredSpace.Translate());
        }

        public void SetSeedStats(ItemSeed seed) {
            seedStatsObject.SetActive(true);
            seedNameText.SetText(seed.ParentModel.DisplayName);
            sizeText.SetText(HousingUtils.GetGridPlantSize(seed.plantSize));
            timeText.SetText($"{seed.totalGrowthTime.TotalHours} {_hours}");
        }
    }
}