using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.UI.Components {
    public interface IWithPricePreview : IModel {
        int Price { get; }
        bool CanAfford { get; }

        public static class Events {
            public static readonly Event<IWithPricePreview, bool> PriceRefreshed = new(nameof(PriceRefreshed));
        }
    }
}