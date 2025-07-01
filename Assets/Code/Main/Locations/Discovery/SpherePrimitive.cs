using UnityEngine;

namespace Awaken.TG.Main.Locations.Discovery {
    public class SpherePrimitive : IAreaPrimitive {
        Vector3 _center;
        float _radius;
        float _radiusSq;

        public SpherePrimitive(Vector3 center, float radius) {
            _center = center;
            _radius = radius;
            _radiusSq = radius * radius;
        }

        public bool Contains(Vector3 point) {
            return (point - _center).sqrMagnitude < _radiusSq;
        }

        public Bounds Bounds {
            get {
                float size = 2 * _radius;
                return new Bounds(_center, new Vector3(size, size, size));
            }
        }
    }
}