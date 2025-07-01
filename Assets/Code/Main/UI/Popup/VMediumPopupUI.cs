using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;

namespace Awaken.TG.Main.UI.Popup {
    public class VMediumPopupUI<TParent> : VPopupUI<TParent> where TParent : IModel { }
    [UsesPrefab("Story/" + nameof(VMediumPopupUI))]
    public class VMediumPopupUI : VMediumPopupUI<PopupUI> { }
}
