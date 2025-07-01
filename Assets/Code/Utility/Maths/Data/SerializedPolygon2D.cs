using System;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Utility.Maths.Data {
    [Serializable]
    public struct SerializedPolygon2D {
        [SerializeField] Vector2[] polygonLocalPoints;
        
        public SerializedPolygon2D(Vector3[] polygonLocalPoints) {
            this.polygonLocalPoints = new Vector2[polygonLocalPoints.Length];
            for (var i = 0; i < polygonLocalPoints.Length; i++) {
                this.polygonLocalPoints[i] = polygonLocalPoints[i].XZ();
            }
        }
        
        public readonly bool IsEmpty => polygonLocalPoints == null || polygonLocalPoints.Length == 0;
        
        public UnsafeArray<float2> PolygonPoints(Transform transform, Allocator allocator = Allocator.Persistent) {
            var localToWorld = transform.localToWorldMatrix;
            var result = new UnsafeArray<float2>((uint)polygonLocalPoints.Length, allocator);
            for (var i = 0u; i < result.Length; i++) {
                result[i] = localToWorld.MultiplyPoint3x4(polygonLocalPoints[i].X0Y()).XZ();
            }
            return result;
        }
        
        public Polygon2D ToPolygon(Transform transform, Allocator allocator = Allocator.Persistent) {
            var points = PolygonPoints(transform, allocator);
            Polygon2DUtils.Bounds(points, out var bounds);
            return new(points, bounds);
        }
    }
}