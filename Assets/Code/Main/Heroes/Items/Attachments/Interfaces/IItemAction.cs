using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.Items.Attachments.Interfaces {
    public interface IItemAction : IElement<Item> {
        ItemActionType Type { get; }
        int Priority() => 0;
        void Submit();
        void AfterPerformed(); // Submit => Cancel => AfterPerformed
        void Perform();
        void Cancel();
    }
}