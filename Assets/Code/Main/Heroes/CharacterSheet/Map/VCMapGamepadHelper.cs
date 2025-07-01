using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.CharacterSheet.Map.Markers;
using Awaken.TG.Main.Rendering;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.Maths;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Map {
    public class VCMapGamepadHelper : ViewComponent<MapUI> {
        [SerializeField] Image _gamepadSelector;

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
            if (!RewiredHelper.IsGamepad || !Target.MarkersCamera) {
                return;
            }

            var mapSceneUI = Target.TryGetElement<MapSceneUI>();
            if (mapSceneUI == null) {
                return;
            }
            
            var centerRay = Target.MarkersCamera.ViewportPointToRay(0.5f.UniformVector3());

            if (!Physics.Raycast(centerRay, out var hit, 900, RenderLayers.Mask.MapMarker)) {
                return;
            }
            var boundsCenter = hit.collider.bounds.center;
            var snapPower = mapSceneUI.GamepadTranslationSpeed * Services.Get<GameConstants>().mapGamepadSnapPower;
            snapPower *= snapPower;

            var difference = boundsCenter.XZ()-centerRay.origin.XZ();
            if (difference.sqrMagnitude < snapPower) {
                var vMapMarker = hit.collider.transform.GetComponentInParent<IVMapMarker>();
                if (vMapMarker.HasBeenDiscarded) {
                    return;
                }
                mapSceneUI.ChangeTranslation(difference.X0Y());
                mapSceneUI.PointingTo(vMapMarker.Target);
            }
        }
    }
}
