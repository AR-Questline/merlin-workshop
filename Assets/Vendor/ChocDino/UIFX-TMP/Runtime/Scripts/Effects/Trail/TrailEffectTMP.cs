//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

#if UIFX_TMPRO

using System.Collections.Generic;
using UnityEngine;
using UnityInternal = UnityEngine.Internal;
using TMPro;

namespace ChocDino.UIFX
{
	// NOTE: Since TMP doesn't derive from Graphic, we could, and so instead of modify the TMP mesh, we could just generate the mesh using OnPopulateMesh() which may be simpler!
	// However, we would then need to render using TMP materials, which may be more difficult...

	/// <summary>
	/// This component is an effect for Text Mesh Pro which renders a trail that follows
	/// the motion of the TMP_Text component.
	/// </summary>
	/// <inheritdoc/>
	[RequireComponent(typeof(TMP_Text))]
	[HelpURL("https://www.chocdino.com/products/unity-assets/")]
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Effects/UIFX - Trail TMP")]
	public class TrailEffectTMP : TrailEffectBase
	{
		private TMP_Text _textMeshPro;

		// NOTE: Usually it's fine to just modify the TMP mesh, but when there is ANOTHER script that's modifying the TMP mesh then there will be a conflict because this script increases the number of vertices.
		// In that case the CanvasRenderer can be set to render our own mesh.
		public enum TargetMesh
		{
			// Modifies the TMP mesh
			TextMeshPro,

			// Doesn't modify the TMP mesh, instead assigns a new mesh to the CanvasRenderer
			Internal,
		}

		private class LayerVertices
		{
			public Vector3[] positions;
			public Vector2[] uvs0;
			public Vector4[] uvs0_v4;
			public Vector2[] uvs1;
			public Color32[] colors;

			public LayerVertices(int vertexCount)
            {
            }

            public int VertexCount { get { return positions.Length; } }

            public void CopyTo(LayerVertices dst)
            {
            }

            public void CopyTo(LayerVertices dst, int dstOffset, int count)
            {
            }

            public void CopyTo(int srcOffset, LayerVertices dst, int dstOffset, int count)
            {
            }
        }
	
		private class TrailLayer
		{
			internal LayerVertices vertices;
			internal Matrix4x4 matrix;
			internal Color color;
			internal float alpha;
		}

		private LayerVertices _originalVerts;
		private LayerVertices _outputVerts;

		// TrailLayer index 0..trailCount order = newest..oldest, front..back
		private List<TrailLayer> _layers = new List<TrailLayer>(16);

		private bool _appliedLastFrame = false;
		private static bool s_isUV0Vector4 = false;

		private TargetMesh _targetMesh = TargetMesh.TextMeshPro;
		private int[] _triangleIndices;
		private Mesh _mesh;

		static TrailEffectTMP()
        {
        }

        [UnityInternal.ExcludeFromDocs]
		protected override void Awake()
        {
        }

        [UnityInternal.ExcludeFromDocs]
		protected override void OnEnable()
        {
        }

        [UnityInternal.ExcludeFromDocs]
		protected override void OnDisable()
        {
        }

        /// <inheritdoc/>
        public override void ResetMotion()
        {
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
        }
#endif

        private void ForceMeshBackToOriginal()
        {
        }

        private bool HasGeometryToProcess()
        {
            return default;
        }

        void LateUpdate()
        {
        }

        void ModifyGeometry(TMP_TextInfo textInfo)
        {
        }

        protected override void SetDirty()
        {
        }

        void StoreOriginalVertices(TMP_TextInfo textInfo)
        {
        }

        void SetupLayer(TrailLayer layer, int layerIndex)
        {
        }

        private void SetupLayerVertices(TrailLayer layer, int layerIndex)
        {
        }

        void AddTrailLayer()
        {
        }

        protected override void OnChangedVertexModifier()
        {
        }

        private void PrepareTrail()
        {
        }

        void InterpolateTrail()
        {
        }

        void UpdateTrailColors()
        {
        }

        void GenerateTrailGeometry()
        {
        }

        void AssignTrailGeometryToMesh(TMP_TextInfo textInfo)
        {
        }

        private int _lastRenderFrame = -1;

		void WillRenderCanvases()
        {
        }

        private void PrepareMesh(TMP_TextInfo textInfo)
        {
        }

        private void ApplyOutputMeshAndMaterial(TMP_TextInfo textInfo)
        {
        }
    }
}
#endif