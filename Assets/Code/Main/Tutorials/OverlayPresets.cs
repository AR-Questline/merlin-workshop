using Awaken.TG.Main.Tutorials.Views;
using Awaken.TG.Utility.Graphics;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Tutorials {
    public enum OverlayPreset {
        None,
        RedCircle,
        RedRectangleSliced,
    }

    public class OverlayPresets {

        public static void SetupPreset(OverlayPreset preset, VGreyOverlay overlay, Texture2D texture, OverlaySettings.HoleSpec holeSpec, Camera camera) {
            if (preset == OverlayPreset.RedCircle) {
                SetupRedCircle(overlay, texture, holeSpec, camera);
            } else if (preset == OverlayPreset.RedRectangleSliced) {
                SetupRedRect(overlay, texture, holeSpec, camera);
            }
        }

        static void SetupRedCircle(VGreyOverlay overlay, Texture2D texture, OverlaySettings.HoleSpec hole, Camera camera) {
            texture.CutHole(hole.area, camera, OverlaySettings.HoleSpec.DefaultMask, hole.scaleFactor);
            texture.Stick(hole.area, camera, OverlaySettings.StickerSpec.DefaultMask, hole.scaleFactor);
        }

        static void SetupRedRect(VGreyOverlay overlay, Texture2D texture, OverlaySettings.HoleSpec hole, Camera camera) {
            texture.CutHole(hole.area, camera, null, hole.scaleFactor);
            
            GameObject stickerGo = new GameObject("Sticker");
            stickerGo.transform.SetParent(overlay.transform, false);
            RectTransform rectTrans = stickerGo.AddComponent<RectTransform>();
            rectTrans.rotation = Quaternion.identity;
            rectTrans.localScale = Vector3.one;
            stickerGo.AddComponent<CanvasRenderer>();
            Image image = stickerGo.AddComponent<Image>();
            image.sprite = OverlaySettings.StickerSpec.RectMask;
            image.type = Image.Type.Sliced;
            Rect rect = TextureUtils.GetScreenPointRectOverlay(hole.area, overlay.GetComponentInParent<Canvas>());
            float x = (rect.x + rect.width / 2) / Screen.width;
            float y = (rect.y + rect.height / 2) / Screen.height;
            rectTrans.anchorMin = new Vector2(x, y);
            rectTrans.anchorMax = new Vector2(x, y);
            rectTrans.pivot = Vector2.one * 0.5f;

            Vector2 refResolution = overlay.GetComponentInParent<CanvasScaler>().referenceResolution;
            float width = (rect.width / Screen.width) * refResolution.x;
            float height = (rect.height / Screen.height) * refResolution.y;

            rectTrans.sizeDelta = new Vector2(width, height);
            rectTrans.localScale *= hole.scaleFactor;
        }
    }
}