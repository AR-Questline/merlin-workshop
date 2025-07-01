using UnityEngine;

namespace Awaken.TG.Main.Locations.Discovery {
    public class AxisAlignedBoxPrimitive : IAreaPrimitive {
        Bounds _bounds;

        public AxisAlignedBoxPrimitive(Bounds bounds) {
            _bounds = bounds;
        }

        public bool Contains(Vector3 point) {
            return _bounds.Contains(point);
        }

        public Bounds Bounds => _bounds;
    }
}