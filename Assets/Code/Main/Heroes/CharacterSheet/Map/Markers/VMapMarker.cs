using Awaken.TG.Assets;
using Awaken.TG.Graphics.MapServices;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility.Animations;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Map.Markers {
    public abstract class VMapMarker<T> : View<T>, IVMapMarker where T : MapMarker {
        protected RectTransform RectTransform { get; private set; }
        MapMarker IView<MapMarker>.Target => Target;
        
        public override Transform DetermineHost() => World.Only<MapSceneUI>().View<VMapSceneUI>().markersParent;
        public MapSceneUI MapSceneUI { get; private set; }

        protected virtual void Awake() {
            RectTransform = (RectTransform)transform;
        }

        public virtual void Init(MapSceneUI mapSceneUI) {
            MapSceneUI = mapSceneUI;
            MapSceneUI.SortMarkers();
            MapSceneUI.AfterFirstCanvasCalculate(() => {
                SetPosition(Target.Position);
                AfterFirstCanvasCalculate();
            });
        }

        protected override void OnInitialize() {
            World.EventSystem.ListenTo(EventSelector.AnySource, MapSceneUI.Events.ParametersChanged, this, UpdateMarker);
            Target.ListenTo(MapMarker.Events.PositionChanged, RefreshPosition, this);
        }
        
        protected virtual void AfterFirstCanvasCalculate() { }

        void UpdateMarker(MapSceneUI mapSceneUI) {
            bool isVisible = UpdateVisibility(mapSceneUI.Scene);
            if (!isVisible) {
                return;
            }
            UpdateScale(mapSceneUI);
            RefreshRotation();
        }

        bool UpdateVisibility(SceneReference scene) {
            bool isVisible = Target.IsAlwaysVisible || World.Services.Get<MapService>().TryGetFogOfWar(scene, out FogOfWar fogOfWar) == false || fogOfWar.IsPositionRevealed(Target.Position);
            gameObject.SetActive(isVisible);
            return isVisible;
        }
        
        void UpdateScale(MapSceneUI mapUI) {
            const float MinMarkerScale = 0.18f;
            const float MaxMarkerScale = 1;
            float desiredScale = mapUI.Zoom.Remap(MapSceneUI.MinZoom, MapSceneUI.MaxZoom, MinMarkerScale, MaxMarkerScale);
            UpdateMarkerScale(desiredScale);
        }

        protected abstract void UpdateMarkerScale(float desiredScale);

        void RefreshPosition(IGrounded grounded) {
            SetPosition(grounded.Coords);
        }

        protected void RefreshRotation() {
            if (Target.Rotate) {
                RectTransform.rotation = Quaternion.Euler(0, 0, 180 - Target.Grounded.Rotation.eulerAngles.y);
            }
        }

        void SetPosition(Vector3 worldPosition) {
            var bounds = MapSceneUI.Data.Bounds;
            var minMaxRect = MapSceneUI.View<VMapSceneUI>().MinMaxRect;

            Vector3 remappedPosition = worldPosition;
            remappedPosition.x = worldPosition.x.Remap(bounds.min.x, bounds.max.x, minMaxRect.xMin, minMaxRect.xMax);
            remappedPosition.y = worldPosition.z.Remap(bounds.min.z, bounds.max.z, minMaxRect.yMin, minMaxRect.yMax);
            remappedPosition.z = 0;
            
            RectTransform.anchoredPosition = remappedPosition;
        }

        protected override IBackgroundTask OnDiscard() {
            MapSceneUI = null;
            return base.OnDiscard();
        }
    }
}
