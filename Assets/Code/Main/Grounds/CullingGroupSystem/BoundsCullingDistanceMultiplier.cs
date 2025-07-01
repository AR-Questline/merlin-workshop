using Awaken.TG.MVC;
using Awaken.Utility.Maths.Data;
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Grounds.CullingGroupSystem {
    public class BoundsCullingDistanceMultiplier : MonoBehaviour, IAreaCullingDistanceModifier {
        [SerializeField, Required] GroundBounds bounds;
        [SerializeField, PropertyRange(0.01f, 2f)] float distanceCullingMultiplier = 1;

        public float ModifierValue => distanceCullingMultiplier;
        public bool AllowMultiplierClamp => true;

        void Awake() {
            World.Services.Get<CullingDistanceMultiplierService>().RegisterAreaModifier(this);
        }

        void OnDestroy() {
            World.Services.Get<CullingDistanceMultiplierService>().UnregisterAreaModifier(this);
        }

        public Polygon2D ToPolygon(Allocator allocator) {
            bounds.CalculateGamePolygon(allocator, out var polygon);
            return polygon;
        }
    }
}