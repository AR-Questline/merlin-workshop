using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Crafting.Slots {
    public interface ICraftingSlotView : IView {
        ButtonConfig SlotButton { get; }
    }
}