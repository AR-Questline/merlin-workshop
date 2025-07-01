using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.Utility.Semaphores;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Selections;
using Awaken.TG.Utility;
using Awaken.Utility.GameObjects;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.Menu.SaveLoadUI {
    [UsesPrefab("UI/SaveLoad/VNewSaveSlotUI")]
    public class VNewSaveSlotUI : View<NewSaveSlotUI>, ISemaphoreObserver, IVSaveLoadSlotUI {
        [SerializeField] TextMeshProUGUI newSaveText;
        [SerializeField] ButtonConfig newSaveButton;
        
        CoyoteSemaphore _isHovered;
        
        public ARButton SlotButton => newSaveButton.button;
        public override Transform DetermineHost() => Target.ParentModel.View<VSaveLoadUI>().NewSaveSlotParent;
        
        void Update() {
            _isHovered.Update();
        }

        protected override void OnMount() {
            Refresh();
            Target.ParentModel.ListenTo(Model.Events.AfterElementsCollectionModified, Refresh, this);
            newSaveText.text = LocTerms.NewSave.Translate();
            _isHovered = new CoyoteSemaphore(this);
            newSaveButton.InitializeButton(() => Target.ParentModel.OpenNewSavePopup());
            newSaveButton.button.OnEvent += Handle;
        }

        void Refresh() {
            DetermineHost().TrySetActiveOptimized(Target.ParentModel.CanCreateNewSaveSlot);
        }
        
        UIResult Handle(UIEvent evt) {
            if (evt is UIEPointTo) {
                _isHovered.Notify();
                return UIResult.Accept;
            }

            return UIResult.Ignore;
        }
        
        void Hover() => World.Only<Selection>().Select(Target);

        void Unhover() => World.Only<Selection>().Deselect(Target);
        
        void ISemaphoreObserver.OnUp() => Hover();
        void ISemaphoreObserver.OnDown() => Unhover();
    }
}