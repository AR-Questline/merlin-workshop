using Awaken.TG.Assets;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Crosshair {
    public class CustomCrosshairAttachment : MonoBehaviour {
        [ARAssetReferenceSettings(new[] {typeof(Sprite), typeof(Texture)})]
        public SpriteReference spriteReference;
        public CrosshairLayer layer;

        public CustomCrosshairPart SpawnCustomCrosshairPart() {
            return new CustomCrosshairPart(spriteReference, layer);
        }
    }
}