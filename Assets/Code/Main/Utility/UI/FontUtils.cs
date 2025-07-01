using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;
using TMPro;

namespace Awaken.TG.Main.Utility.UI {
    public static class FontUtils {
        public static void UnifyFontSize(this IEnumerable<TextMeshProUGUI> texts) {
            float size = texts.Select(text => text.fontSize).Min();
            texts.ForEach(text => {
                text.enableAutoSizing = false;
                text.fontSize = size;
            });
        }

        [UnityEngine.Scripting.Preserve]
        public static async UniTaskVoid UnifyFontSizeDelayed(this IEnumerable<TextMeshProUGUI> texts, int delay) {
            await UniTask.DelayFrame(delay);
            UnifyFontSize(texts);
        }
    }
}