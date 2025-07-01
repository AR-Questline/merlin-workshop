using System.Collections.Generic;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Components {
    public class ARSelectable : Selectable, IUIAwareContainer {
        List<IUIAware> _listeners = new List<IUIAware>();
        public IReadOnlyList<IUIAware> UIAwares => _listeners;
        
        public void RegisterUIAware(IUIAware aware) {
            _listeners.Add(aware);
        }

        public void RemoveUIAware(IUIAware uiAware) {
            _listeners.Remove(uiAware);
        }

        public UIResult Handle(UIEvent evt) {
            return UIResult.Ignore;
        }
    }
}