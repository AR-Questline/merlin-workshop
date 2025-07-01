using System;
using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Housing;
using Awaken.TG.Main.Heroes.Housing.Farming;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Times;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Components {
    [Serializable]
    public class PlantItemInfoComponent : IItemTooltipComponent {
        [SerializeField] TextMeshProUGUI requiredSpaceText;
        [SerializeField] TextMeshProUGUI spaceText;
        [SerializeField] TextMeshProUGUI growthTimeText;
        [SerializeField] TextMeshProUGUI timeText;
        [SerializeField] TextMeshProUGUI plantSlotStateText;
        [SerializeField] TextMeshProUGUI additionalStateText;

        [SerializeField] List<GameObject> itemSeedObjects;
        [SerializeField] List<GameObject> plantSlotObjects;

        string _requiredSpace;
        string _growthTime;
        string _hours;

        string RequiredSpace => _requiredSpace ??= LocTerms.FarmingRequiredSpace.Translate();
        string GrowTime => _growthTime ??= LocTerms.FarmingGrowthTime.Translate();
        string Hours => _hours ??= LocTerms.Hours.Translate();
        public View TargetView { get; set; }
        public ref PartialVisibility Visibility => ref _visibility;
        PartialVisibility _visibility;
        public bool UseReadMore { get; private set; }

        public void Refresh(IItemDescriptor descriptor, IItemDescriptor descriptorToCompare) {
            bool isPlantSlot = descriptor.PlantSlot != null;
            itemSeedObjects.ForEach(obj => obj.SetActive(!isPlantSlot));
            plantSlotObjects.ForEach(obj => obj.SetActive(isPlantSlot));
            
            if (isPlantSlot) {
                PlantSlot plantSlot = descriptor.PlantSlot;
                plantSlotStateText.SetText(plantSlot.DisplayState);

                bool isGrowing = plantSlot.State is PlantState.Growing;
                additionalStateText.gameObject.SetActive(isGrowing);
                if (isGrowing) {
                    ARTimeSpan remainingTime = plantSlot.TotalTimeLeft;
                    additionalStateText.SetText($"| <sprite=0> {remainingTime.Hours:00}:{remainingTime.Minutes:00}");
                }
                
                return;
            }
            
            requiredSpaceText.SetText(RequiredSpace);
            growthTimeText.SetText(GrowTime);
            ItemSeed seed = descriptor.ItemSeed;
            if (seed) {
                spaceText.SetText(HousingUtils.GetGridPlantSize(seed.plantSize));
                timeText.SetText($"{seed.totalGrowthTime.TotalHours} {Hours}");
            }
        }

        public void Refresh(IItemDescriptor descriptor, IItemDescriptor descriptorToCompare, View view) {
            Refresh(descriptor, descriptorToCompare);
        }

        public void ToggleSectionActive(bool active) { }
    }
}