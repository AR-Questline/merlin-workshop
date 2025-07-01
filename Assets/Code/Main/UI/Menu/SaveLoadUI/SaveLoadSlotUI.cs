using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI;

namespace Awaken.TG.Main.UI.Menu.SaveLoadUI {
    public partial class SaveLoadSlotUI : Element<ISaveLoadUI>, ISaveLoadSlotUI, IWithRecyclableView {
        public sealed override bool IsNotSaved => true;
        public readonly SaveSlot saveSlot;
        public int Index { get; private set; }

        public SaveLoadSlotUI(SaveSlot saveSlot, int index) {
            Index = index;
            this.saveSlot = saveSlot;
            this.saveSlot.ListenTo(Events.BeforeDiscarded, Discard, this);
        }

        protected override void OnInitialize() {
            saveSlot.screenShotTaken += TriggerChange;
        }

        public void DeleteSlot() {
            saveSlot.Discard();
            if (!HasBeenDiscarded) Discard();
        }
        
        public void RefreshIndex(int index) {
            Index = index;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            saveSlot.screenShotTaken -= TriggerChange;
        }
    }
}