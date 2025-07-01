using Awaken.TG.MVC;

namespace Awaken.TG.Main.Locations.Gems.GemManagement {
    public class VCGemResultUI : VCStaticItemInfoUI<GemManagementUI, VGemManagementUI> {
        protected override void Initialize() {
            Target.ListenTo(GemManagementUI.Events.GemSlotUnlocked, ItemRefreshed, this);
            Target.ListenTo(GemManagementUI.Events.GemAttached, change => ItemRefreshed(change.item), this);
            Target.ListenTo(GemManagementUI.Events.GemDetached, change => ItemRefreshed(change.item), this);
            Target.ListenTo(IGemBase.Events.ClickedItemChanged, ItemRefreshed, this);
            Target.ListenTo(Model.Events.AfterFullyInitialized, _ => SetupSections(Target.View<VGemManagementUI>()), this);
        }
    }
}