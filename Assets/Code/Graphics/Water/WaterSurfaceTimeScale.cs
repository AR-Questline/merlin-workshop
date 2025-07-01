using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Graphics.Water {
    [RequireComponent(typeof(WaterSurface))]
    public class WaterSurfaceTimeScale : MonoBehaviour {
        WaterSurface _waterSurface;
        float _waterTimeMultiplier;

        void Awake() {
            _waterSurface = GetComponent<WaterSurface>();
            _waterTimeMultiplier = _waterSurface.timeMultiplier;
        }

        void Update() {
            var timeScale = Time.timeScale;
            _waterSurface.timeMultiplier = timeScale <= 1
                ? Mathf.Lerp(0, _waterTimeMultiplier, timeScale)
                : Mathf.Lerp(_waterTimeMultiplier, _waterTimeMultiplier * timeScale, timeScale * 0.1f);
        }
    }
}
