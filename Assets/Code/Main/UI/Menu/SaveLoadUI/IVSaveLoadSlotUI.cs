using Awaken.TG.Main.UI.Components;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.UI.Menu.SaveLoadUI {
    public interface IVSaveLoadSlotUI : IView {
        ARButton SlotButton { get; }
    }
}