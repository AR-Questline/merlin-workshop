using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Maths;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Map.Markers {
    [UsesPrefab("CharacterSheet/Map/" + nameof(VPointMapMarker))]
    public class VPointMapMarker : VSpriteMapMarker<PointMapMarker> {
        [SerializeField] Vector2 spriteSize = new(2f, 2f);
        
        protected override void UpdateMarkerScale(float heightWorldPercent) {
            var heightScale = heightWorldPercent / spriteSize.y;
            Transform.localScale = heightScale.UniformVector3();
        }
    }
}