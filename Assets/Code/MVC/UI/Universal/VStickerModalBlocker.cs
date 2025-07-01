using Awaken.TG.Main.UI.Stickers;
using Awaken.TG.MVC.Attributes;
using UnityEngine;

namespace Awaken.TG.MVC.UI.Universal {
    [UsesPrefab("UI/VStickerModalBlocker")]
    public class VStickerModalBlocker : VModalBlocker {
        public override Transform DetermineHost() {
            return Services.Get<MapStickerUI>().AddOverlaySticker();
        }
    }
}
