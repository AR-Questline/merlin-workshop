using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Stories {
    public class StoryDicesDisplay : MonoBehaviour {
        const float RenderTextureUpscaleFactor = 2f;
        // These values are set from code, because when spawning prefab RectTransform don't have set size in the first frame
        const int RawImageWidth = 336;
        const int RawImageHeight = 168;
        [UnityEngine.Scripting.Preserve] public RawImage rawImage;
        
        [UnityEngine.Scripting.Preserve]
        public Vector2 RawImageSize() {
            return new Vector2(RawImageWidth * RenderTextureUpscaleFactor, RawImageHeight * RenderTextureUpscaleFactor);
        }
    }
}
