//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

#if UIFX_TMPRO

#if UNITY_2022_3_OR_NEWER
	#define UIFX_SUPPORTS_VERTEXCOLORALWAYSGAMMASPACE
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityInternal = UnityEngine.Internal;
using TMPro;
using ChocDino.UIFX;
using UnityEngine.TextCore.Text;

namespace ChocDino.UIFX
{
	/// <summary>
	/// Allows multiple image filters derived from FilterBase to be applied to TextMeshPro
	/// Tested with TextMeshPro v2.1.6 (Unity 2019), v3.0.8 (Unity 2020), 3.2.0-pre.9 (Unity 2022)
	/// </summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(TextMeshProUGUI)), DisallowMultipleComponent]
	[HelpURL("https://www.chocdino.com/products/unity-assets/")]
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Filters/UIFX - Filter Stack (TextMeshPro)", 200)]
	public class FilterStackTextMeshPro : UIBehaviour
	{
		private Graphic _graphic;
		private Graphic GraphicComponent { get { if (_graphic == null) _graphic = GetComponent<Graphic>(); return _graphic; } }

		private TextMeshProUGUI _textMeshPro;

		private List<TMP_SubMeshUI> _subMeshes = new List<TMP_SubMeshUI>(8);
		private static List<TMP_SubMeshUI> _subMeshTemp = new List<TMP_SubMeshUI>(8);

		protected static class ShaderProp
		{
			public readonly static int SourceTex = Shader.PropertyToID("_SourceTex");
			public readonly static int ResultTex = Shader.PropertyToID("_ResultTex");
		}

		private ScreenRectFromMeshes _screenRect = new ScreenRectFromMeshes();
		private Compositor _composite = new Compositor();
		private RenderTexture _rt;
		private RenderTexture _rt2;
		private Material _displayMaterial;
		private VertexHelper _quadVertices;
		private List<Color> _vertexColors;
		private Mesh _quadMesh;
		private int _lastRenderFrame = -1;
		private bool _needsRendering = true;

		[SerializeField] bool _applyToSprites = true;
		[SerializeField] bool _updateOnTransform = true;
		[SerializeField] bool _relativeToTransformScale = false;
		[SerializeField] FilterRenderSpace _renderSpace = FilterRenderSpace.Canvas;
		[SerializeField, Delayed] float _relativeFontSize = 0f;
		[SerializeField] FilterBase[] _filters = new FilterBase[0];

		public bool ApplyToSprites { get { return _applyToSprites; } set { ChangeProperty(ref _applyToSprites, value); } }
		public bool UpdateOnTransform { get { return _updateOnTransform; } set { ChangeProperty(ref _updateOnTransform, value); } }
		public FilterRenderSpace RenderSpace { get { return _renderSpace; } set { ChangeProperty(ref _renderSpace, value); } }
		public List<FilterBase> Filters { get { return new List<FilterBase>(_filters); } set { ChangePropertyArray(ref _filters, value.ToArray()); } }

		private bool CanApplyFilter()
        {
            return default;
        }

        [UnityInternal.ExcludeFromDocs]
		protected override void Awake()
        {
        }

        /// <summary>
        /// NOTE: OnDidApplyAnimationProperties() is called when the Animator is used to keyframe properties
        /// </summary>
        protected override void OnDidApplyAnimationProperties()
        {
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
        }

        protected override void OnValidate()
        {
        }
#endif

        protected void ChangeProperty<T>(ref T backing, T value) where T : struct
        {
        }

        protected bool ChangePropertyArray<T>(ref T backing, T value) where T : System.Collections.ICollection
        {
            return default;
        }

        [UnityInternal.ExcludeFromDocs]
		protected override void OnEnable()
        {
        }

        [UnityInternal.ExcludeFromDocs]
        protected override void OnDisable()
        {
        }

        protected void OnGraphicMaterialDirtied()
        {
        }

        protected void OnGraphicVerticesDirtied()
        {
        }

        //void OnTextVerticesChanged(TMP_TextInfo textInfo) {}

        void OnTextGeomeryRebuilt(Object obj)
        {
        }

        void GatherSubMeshes()
        {
        }

        void CalculateScreenRect()
        {
        }

        private void SetupMaterialTMPro(Material material, Camera camera, RectInt textureRect)
        {
        }

        private void HandleVertexColors(Mesh mesh, Material material, bool vertexColorAlwaysGammaSpace, bool isSprite)
        {
        }

        void RenderToTexture()
        {
        }

        private Matrix4x4 _previousLocalToWorldMatrix;
        private Matrix4x4 _previousCameraMatrix;

		protected virtual void Update()
        {
        }

        void WillRenderCanvases()
        {
        }

        private void ApplyOutputMeshAndMaterial()
        {
        }

        private void ApplyPreviousOutput()
        {
        }

        private Camera GetRenderCamera()
        {
            return default;
        }

        private bool HasActiveFilters()
        {
            return default;
        }
		protected void LOG(string message, [System.Runtime.CompilerServices.CallerMemberName] string callerName = "")
        {
        }

        protected void LOGFUNC(string message = null, [System.Runtime.CompilerServices.CallerMemberName] string callerName = "")
        {
        }
    }
}
#endif