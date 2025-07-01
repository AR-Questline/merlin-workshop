using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Animations;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Map {
    [UsesPrefab("CharacterSheet/Map/" + nameof(VMapCamera))]
    public class VMapCamera : View<MapUI> {
        public const float MarginSize = 0.05f;
        // Margin size works for orthographic camera. IDK why for real world size multiplier is 21, but it works.  
        public const float MarginSizeWorldMultiplier = 21;
        [SerializeField] Camera markersCamera;

        public Camera MarkersCamera => markersCamera;

        protected override void OnInitialize() {
            Target.ListenTo(MapSceneUI.Events.ParametersChanged, UpdateZoomAndTranslation, this);
        }

        protected override IBackgroundTask OnDiscard() {
            markersCamera.targetTexture = null;
            return base.OnDiscard();
        }

        void UpdateZoomAndTranslation(MapSceneUI mapScene) {
            var currentOrthoSize = GetOrthoSize(mapScene.Zoom, mapScene.Data.Bounds.size, mapScene.Data.AspectRatio);
            transform.position = mapScene.WorldPosition;
            markersCamera.orthographicSize = currentOrthoSize;
        }

        public static float GetOrthoSize(float zoom, in Vector3 boundsSize, float mapAspectRatio) {
            float maxZ = math.max(boundsSize.z, boundsSize.x / mapAspectRatio);
            float maxSize = maxZ / 2;
            return (zoom + MarginSize) * maxSize;
        }
    }
}
