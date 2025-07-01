using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.IL2CPP.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.Utility.Maths {
    [Il2CppEagerStaticClassConstruction]
    public static class RectTransformUtil {
        static Vector3[] s_worldCorners = new Vector3[4];
        
        public static void GetWorldCorners2D(this RectTransform rect, out Vector2 min, out Vector2 max) {
            rect.GetWorldCorners(s_worldCorners);

            min = max = new Vector2(s_worldCorners[0].x, s_worldCorners[0].y);

            for (int i = 0; i < 4; i++) {
                if (s_worldCorners[i].x < min.x) {
                    min.x = s_worldCorners[i].x;
                }
                if (s_worldCorners[i].y < min.y) {
                    min.y = s_worldCorners[i].y;
                }
                
                if (s_worldCorners[i].x > max.x) {
                    max.x = s_worldCorners[i].x;
                }
                if (s_worldCorners[i].y > max.y) {
                    max.y = s_worldCorners[i].y;
                }
            }
        }
        
        public static Bounds CalculateBoundsOfRectTransform(IEnumerable<RectTransform> rectCollection) {
            Vector2 min = Vector2.positiveInfinity;
            Vector2 max = Vector2.negativeInfinity;

            foreach (var rectTransform in rectCollection) {
                rectTransform.GetWorldCorners2D(out var rMin, out var rMax);
                min = new Vector2(math.min(min.x, rMin.x), math.min(min.y, rMin.y));
                max = new Vector2(math.max(max.x, rMax.x), math.max(max.y, rMax.y));
            }
            
            Bounds bounds = new();
            bounds.SetMinMax(min, max);
            return bounds;
        }
        
        public static Bounds CalculateBoundsOfRectTransform(RectTransform rect) {
            rect.GetWorldCorners2D(out var rMin, out var rMax);
            
            Bounds bounds = new();
            bounds.SetMinMax(rMin, rMax);
            return bounds;
        }

        public static Vector3 WorldBottomLeftCorner(this RectTransform rectTransform) {
            var rect = rectTransform.rect;
            var localCorner = new Vector3(rect.x, rect.y, 0.0f);
            return rectTransform.localToWorldMatrix.MultiplyPoint(localCorner);
        }
        
        public static Vector3 WorldTopRightCorner(this RectTransform rectTransform) {
            var rect = rectTransform.rect;
            var localCorner = new Vector3(rect.xMax, rect.yMax, 0.0f);
            return rectTransform.localToWorldMatrix.MultiplyPoint(localCorner);
        }

        public static void RebuildUpTo(this RectTransform from, RectTransform to) {
            while (from != null && from != to) {
                LayoutRebuilder.ForceRebuildLayoutImmediate(from);
                from = from.parent as RectTransform;
            }
        }
        public static async UniTaskVoid RebuildAllBelowInverse(this RectTransform from, MonoBehaviour coroutineRunner) {
            await UniTask.WaitForEndOfFrame(coroutineRunner);
            RebuildAllBelowInverse_Internal(from);
        }
        public static void RebuildAllBelowInverse(this RectTransform from) {
            RebuildAllBelowInverse_Internal(from);
        }
        static void RebuildAllBelowInverse_Internal(RectTransform from) {
            foreach (RectTransform rect in from) {
                RebuildAllBelowInverse(rect);
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            }
        }
    }
}