using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Video {
    [UsesPrefab("UI/Video/VVideoBlackBackground")]
    public class VVideoBlackBackground : View<VideoBlackBackground> {
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
    }
}