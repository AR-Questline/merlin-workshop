using Awaken.Utility.Debugging;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Graphics.Water {
    [RequireComponent(typeof(WaterSurface))]
    public class WaterSurfaceRegionAnchorController : MonoBehaviour {
        WaterSurface _waterSurface;
        bool _anchorOverriden;
        Transform _originalAnchor;

        void Awake() {
            _waterSurface = GetComponent<WaterSurface>();
        }
        
        public void OverrideRegionAnchor(Transform anchor) {
            if (_anchorOverriden) {
                Log.Minor?.Error($"Attempting to override water surface region anchor with \"{anchor.name}\" while it is already overriden with \"{_waterSurface.decalRegionAnchor}\". Ignoring.");
                return;
            }
            _originalAnchor = _waterSurface.decalRegionAnchor;
            _anchorOverriden = true;
            _waterSurface.decalRegionAnchor = anchor;
        }

        public void ResetRegionAnchor() {
            _anchorOverriden = false;
            _waterSurface.decalRegionAnchor = _originalAnchor;
            _originalAnchor = null;
        }
    }
}