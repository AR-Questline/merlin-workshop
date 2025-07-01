using Awaken.TG.Graphics.DayNightSystem;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Awaken.Utility.Maths.Data;
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Grounds.CullingGroupSystem {
    public class RepellerCullingDistanceMultiplier : MonoBehaviour, IAreaCullingDistanceModifier {
        [SerializeField, Required] public WyrdnightSplineRepeller areaProvider;
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
            if (areaProvider != null) {
                return areaProvider.RepellerPolygon.ToPolygon(areaProvider.transform, allocator);
            } else {
                Log.Important?.Error($"Area {name} does not have {nameof(RepellerCullingDistanceMultiplier.areaProvider)} assigned", this);
                return Polygon2D.Invalid;
            }
        }
    }
}