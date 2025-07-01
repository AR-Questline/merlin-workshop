//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityInternal = UnityEngine.Internal;

namespace ChocDino.UIFX
{
	public enum SkewDirection
	{
		Horizontal,
		Vertical,
	}

	public enum SkewPivotBounds
	{
		Mesh,
		Quads,
	}

	/// <summary>
	/// Apply an affine skew transform to the vertex positions of a UGUI component
	/// </summary>
	[RequireComponent(typeof(Graphic))]
	[ExecuteInEditMode]
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Effects/UIFX - Vertex Skew")]
	public class VertexSkew : UIBehaviour, IMeshModifier
	{
		[SerializeField] SkewPivotBounds _pivotBounds = SkewPivotBounds.Mesh;

		[SerializeField] TextAnchor _pivot = TextAnchor.MiddleCenter;

		[SerializeField] SkewDirection _direction = SkewDirection.Vertical;

		[Range(-90f, 90f)]
		[SerializeField] float _angle = 16f;

		[SerializeField] float _offset = 0f;

		[Range(0f, 1f)]
		[SerializeField] float _strength = 1f;

		private Matrix4x4 _matrix;
		private Vector3 _boundsMin, _boundsMax;
		private Vector3 _pivotPoint;

		public SkewPivotBounds PivotBounds { get { return _pivotBounds; } set { if (value != _pivotBounds) { _pivotBounds = value; ForceVerticesUpdate(); } } }
		public float Angle { get { return _angle; } set { value = Mathf.Clamp(value, -90f, 90f); if (value != _angle) { _angle = value; ForceVerticesUpdate(); } } }
		public float Offset { get { return _offset; } set { if (value != _offset) { _offset = value; ForceVerticesUpdate(); } } }
		public SkewDirection Direction { get { return _direction; } set { if (value != _direction) { _direction = value; ForceVerticesUpdate(); } } }
		public TextAnchor Pivot { get { return _pivot; } set { if (value != _pivot) { _pivot = value; ForceVerticesUpdate(); } } }
		public float Strength { get { return _strength; } set { value = Mathf.Clamp01(value); if (value != _strength) { _strength = value; ForceVerticesUpdate(); } } }

		public Vector3 PivotPoint { get { return _pivotPoint; } }
		public Vector3 BoundsMin { get { return _boundsMin; } }
		public Vector3 BoundsMax { get { return _boundsMax; } }

		private Graphic _graphic;
		private Graphic GraphicComponent { get { if (_graphic == null) _graphic = GetComponent<Graphic>(); return _graphic; } }

		#if UNITY_EDITOR
		protected override void Reset()
        {
        }

        protected override void OnValidate()
        {
        }
#endif

        protected override void OnDisable()
        {
        }

        protected override void OnEnable()
        {
        }

        private void ForceVerticesUpdate()
        {
        }

        private void BuildMatrix()
        {
        }

        [UnityInternal.ExcludeFromDocs]
        public void ModifyMesh(VertexHelper vh)
        {
        }

        private static void GetBounds(VertexHelper vh, out Vector3 min, out Vector3 max)
        {
            min = default(Vector3);
            max = default(Vector3);
        }

        private static void GetBounds(UIVertex[] v, out Vector3 min, out Vector3 max)
        {
            min = default(Vector3);
            max = default(Vector3);
        }

        public static Vector3 GetAnchorPositionForBounds(TextAnchor anchor, Vector3 boundsMin, Vector3 boundsMax)
        {
            return default;
        }

        [UnityInternal.ExcludeFromDocs]
        [System.Obsolete("use IMeshModifier.ModifyMesh (VertexHelper verts) instead", false)]
        public void ModifyMesh(Mesh mesh)
        {
        }
    }
}