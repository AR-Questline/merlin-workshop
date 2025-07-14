using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Maths;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Map.Markers {
    [UsesPrefab("CharacterSheet/Map/" + nameof(VQuestAreaSphereMapMarker))]
    public class VQuestAreaSphereMapMarker : VSpriteMapMarker<QuestAreaMapMarker> {
        [SerializeField] Image areaRenderer;
        [SerializeField] RectTransform iconParent;

        protected override void AfterFirstCanvasCalculate() {
            InitArea();
        }
        
        protected override void UpdateMarkerScale(float desiredScale) {
            iconParent.localScale = desiredScale.UniformVector3();
        }

        void InitArea() {
            if (Target.ShowArea) {
                var area = (LocationAreaSphere)Target.Area;
                var sprite = areaRenderer.sprite;
                var diameterInMeters = area.Radius * 2;
                var diameterInPixels = diameterInMeters * sprite.pixelsPerUnit;
                var scale = diameterInPixels / sprite.texture.width / areaRenderer.rectTransform.rect.size.x;
                
                areaRenderer.transform.localScale = scale.UniformVector3();
                areaRenderer.gameObject.SetActive(true);
            } else {
                areaRenderer.gameObject.SetActive(false);
            }
        }
    }
}