using System;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Utility.Maths.Data {
    public class Polygon2DAuthoring : MonoBehaviour {
        const int DefaultExtent = 25;

#if UNITY_EDITOR
        [SerializeField] Color _gizmosColor = Color.cyan;
#endif
        [SerializeField] Vector2[] _polygonLocalPoints = Array.Empty<Vector2>();

        void Reset() {
            _polygonLocalPoints = new[] {
                new Vector2(-DefaultExtent, -DefaultExtent),
                new Vector2(+DefaultExtent, -DefaultExtent),
                new Vector2(+DefaultExtent, +DefaultExtent),
                new Vector2(-DefaultExtent, +DefaultExtent),
            };
        }
        
        public UnsafeArray<float2> PolygonPoints(Allocator allocator = Allocator.Persistent) {
            var localToWorld = transform.localToWorldMatrix;
            var result = new UnsafeArray<float2>((uint)_polygonLocalPoints.Length, allocator);
            for (var i = 0u; i < result.Length; i++) {
                result[i] = localToWorld.MultiplyPoint3x4(_polygonLocalPoints[i].X0Y()).XZ();
            }
            return result;
        }

        public UnsafeArray<float2> LocalPolygonPoints(Allocator allocator = Allocator.Persistent) {
            return new UnsafeArray<Vector2>(_polygonLocalPoints, allocator).Move<float2>();
        }

        public Polygon2D ToPolygon(Allocator allocator = Allocator.Persistent) {
            var points = PolygonPoints(allocator);
            Polygon2DUtils.Bounds(points, out var bounds);
            return new(points, bounds);
        }

#if UNITY_EDITOR
        public struct EditorAccess {
            Polygon2DAuthoring _polygon;

            public ref Color GizmosColor => ref _polygon._gizmosColor;
            public ref Vector2[] PolygonLocalPoints => ref _polygon._polygonLocalPoints;

            public EditorAccess(Polygon2DAuthoring polygon) {
                _polygon = polygon;
            }
        }
#endif
    }
}
