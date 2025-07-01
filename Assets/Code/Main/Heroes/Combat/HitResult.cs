using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public struct HitResult {
        public Collider Collider { get; }
        public Vector3 Point { get; }
        public Vector3 Normal { get; }
        public bool Prevented { get; private set; }

        public HitResult(Collider collider, Vector3 point, Vector3 normal, bool prevented = false) {
            Collider = collider;
            Point = point;
            Normal = normal;
            Prevented = prevented;
        }

        [UnityEngine.Scripting.Preserve]
        public static HitResult PreventedResult => new(null, Vector3.zero, Vector3.zero, true);
    }
}