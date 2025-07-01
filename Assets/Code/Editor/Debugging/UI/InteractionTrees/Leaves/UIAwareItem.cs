using Awaken.TG.Editor.Debugging.UI.InteractionTrees.Containers;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.Utility.GameObjects;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging.UI.InteractionTrees.Leaves {
    public class UIAwareItem : IHandlerItem {
        public IUIAware Aware { get; }
        public AwareWithHandlers Parent { get; }

        public string Name { get; }
        public UIResult? Result { get; set; }

        public UIAwareItem(IUIAware aware, AwareWithHandlers parent) {
            Aware = aware;
            Parent = parent;
            Name = ItemName(aware);
        }
        
        public static string ItemName(IUIAware aware) {
            return aware switch {
                IModel model => model.ContextID,
                IView view => view.ID,
                IService service => service.GetType().Name,
                Component component => component.gameObject.PathInSceneHierarchy(),
                _ => aware.GetType().Name
            };
        }
    }
}