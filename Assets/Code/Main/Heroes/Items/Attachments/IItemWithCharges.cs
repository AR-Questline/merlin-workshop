using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public interface IItemWithCharges : IElement {
        int ChargesRemaining { get; }
        void SpendCharges(int charges = 1);
        void RestoreCharges();
        
        public static class Events {
            public static readonly Event<Item, int> AllChargesSpent = new(nameof(AllChargesSpent));
        }
    }
}