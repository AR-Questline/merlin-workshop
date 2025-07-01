using Awaken.Utility.Maths.Data;
using Unity.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Grounds.CullingGroupSystem {
    public interface IAreaCullingDistanceModifier : ICullingDistanceModifier {
        public string name { get; }
        public GameObject gameObject { get; }
        
        public Polygon2D ToPolygon(Allocator allocator);
    }
}