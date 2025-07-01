using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.UI.Components {
    public interface IWithResourceCostPreview : IModel {
        ItemTemplate ItemTemplate { get; }
        int CurrentQuantity { get; }
        int RequiredQuantity { get; }
        bool HasRequiredQuantity => CurrentQuantity >= RequiredQuantity;
        
        public static class Events {
            public static readonly Event<IWithResourceCostPreview, bool> ResourceCostRefreshed = new(nameof(ResourceCostRefreshed));
        }
    }
}