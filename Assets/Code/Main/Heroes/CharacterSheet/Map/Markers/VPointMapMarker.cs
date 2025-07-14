using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Maths;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Map.Markers {
    [UsesPrefab("CharacterSheet/Map/" + nameof(VPointMapMarker))]
    public class VPointMapMarker : VSpriteMapMarker<PointMapMarker> {
        protected override void UpdateMarkerScale(float desiredScale) {
            RectTransform.localScale = desiredScale.UniformVector3();
        }
    }
}