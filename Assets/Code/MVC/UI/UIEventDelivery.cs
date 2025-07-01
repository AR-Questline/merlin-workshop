using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers;

namespace Awaken.TG.MVC.UI {
    public struct UIEventDelivery {
        public IUIAware responsibleObject;
        public UIEvent handledEvent;
        public UIResult finalResult;
    }
}