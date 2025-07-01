//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityInternal = UnityEngine.Internal;

namespace ChocDino.UIFX
{
	public enum FrustmIntersectResult
	{
		/// <summary>The object is completely outside of the planes.</summary>
		Out,
		/// <summary>The object is completely inside of the planes.</summary
		In,
		/// <summary>The object is partially intersecting the planes.</summary>
		Partial,
	}

	[UnityInternal.ExcludeFromDocs]
	public static class MathUtils
	{
		public static FrustmIntersectResult GetFrustumIntersectsOBB(Plane[] planes, Vector3[] points)
        {
            return default;
        }

        [UnityInternal.ExcludeFromDocs]
        public static float Snap(float v, float snap)
        {
            return default;
        }

        /// <summary> 
        /// Adds padding to a number and then rounds up to the nearest multiple.
        /// This is useful for textures to ensure they have constant minimum padding amount, but also have a width/height that is a multiple size.
        /// This can allow a texture size that is frequently changing slightly (eg when filter sizes change) to not reallocate too frequently, and
        /// can stabilise flickering caused when downsampling very small which can cause textures to oscilate between odd/even sizes causing the
        /// texture sampling is jump around between frames and flicker.
        /// Eg params [9,10,10] = 20, [10,10,10] = 20, [11,10,10] = 30
        /// <summary>
        [UnityInternal.ExcludeFromDocs]
        public static int PadAndRoundToNextMultiple(float v, int pad, int multiple)
        {
            return default;
        }

        [UnityInternal.ExcludeFromDocs]
        // Based on https://www.rorydriscoll.com/2016/03/07/frame-rate-independent-damping-using-lerp/
        public static float GetDampLerpFactor(float lambda, float deltaTime)
        {
            return default;
        }

        [UnityInternal.ExcludeFromDocs]
        public static float DampTowards(float a, float b, float lambda, float deltaTime)
        {
            return default;
        }

        [UnityInternal.ExcludeFromDocs]
		public static Vector2 DampTowards(Vector2 a, Vector2 b, float lambda, float deltaTime)
        {
            return default;
        }

        [UnityInternal.ExcludeFromDocs]
		public static Vector3 DampTowards(Vector3 a, Vector3 b, float lambda, float deltaTime)
        {
            return default;
        }

        [UnityInternal.ExcludeFromDocs]
		public static Vector4 DampTowards(Vector4 a, Vector4 b, float lambda, float deltaTime)
        {
            return default;
        }

        [UnityInternal.ExcludeFromDocs]
		public static Color DampTowards(Color a, Color b, float lambda, float deltaTime)
        {
            return default;
        }

        [UnityInternal.ExcludeFromDocs]
		public static Matrix4x4 DampTowards(Matrix4x4 a, Matrix4x4 b, float lambda, float deltaTime)
        {
            return default;
        }

        [UnityInternal.ExcludeFromDocs]
		public static Matrix4x4 LerpUnclamped(Matrix4x4 a, Matrix4x4 b, float t, bool preserveScale)
        {
            return default;
        }

        [UnityInternal.ExcludeFromDocs]
        public static void LerpUnclamped(ref Matrix4x4 result, Matrix4x4 b, float t, bool preserveScale)
        {
        }

        /// <summary>
        /// Lerp between 3 values (a, b, c) using t with range [0..1]
        /// </summary>
        [UnityInternal.ExcludeFromDocs]
        public static float Lerp3(float a, float b, float c, float t)
        {
            return default;
        }

        [UnityInternal.ExcludeFromDocs]
        public static bool HasMatrixChanged(Matrix4x4 a, Matrix4x4 b, bool ignoreTranslation)
        {
            return default;
        }

        [UnityInternal.ExcludeFromDocs]
        public static void CreateRandomIndices(ref int[] array, int length)
        {
        }

        /// <summary>
        /// Given two rectangles in absolute coordinates, return the rectangle such that if src is remapped to range [0..1] relative to dst
        /// If src and dst are the same, rect(0, 0, 1, 1) will be returned
        /// If src is within dst, then rect values will be > 0 and < 1
        /// If src is larger than dst, rect values will be < 0 and > 1
        /// The returned rect could be used to offset and scale UV coordinates from one quad to another
        /// </summary>
        public static Rect GetRelativeRect(Rect src, Rect dst)
        {
            return default;
        }

        /// <summary>Move rect horizontally so that specific point along it's width matches the equivelent point along target's width. This is useful for snapping the ege of a rectangle to the edge of another.</summary>
        private static Rect SnapRectToRectHoriz(Rect rect, Rect target, float sizeT)
        {
            return default;
        }

        /// <summary>Move rect vertically so that specific point along it's height matches the equivelent point along target's height. This is useful for snapping the ege of a rectangle to the edge of another.</summary>
        private static Rect SnapRectToRectVert(Rect rect, Rect target, float sizeT)
        {
            return default;
        }

        /// <summary>
        /// Snap one rectangle to the edge of another (or fractional positions between)
        /// widthT 0 is left, widthT 1 is right
        /// heightT 0 is bottom, heightT 1 is top
        /// </summary>
        public static Rect SnapRectToRectEdges(Rect rect, Rect target, bool applyWidth, bool applyHeight, float widthT, float heightT)
        {
            return default;
        }

        /// <summary>Return rectangle of aspect ratio using the scaling mode</summary>
        public static Rect ResizeRectToAspectRatio(Rect rect, ScaleMode scaleMode, float aspect)
        {
            return default;
        }
    }
}