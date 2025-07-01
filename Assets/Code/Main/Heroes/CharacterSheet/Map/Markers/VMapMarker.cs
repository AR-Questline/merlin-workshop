using Awaken.TG.Assets;
using Awaken.TG.Graphics.MapServices;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Map.Markers {
    public abstract class VMapMarker<T> : View<T>, IVMapMarker where T : MapMarker {
        const float DefaultYHeight = Ground.BelowGroundHeight * -2f;
        
        protected Transform Transform { get; private set; }
        MapMarker IView<MapMarker>.Target => Target;
        
        public override Transform DetermineHost() {
            return Target.Grounded is Hero ?
                Services.Get<ViewHosting>().DefaultForHero() :
                Services.Get<ViewHosting>().MapMarkersHost();
        }

        protected virtual void Awake() {
            Transform = transform;
        }

        protected override void OnInitialize() {
            SetPosition(Target.Position);

            World.EventSystem.ListenTo(EventSelector.AnySource, MapSceneUI.Events.ParametersChanged, this, UpdateMarker);
            Target.ListenTo(MapMarker.Events.PositionChanged, RefreshPosition, this);
        }

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
            var orthoSize = VMapCamera.GetOrthoSize(mapUI.Zoom, mapUI.Data.Bounds.size, mapUI.Data.AspectRatio);
            var fullHdSize = Services.Get<GameConstants>().mapMarkerFullHdSize;

            var worldScreenHeight = orthoSize * 2;

            var heightScreenPercent = fullHdSize / 1080f;
            var widthScreenPercent = fullHdSize / 1920f;
            
            var heightWorldPercent = heightScreenPercent * worldScreenHeight;
            var widthWorldPercent = widthScreenPercent * worldScreenHeight;
            
            UpdateMarkerScale(math.min(heightWorldPercent, widthWorldPercent));
        }

        protected abstract void UpdateMarkerScale(float height);

        void RefreshPosition(IGrounded grounded) {
            SetPosition(grounded.Coords);
        }

        void RefreshRotation() {
            if (Target.Rotate) {
                Transform.rotation = Quaternion.Euler(90, 0, 180 - Target.Grounded.Rotation.eulerAngles.y);
            }
        }

        void SetPosition(Vector3 coords) {
            var position = coords;
            position.y = DefaultYHeight + Target.Order * 10;
            Transform.position = position;
        }
    }
}
