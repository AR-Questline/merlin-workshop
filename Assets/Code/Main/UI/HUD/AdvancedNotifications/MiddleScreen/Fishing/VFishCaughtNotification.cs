using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Fishing {
    [UsesPrefab("HUD/AdvancedNotifications/VFishCaughtNotification")]
    public class VFishCaughtNotification : VAdvancedNotification<FishCaughtNotification> {
        [SerializeField] GameObject normalBg;
        [SerializeField] GameObject newFishBg;
        [SerializeField] TextMeshProUGUI newFish;
        [SerializeField] TextMeshProUGUI catchName;
        [SerializeField] Image catchIcon;
        [SerializeField] TextMeshProUGUI itemName;
        [SerializeField] Image itemIcon;
        [SerializeField] TextMeshProUGUI itemQuantity;
        [SerializeField] GameObject fishData;
        [SerializeField] TextMeshProUGUI weightLabel;
        [SerializeField] TextMeshProUGUI weight;
        [SerializeField] TextMeshProUGUI lenghtLabel;
        [SerializeField] TextMeshProUGUI lenght;
        [SerializeField] TextMeshProUGUI newRecord;
        [SerializeField] GameObject record;
        [SerializeField] VGenericPromptUI closePrompt;

        Prompt _closePrompt;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
        FishCaughtData Data => Target.data;

        protected override void OnInitialize() {
            newFish.text = LocTerms.FishNewTitleNotification.Translate();
            newRecord.text = LocTerms.FishNewRecordNotification.Translate();
            weightLabel.text = LocTerms.Weight.Translate();
            lenghtLabel.text = LocTerms.Length.Translate();
            weight.text = Data.fishWeight;
            lenght.text = Data.fishLength;
            catchName.text = Data.fishName;
            itemName.text = Data.itemName;
            itemQuantity.text = Data.itemQuantity.ToString();
            
            newFish.gameObject.SetActive(Data is { isFish: true, isNewFish: true });
            normalBg.SetActive(Data is {  isFish: true, isNewFish: false} or {isFish: false});
            newFishBg.SetActive(Data is {  isFish: true, isNewFish: true});
            record.SetActive(Data is {isFish: true, isRecord: true});
            newRecord.gameObject.SetActive(Data.isRecord);
            fishData.SetActive(Data.isFish);
            
            if (Data.fishIcon is {IsSet: true}) {
                Data.fishIcon.SetSprite(catchIcon);
            }
            
            if (Data.itemIcon is {IsSet: true}) {
                Data.itemIcon.SetSprite(itemIcon);
            }
            
            var prompts = Target.AddElement(new Prompts(null));
            _closePrompt = prompts.BindPrompt(Prompt.Tap(KeyBindings.Gameplay.Interact, LocTerms.Close.Translate(), ClosePopup), Target, closePrompt);
            _closePrompt.SetupState(true, true);
        }

        void ClosePopup() {
            Target.Discard();
        }
    }
}