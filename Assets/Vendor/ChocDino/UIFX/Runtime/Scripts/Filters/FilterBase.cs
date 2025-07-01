//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

//#define UIFX_FILTER_SUPPORT_CHILDREN
//#define UIFX_FILTER_TMP
//#define UIFX_OLD178_RESOLUTION_SCALING
#if UNITY_EDITOR
	#define UIFX_FILTER_DEBUG
#endif

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using UnityInternal = UnityEngine.Internal;
#if UIFX_FILTER_TMP
using TMPro;
#endif

namespace ChocDino.UIFX
{
	public enum FilterRenderSpace
	{
		// Rendering is done in local canvas-space, before transforming the vertices to screen-space.
		Canvas,
		// Rendering is done after transform the vertices from local-space to screen-space.
		Screen,
	}

	public enum FilterExpand
	{
		None,
		Expand,
	}

	/// <summary>
	/// Base class for all derived filter classes
	/// </summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(CanvasRenderer))]
	[HelpURL("https://www.chocdino.com/products/unity-assets/")]
	public abstract partial class FilterBase : UIBehaviour, IMaterialModifier, IMeshModifier
	{
		[Tooltip("How strongly the effect is applied.")]
		[Range(0f, 1f)]
		[SerializeField] protected float _strength = 1f;

		[Tooltip("")]
		[SerializeField] protected FilterRenderSpace _renderSpace = FilterRenderSpace.Canvas;

		[SerializeField] protected FilterExpand _expand = FilterExpand.Expand;

		/// <summary>How much of the maximum effect to apply.  Range [0..1] Default is 1.0</summary>
		public float Strength { get { return _strength; } set { ChangeProperty(ref _strength, Mathf.Clamp01(value)); } }

		/// <summary>Scale all resolution calculations by this ammount.  Useful for allowing font size / transform scale to keep consistent rendering of effects.</summary>
		public float UserScale { get { return _userScale; } set { ChangeProperty(ref _userScale, value); } }

		/// <summary></summary>
		public FilterRenderSpace RenderSpace { get { return _renderSpace; } set { ChangeProperty(ref _renderSpace, value); } }

		protected readonly static Vector4 Alpha8TextureAdd = new Vector4(1f, 1f, 1f, 0f);

		protected static class ShaderProp
		{
			public readonly static int SourceTex = Shader.PropertyToID("_SourceTex");
			public readonly static int ResultTex = Shader.PropertyToID("_ResultTex");
			public readonly static int Strength = Shader.PropertyToID("_Strength");
		}

		private bool _isGraphicText;
		private Graphic _graphic;
		internal Graphic GraphicComponent { get { if (!_graphic) { _graphic = GetComponent<Graphic>(); _isGraphicText = _graphic is Text; } return _graphic; } }

		private MaskableGraphic _maskableGraphic;
		private MaskableGraphic MaskableGraphicComponent { get { if (!_maskableGraphic) { _maskableGraphic = GraphicComponent as MaskableGraphic; } return _maskableGraphic; } }

		private CanvasRenderer _canvasRenderer;
		private CanvasRenderer CanvasRenderComponent { get { if (!_canvasRenderer) { if (GraphicComponent) { _canvasRenderer = _graphic.canvasRenderer; } else { _canvasRenderer = GetComponent<CanvasRenderer>(); } } return _canvasRenderer; } }

		private Material _baseMaterial;
		protected ScreenRectFromMeshes _screenRect = new ScreenRectFromMeshes();
		internal Compositor _composite = new Compositor();
		protected Material _displayMaterial;
		private Mesh _mesh;
		private List<Color> _vertexColors;
		private Mesh _quadMesh;
		private Material _baseMaterialCopy;
		private Canvas _canvas;
		private bool _materialOutputPremultipliedAlpha = false;
		private bool _isFilterEnabled = false;
		protected float _userScale = 1f;
		protected bool _forceUpdate = true;
		protected RectAdjustOptions _rectAdjustOptions = new RectAdjustOptions();

		internal RectAdjustOptions RectAdjustOptions { get { return _rectAdjustOptions;} }
		internal float ResolutionScalingFactor { get; private set; }

		internal const string DefaultBlendShaderPath = "Hidden/ChocDino/UIFX/Blend";
		private const string ResolveShaderPath = "Hidden/ChocDino/UIFX/Resolve";
		private RenderTexture _resolveTexture;
		private Material _resolveMaterial;
		private Texture2D _readableTexture;

		internal bool DisableRendering { get; set; }
		
		protected void LOG(string message, [System.Runtime.CompilerServices.CallerMemberName] string callerName = "")
        {
        }

        protected void LOGFUNC(string message = null, [System.Runtime.CompilerServices.CallerMemberName] string callerName = "")
        {
        }

        public bool IsFiltered()
        {
            return default;
        }

        internal virtual bool CanApplyFilter()
        {
            return default;
        }

        protected virtual bool DoParametersModifySource()
        {
            return default;
        }

        private const string TextMeshProShaderPrefix = "TextMeshPro";
		private Material _lastBaseMaterial;
		private bool _isLastMaterialTMPro;

		private bool CanSelfRender()
        {
            return default;
        }

        public virtual Material GetModifiedMaterial(Material baseMaterial)
        {
            return default;
        }

        protected virtual string GetDisplayShaderPath()
        {
            return default;
        }

        private bool _graphicMaterialDirtied = false;

		protected void OnGraphicMaterialDirtied()
        {
        }

        protected override void OnEnable()
        {
        }

        protected override void OnDisable()
        {
        }

        protected override void OnDestroy()
        {
        }

        /// <summary>
        /// NOTE: OnRectTransformDimensionsChange() is called whenever any of the elements in RectTransform (or parents) change
        /// This doesn't get called when pixel-perfect option is disabled and the translation/rotation/scale etc changes..
        /// </summary>
        protected override void OnRectTransformDimensionsChange()
        {
        }

        /// <summary>
        /// NOTE: OnDidApplyAnimationProperties() is called when the Animator is used to keyframe properties
        /// </summary>
        protected override void OnDidApplyAnimationProperties()
        {
        }

        /// <summary>
        /// OnCanvasHierarchyChanged() is called when the Canvas is enabled/disabled
        /// </summary>
        protected override void OnCanvasHierarchyChanged()
        {
        }

        /// <summary>
        /// OnTransformParentChanged() is called when a parent is changed, in which case we may need to get a new Canvas
        /// </summary>
        protected override void OnTransformParentChanged()
        {
        }

        /// <summary>
        /// Forces the filter to update.  Usually this happens automatically, but in some cases you may want to force an update.
        /// </summary>
        public void ForceUpdate(bool force = false)
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

        private Matrix4x4 _previousLocalToWorldMatrix;
		private Matrix4x4 _previousCameraMatrix;
		private float _lastRenderAlpha = -1f;
		internal Vector2Int _lastRenderAdjustLeftDown;
		internal Vector2Int _lastRenderAdjustRightUp;

		protected void OnPropertyChange()
        {
        }

        protected virtual void Update()
        {
        }

#if false
		void LateUpdate()
		{
			if (CanApplyFilter() != _isFilterEnabled)
			{
#if UIFX_FILTER_SUPPORT_CHILDREN
				ForceChildrenUpdate();
#endif
			}

#if UIFX_FILTER_TMP
			if (GraphicComponent is TextMeshProUGUI)
			{
				var tmp = ((TextMeshProUGUI)GraphicComponent);
				//if (tmp.havePropertiesChanged)
				{
					Mesh sourceMesh = null;
					if (CanApplyFilter())
					{
						sourceMesh = ((TextMeshProUGUI)GraphicComponent).mesh;
					}

					ApplyMesh(sourceMesh);
				}
			}
#endif
		}

		void WillRenderCanvases()
		{
#if UIFX_FILTER_SUPPORT_CHILDREN
			// This will run when there is no GraphicComponent (useful for cases where 
			// only the children are having the effect applied to them)
			if (GraphicComponent == null && CanApplyFilter())
			{
				if (_quadMesh == null)
				{
					_quadMesh = new Mesh();
				}
				_quadMesh.Clear();
				VertexHelper v = new VertexHelper(_quadMesh);
				ModifyMesh(v);
				v.FillMesh(_quadMesh);
				v.Dispose();
				var cr = CanvasRenderComponent;
				cr.SetMesh(_quadMesh);
				if (_displayMaterial)
				{
					cr.materialCount = 1;
					cr.SetMaterial(_displayMaterial, 0);
				}
			}
#endif
		}
#endif

        private Mesh _blitMesh;
		private CommandBuffer _blitCommands;
		
		internal bool RenderToTexture(RenderTexture sourceTexture, RenderTexture destTexture)
        {
            return default;
        }

        private bool _isResolveTextureDirty = true;

        /// <summary>
        /// Resolves to a final sRGB straight-alpha texture suitable for display or saving to image file.
        /// </summary>
        public RenderTexture ResolveToTexture()
        {
            return default;
        }

        /// <summary>
        /// Resolve the filter output to a sRGB texture and write it to a PNG file
        /// </summary>
        public bool SaveToPNG(string path)
        {
            return default;
        }

        public bool IsFilterEnabled()
        {
            return default;
        }

        /// <summary>
        /// This method is only intended to be used by the developers of UIFX.
        /// </summary>
        public void SetFilterEnabled(bool state)
        {
        }

        protected virtual float GetAlpha()
        {
            return default;
        }

        private static List<Canvas> s_canvasList;
        private static bool s_warnCanvasPlaneDistanceCulling = false;
        internal const string s_warnCanvasPlaneDistanceCullingMessage = "[UIFX] Canvas.planeDistance is beyond Camera.nearClipPlane and Camera.farClipPlane. This can result in the Graphic being culled.";

        private Canvas GetCanvas()
        {
            return default;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Warn users if Canvas.planeDistance is not between Camera.nearClipPlane and Camera.farClipPlane
        /// Some users scenes have this set incorrectly, so it's easy to add a test here.  For normal Unity UI this doesn't matter,
        /// but because in FilterRenderSpace.Screen mode we're converting to screen space and doing frustum clipping having incorrect Canvas.planeDistance
        /// can result in the Graphic being culled.
        /// </summary>
        internal bool IsCanvasPlaneDistanceOutOfRange()
        {
            return default;
        }
#endif

        private Camera GetRenderCamera()
        {
            return default;
        }

        private static UIVertex s_vertex;
        private bool HasMeshChanged(VertexHelper verts)
        {
            return default;
        }

        private void GrabMesh(VertexHelper verts)
        {
        }

        private void GrabMesh(Mesh mesh)
        {
        }

        private void BuildOutputQuad(VertexHelper verts)
        {
        }

        private void ApplyMesh(Mesh mesh)
        {
        }

        private int _lastModifyMeshFrame = -1;

        /// <summary>
        /// Implements IMeshModifier.ModifyMesh() which is called by uGUI automatically when Graphic components generate geometry
        /// Note that this method is not called when using TextMeshPro as it bypasses the standard geometry generation and instead
        /// applies internally generated Mesh to the CanvasRenderer directly.
        /// Note that ModifyMesh() is called BEFORE GetModifiedMaterial()
        /// </summary>
        [UnityInternal.ExcludeFromDocs]
        public void ModifyMesh(VertexHelper verts)
        {
        }

        private static bool _issuedModifyMeshWarning = false;

        [UnityInternal.ExcludeFromDocs]
        [System.Obsolete("use IMeshModifier.ModifyMesh (VertexHelper verts) instead, or set useLegacyMeshGeneration to false", false)]
        public void ModifyMesh(Mesh mesh)
        {
        }

        protected void GetResolutionScalingFactor()
        {
        }

        protected bool GenerateScreenRect()
        {
            return default;
        }

        internal void AdjustRect(ScreenRectFromMeshes rect)
        {
        }

        internal void SetFinalRect(ScreenRectFromMeshes finalRect)
        {
        }

        protected Rect _rectRatio = new Rect(0f, 0f, 1f, 1f);

		private void SetupMaterialForRendering(Material baseMaterial)
        {
        }

        protected virtual bool RenderFilter(bool generateScreenRect)
        {
            return default;
        }

        protected virtual RenderTexture RenderFilters(RenderTexture source)
        {
            return default;
        }

        protected virtual void GetFilterAdjustSize(ref Vector2Int leftDown, ref Vector2Int rightUp)
        {
        }

        protected virtual void SetupDisplayMaterial(Texture source, Texture result)
        {
        }

        protected bool ChangeProperty<T>(ref T backing, T value) where T : struct
        {
            return default;
        }

        protected bool ChangePropertyRef<T>(ref T backing, T value) where T : class
        {
            return default;
        }

        internal Rect GetLocalRect()
        {
            return default;
        }

        internal virtual string GetDebugString()
        {
            return default;
        }

        internal virtual Texture[] GetDebugTextures()
        {
            return default;
        }
    }
}