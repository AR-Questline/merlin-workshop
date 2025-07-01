using Awaken.TG.Main.UI;
using Awaken.TG.MVC;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Graphics.FloatingTexts {
    /// <summary>
    /// Service which manage FloatingText objects creation
    /// </summary>
    public class FloatingTextService : MonoBehaviour, IService {
        // === References
        public FloatingText prefab;

        // === FloatingText creation
        public FloatingText SpawnGlobalFloatingText(string text, RectTransform target, Transform parent = null) {
            FloatingText floating = Spawn(parent);
            var size = floating.transform.parent.GetComponent<RectTransform>().localScale;
            return floating.Init(text, target.position)
                .ShakeCamera(1.5f, 0.85f, true)
                .BumpScale(0.25f, false, size * 1.1f)
                .SetTextAlpha(0.35f, false, 1, Ease.InQuint)
                .BumpScale(0.15f, true, size * 0.95f)
                .BumpScale(0.15f, true, size)
                .Wait(1f)
                .SetTextAlpha(0.2f, true, 0)
                .FadeOut(0.25f)
                .BumpScale(0.25f, false, size * 0.2f)
                .StartFloating();
        }

        FloatingText Spawn(Transform parent) {
            if (parent == null) {
                parent = World.Services.Get<CanvasService>().MainTransform;
            }

            return Instantiate(prefab, parent);
        }
    }
}