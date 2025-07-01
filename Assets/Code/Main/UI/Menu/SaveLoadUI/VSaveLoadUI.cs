using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Menu.SaveLoadUI {
    [UsesPrefab("UI/SaveLoad/VSaveLoadUI")]
    public class VSaveLoadUI : View<ISaveLoadUI>, IAutoFocusBase {
        [SerializeField] TextMeshProUGUI titleText, savingText, availableSlotsText;
        [SerializeField] Transform promptsHost;
        [SerializeField] GameObject savingBlend;
        [SerializeField] RecyclableCollectionManager recyclableCollectionManager;
        [SerializeField] Transform slotsParent;
        [SerializeField] Transform newSaveSlotParent;
        [SerializeField] VerticalLayoutGroup mainContentLayoutGroup;

        Focus _focus;

        public RecyclableCollectionManager RecyclableCollectionManager => recyclableCollectionManager;
        public Transform NewSaveSlotParent => newSaveSlotParent;
        public Transform SlotsParent => slotsParent;
        public Transform PromptsHost => promptsHost;
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
        ISaveLoadSlotUI Slot => Target.TryGetElement<ISaveLoadSlotUI>();
        ARButton SlotButton => Slot?.View<IVSaveLoadSlotUI>().SlotButton;

        protected override void OnMount() {
            mainContentLayoutGroup.enabled = false;
            _focus = World.Only<Focus>();
            titleText.SetText(Target.TitleName);
            savingText.SetText(LocTerms.Saving.Translate());
            
            EnableCollectionManagerDelayed().Forget();
            Target.ListenTo(Model.Events.AfterElementsCollectionModified, OnElementsChanged, this);
        }
        
        void SetAvailableSlotsText() {
            uint saveSlotsCount = Target.Elements<SaveLoadSlotUI>().Count();
            uint availableSlotsCount = saveSlotsCount > SaveMenuUI.MaxSlotsCount ? 0 : SaveMenuUI.MaxSlotsCount - saveSlotsCount;
            availableSlotsText.SetText($"{LocTerms.Available.Translate()}: {availableSlotsCount.ToString().ColoredText(ARColor.MainWhite)}/{SaveMenuUI.MaxSlotsCount}");
        }

        async UniTaskVoid EnableCollectionManagerDelayed() {
            if (await AsyncUtil.DelayFrame(Target)) {
                recyclableCollectionManager.EnableCollectionManager();
                mainContentLayoutGroup.enabled = true;
                _focus.Select(SlotButton);
                SetAvailableSlotsText();
            }
        }
        
        void OnElementsChanged(Element element) {
            SetAvailableSlotsText();
            if (element is not ISaveLoadSlotUI saveLoadSlotUI || element.HasBeenDiscarded == false) return;

            var prevIndex = saveLoadSlotUI.Index - 1 < 0 
                ? Target is SaveMenuUI ? -1 : 0 
                : saveLoadSlotUI.Index - 1;

            FocusWithDelay(prevIndex).Forget();
        }
        
        async UniTaskVoid FocusWithDelay(int index) {
            if (!await AsyncUtil.DelayFrame(this)) return;
            recyclableCollectionManager.ContainerResize();
            
            var prevElement = Target.Elements<ISaveLoadSlotUI>().FirstOrDefault(e => e.Index == index);
            if (prevElement != null) {
                _focus.Select(prevElement.View<IVSaveLoadSlotUI>().SlotButton);
            }
        }
        
        public void SetActiveSavingBlend(bool active) {
            savingBlend.SetActive(active);
        }
    }
}