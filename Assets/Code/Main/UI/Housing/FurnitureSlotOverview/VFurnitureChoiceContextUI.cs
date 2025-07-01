using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Housing.FurnitureSlotOverview {
    [UsesPrefab("UI/Housing/" + nameof(VFurnitureChoiceContextUI))]
    public class VFurnitureChoiceContextUI : RetargetableView<FurnitureChoiceUI>, IFocusSource {
        [SerializeField] ButtonConfig buttonConfig;
        [SerializeField] Image furnitureChoiceImage;
        [SerializeField] GameObject inUseGameObject;

        public bool ForceFocus => false;
        public Component DefaultFocus => buttonConfig.button;

        protected override void OnInitialize() {
            World.EventSystem.ListenTo(EventSelector.AnySource, FurnitureChoiceUI.Events.OnFurnitureVariantUnlocked, this, RefreshAll);
            World.EventSystem.ListenTo(EventSelector.AnySource, FurnitureChoiceUI.Events.OnFurnitureVariantChanged, this, RefreshAll);
            RefreshAll();
            base.OnInitialize();
        }

        protected override void OnNewTarget() {
            SetupButton();
        }
        
        void SetupButton() {
            if (Target.Icon is { IsSet: true }) {
                Target.Icon.RegisterAndSetup(this, furnitureChoiceImage);
            }
            
            buttonConfig.InitializeButton(OnClicked);
            buttonConfig.button.ClearAllOnClickAudioFeedback();
            buttonConfig.button.OnHover += OnHover;
            buttonConfig.button.OnSelected += OnSelect;
        }
        
        void OnHover(bool state) {
            if (RewiredHelper.IsGamepad) return;
            Target.TriggerChoiceHovered(state);
        }

        void OnSelect(bool state) {
            if (RewiredHelper.IsGamepad == false) return;
            Target.TriggerChoiceHovered(state);
        }
        
        void OnClicked() {
            buttonConfig.button.PlayClickAudioFeedback(true, false);
            Target.Select();
        }
        
        void RefreshAll() {
            bool isUsed = Target.IsVariantUsed();
            inUseGameObject.SetActive(isUsed);
        }
    }
}