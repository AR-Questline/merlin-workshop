using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Relations;

namespace Awaken.TG.Main.Heroes.Items {
    public interface IItemOwnerRelatedElement : IElement<Item> {
        public void AfterOwnerAdded(RelationEventData data);
        public void BeforeOwnerRemoved(RelationEventData data);
    }
}
