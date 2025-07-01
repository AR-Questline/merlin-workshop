using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.MVC.Attributes;

namespace Awaken.TG.Main.UI.Popup {
    [UsesPrefab("Story/" + nameof(VSmallStoryPopupUI))]
    public class VSmallStoryPopupUI : VSmallPopupUIBase<Story>, IVStoryPanel  {
        public void ClearText() { }
    }
}