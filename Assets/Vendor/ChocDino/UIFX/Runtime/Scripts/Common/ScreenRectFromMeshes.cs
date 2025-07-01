//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChocDino.UIFX
{
	public struct RectAdjustOptions
	{
		public int padding;
		public int roundToNextMultiple;
		public bool clampToScreen;
	}

	/// <summary>
	/// Takes a collection of Mesh / VertexHelper and calculates the screen-space rectangle that would encapsulate all of them.
	/// </summary>
	public class ScreenRectFromMeshes
	{
		private bool _isFirstPoint;
		internal Bounds _screenBounds;
		private Rect _screenRect;
		private Rect _localRect;
		private RectInt _textureRect;
		private Camera _camera;
		private FilterRenderSpace _renderSpace;

		private static Vector3[] s_boundsPoints = new Vector3[8];
		private static Plane[] s_planes = new Plane[6];

		// Convert 8 points to 12 lines so we can clip with the camera frustum
		// front: 3,7 0,4, 3,0, 7,4 (FTL,FTR) (FBL,FBR) (FTL,FBL) (FTR,FBR)
		// back : 5,1, 2,6, 5,2 1,6 (BTL,BTR) (BBL,BBR) (BTL,BBL) (BTR,BBR)
		// sides: 5,3 2,0 1,7 6,4 (BTL,FTL) (BBL,FBL) (BTR, FTR) (BBR,FBR)
		private static int[] s_clipLinePairs = new int[] { 3, 7, 0, 4, 3, 0, 7, 4, 5, 1, 2, 6, 5, 2, 1, 6, 5, 3, 2, 0, 1, 7, 6, 4 };

		#if UNITY_EDITOR
		static ScreenRectFromMeshes()
        {
        }
#endif

        public void Start(Camera camera, FilterRenderSpace renderSpace)
        {
        }

        public void AddMeshBounds(Transform xform, Mesh mesh)
        {
        }

        public void AddVertexBounds(Transform xform, Vector3[] verts, int vertexCount)
        {
        }

        public void AddVertexBounds(Transform xform, VertexHelper verts)
        {
        }

        private void AddBounds(Transform xform, Vector3 boundsMin, Vector3 boundsMax)
        {
        }

        public void End()
        {
        }

        public void Adjust(Vector2Int leftDown, Vector2Int rightUp)
        {
        }

        internal void SetRect(Rect rect)
        {
        }

        public Rect GetRect()
        {
            return default;
        }

        public Rect GetLocalRect()
        {
            return default;
        }

        public RectInt GetTextureRect()
        {
            return default;
        }

        public void OptimiseRects(RectAdjustOptions options)
        {
        }

        private Rect _innerRect;

        public void BuildScreenQuad(Camera camera, Transform xform, float alpha, VertexHelper vh)
        {
        }
    }
}