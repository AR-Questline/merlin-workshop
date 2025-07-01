using System.Linq;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Housing;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.UI.Housing.FurnitureSlotOverview {
    public partial class FurnitureSlotOverviewUI : Model, IUIStateSource, IPromptHost, IWithPricePreview {
        Prompt _choosePrompt;
        FurnitureChoiceUI _furnitureChoiceUI;
        
        public FurnitureSlotBase FurnitureSlot { get; }
        
        public override Domain DefaultDomain => Domain.Gameplay;
        public sealed override bool IsNotSaved => true;

        public UIState UIState => UIState.ModalState(HUDState.MiddlePanelShown | HUDState.CompassHidden | HUDState.QuestTrackerHidden);
        public Transform PromptsHost => View<VFurnitureSlotOverviewUI>().PromptsHost;
        public int Price => 0;
        public bool CanAfford => true;

        FurnitureSlotOverviewUI(FurnitureSlotBase furnitureSlot) {
            FurnitureSlot = furnitureSlot;
        }

        protected override void OnFullyInitialized() {
            PrepareChoices();
            InitializePrompts();
            
            World.EventSystem.ListenTo(EventSelector.AnySource, FurnitureChoiceUI.Events.OnFurnitureVariantHoverStarted, this, Refresh);
            World.EventSystem.ListenTo(EventSelector.AnySource, FurnitureChoiceUI.Events.OnFurnitureVariantHoverEnded, this, RefreshOnHoverEnded);
            World.EventSystem.ListenTo(EventSelector.AnySource, FurnitureChoiceUI.Events.OnFurnitureVariantChanged, this, Refresh);
            RefreshAtInit();
            DelayedFocus().Forget();
            AddElement(new HeroHousingInvolvement());
        }

        void PrepareChoices() {
            var view = World.SpawnView<VFurnitureSlotOverviewUI>(this, true);
            bool slotHasTags = FurnitureSlot.Tags.IsNotNullOrEmpty();
            var availableFurnitureItems = Hero.Current.Element<HeroFurnitures>().FurnitureVariants.Where(variant =>
                    slotHasTags && variant.Tags.IsNotNullOrEmpty() &&
                    TagUtils.HasAnyTag(variant.Tags, FurnitureSlot.Tags))
                .ToArray();
            
            for (int index = 0; index < availableFurnitureItems.Length; index++) {
                FurnitureVariant variant = availableFurnitureItems[index];
                AddElement(new FurnitureChoiceUI(FurnitureSlot, variant, index, typeof(VFurnitureChoiceContextUI), view.FurnitureChoicesHost));
            }
        }

        void RefreshAtInit() {
            var choice = Elements<FurnitureChoiceUI>().FirstOrDefault();
            if (choice != null) {
                choice.TriggerChoiceHovered(true);
                _choosePrompt.SetActive(false);
            }
        }

        async UniTaskVoid DelayedFocus() {
            if (await AsyncUtil.DelayFrame(this, 3)) {
                World.Only<Focus>().Select(((IFocusSource)Elements<FurnitureChoiceUI>().FirstOrDefault()?.Views.FirstOrDefault())?.DefaultFocus);
            }
        }
        
        void InitializePrompts() {
            var prompts = AddElement(new Prompts(this));
            _choosePrompt = prompts.AddPrompt(Prompt.Tap(KeyBindings.UI.Items.SelectItem, LocTerms.Select.Translate(), ChoosePromptAction), this, false, false);
            prompts.AddPrompt(Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.Close.Translate(), Close), this);
        }
        
        void ChoosePromptAction() {
            _furnitureChoiceUI?.Use();
        }

        public static void OpenFurnitureSlotOverviewUI(FurnitureSlotBase furnitureSlot) {
            World.Add(new FurnitureSlotOverviewUI(furnitureSlot));
        }

        void Close() => Discard();

        void RefreshOnHoverEnded(FurnitureChoiceUI _) {
            _choosePrompt.SetActive(false);
        }

        void Refresh(FurnitureChoiceUI choice) {
            _furnitureChoiceUI = choice;
            bool isUsed = choice.IsVariantUsed();
            bool choosePromptActive = !isUsed;
            _choosePrompt.SetupState(choosePromptActive, choosePromptActive);
            
            this.Trigger(IWithPricePreview.Events.PriceRefreshed, true);
        }
    }
}