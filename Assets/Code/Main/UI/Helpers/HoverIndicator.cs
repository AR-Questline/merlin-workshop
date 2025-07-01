using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.Hovers;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Main.UI.Helpers
{
    public class HoverIndicator : ViewComponent<Model> {

        // === References

        Light _light;
        float _baseIntensity;

        // === State

        Tweener _activeTween;

        // === Initialization

        protected override void OnAttach() {
            // grab the light
            _light = GetComponent<Light>();
            _baseIntensity = _light.intensity;
            _light.intensity = 0;
            _light.enabled = false;
            // register for events
            ParentView.ListenTo(Hovering.Events.HoverChanged, UpdateHover, this);
        }

        // === Updating state

        public void UpdateHover(HoverChange e) {
            // complete any previous tweens
            _activeTween?.Complete(withCallbacks: false);
            // tween to new state
            bool lightEnabled = e.Hovered;
            if (lightEnabled && !_light.enabled) _light.enabled = true;
            _activeTween = _light
                .DOIntensity(e.Hovered ? _baseIntensity : 0f, 0.2f)
                .OnComplete(() => _light.enabled = lightEnabled);
        }
    }
}
