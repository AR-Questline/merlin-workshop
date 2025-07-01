using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Maths;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Map.Markers {
    [UsesPrefab("CharacterSheet/Map/" + nameof(VQuestAreaSphereMapMarker))]
    public class VQuestAreaSphereMapMarker : VSpriteMapMarker<QuestAreaMapMarker> {
        [SerializeField] Vector2 spriteSize = new(2f, 2f);
        [SerializeField] SpriteRenderer areaRenderer;
        
        Transform _spriteRendererTransform;
        
        protected override void Awake() {
            base.Awake();
            _spriteRendererTransform = SpriteRenderer.transform;
        }
        
        protected override void OnInitialize() {
            base.OnInitialize();
            InitArea();
        }
        
        protected override void UpdateMarkerScale(float heightWorldPercent) {
            var heightScale = heightWorldPercent / spriteSize.y;
            _spriteRendererTransform.localScale = heightScale.UniformVector3();
        }

        void InitArea() {
            if (Target.ShowArea) {
                var area = (LocationAreaSphere)Target.Area;
                var sprite = areaRenderer.sprite;
                var diameterInMeters = area.Radius * 2;
                var diameterInPixels = diameterInMeters * sprite.pixelsPerUnit;
                var scale = diameterInPixels / sprite.texture.width;
                areaRenderer.transform.localScale = scale.UniformVector3();
                areaRenderer.gameObject.SetActive(true);
            } else {
                areaRenderer.gameObject.SetActive(false);
            }
        }
    }
}