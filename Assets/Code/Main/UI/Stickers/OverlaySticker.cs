using UnityEngine;

namespace Awaken.TG.Main.UI.Stickers {
    public class OverlaySticker : Sticker {
        public static OverlaySticker Create(GameObject prefab) {
            GameObject gob = Instantiate(prefab);
            OverlaySticker sticker = gob.GetComponent<OverlaySticker>();
            return sticker;
        }

        public override void Realign(Camera referenceCamera, Canvas referenceCanvas) {}
    }
}