using System.Collections.Generic;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.CharacterSheet.Map.Markers;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Maths;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Map {
    public class VCMapGamepadHelper : ViewComponent<MapUI> {
        [SerializeField] Image _gamepadSelector;
        
        PointerEventData _pointerEventData;
        readonly List<RaycastResult> _markerUnderRaycastResults = new(32);

        protected override void OnAttach() {
            UpdateSelector();
        }

        void Update() {
            if (Target?.HasBeenDiscarded ?? true) {
                return;
            }
            UpdateSelector();
            UpdateSnap();
        }

        void UpdateSelector() { _gamepadSelector.color = RewiredHelper.IsGamepad ? Color.white : Color.clear; }
        
        void UpdateSnap() {
            if (!RewiredHelper.IsGamepad) {
                return;
            }

            var mapSceneUI = Target.TryGetElement<MapSceneUI>();
            var pointedMarker = mapSceneUI?.PointedMarker;
            var vMapMarker = pointedMarker?.View<IVMapMarker>();
            if (vMapMarker == null || vMapMarker.HasBeenDiscarded) {
                return;
            }
            
            Vector3 markerWorldPosition = ((RectTransform)vMapMarker.transform).anchoredPosition;
            var minMaxRect = mapSceneUI.View<VMapSceneUI>().MinMaxRect;
            var bounds = mapSceneUI.Data.Bounds;
            markerWorldPosition.x = markerWorldPosition.x.Remap(minMaxRect.xMin, minMaxRect.xMax, bounds.min.x, bounds.max.x);
            markerWorldPosition.z = markerWorldPosition.y.Remap(minMaxRect.yMin, minMaxRect.yMax, bounds.min.z, bounds.max.z);
            markerWorldPosition.y = 0;
            var snapPower = mapSceneUI.GamepadTranslationSpeed * Services.Get<GameConstants>().mapGamepadSnapPower;
            snapPower *= snapPower;
            
            var difference = markerWorldPosition - mapSceneUI.WorldPosition.ToHorizontal3();
            if (difference.sqrMagnitude < snapPower && difference.sqrMagnitude > 0.01f) {
                mapSceneUI.ChangeTranslation(difference);
                mapSceneUI.PointingTo(vMapMarker.Target);
            }
        }
    }
}
