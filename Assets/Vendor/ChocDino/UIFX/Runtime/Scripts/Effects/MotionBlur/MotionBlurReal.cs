//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

#define UIFX_MOTION_BLUR_CLIP_TO_SCREEN
#define UIFX_MOTION_BLUR_SKIP_ZERO_AREA_MESHES
#if UNITY_EDITOR
//#define UIFX_MOTION_BLUR_DEBUG
#endif

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityInternal = UnityEngine.Internal;

namespace ChocDino.UIFX
{
	/// <summary>
	/// The MotionBlurReal component is a visual effect that can be applied to any UI (uGUI) components 
	/// to create an accurate motion blur effect when the UI components are in motion.
	/// </summary>
	/// <remark>
	/// How it works:
	/// 1. Store the mesh and transforms for a UI component for the previous and current frames.
	/// 2. Generates a new mesh containing multiple copies of the stored meshes interpolated from previous to current mesh.
	/// 3. Rendered this mesh additively to a RenderTexture.
	/// 4. On the next frame a quad is rendered to the canvas in place of the UI component geometry.  This quad
	///    uses a shader to resolve the previously rendered motion blur mesh.
	/// 5. If no motion is detected then the effect is disabled.
	///
	/// Comparison between MotionBlurSimple and MotionBlurReal:
	/// 1. MotionBlurSimple is much less expensive to render than MotionBlurReal.
	/// 2. MotionBlurReal produces a much more accurate motion blur than MotionBlurSimple.
	/// 3. MotionBlurReal handles transparency much better than MotionBlurSimple.
	/// 4. MotionBlurReal can become very slow when the motion traveled in a single frame is very large on screen.
	/// 5. MotionBlurReal renders with 1 frame of latency, MotionBlurSimple renders immediately with no latency.
	///
	/// Notes:
	/// 1. Masking is supported, but it doesn't motion blur beyond the bounds of the mask
	/// </remark>
	//[ExecuteAlways]
	[RequireComponent(typeof(Graphic))]
	[HelpURL("https://www.chocdino.com/products/unity-assets/")]
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Effects/UIFX - Motion Blur (Real)")]
	public class MotionBlurReal : UIBehaviour, IMeshModifier, IMaterialModifier
	{
		[Tooltip("Which vertex modifiers are used to calculate the motion blur.")]
		[SerializeField] VertexModifierSource _mode = VertexModifierSource.Transform;

		[Tooltip("The number of motion blur steps to calculate.  The higher the number the more expensive the effect.  Set to 1 means the effect is not applied.")]
		[SerializeField, Range(1f, 64f)] int _sampleCount = 16;

		[Tooltip("Interpolate texture coordinates. Disable this if you are changing the characters of text.")]
		[SerializeField] bool _lerpUV = false;

		[Tooltip("Allows frame-rate independent blur length.  This is unrealistic but may be more artistically pleasing as the visual appearance of the motion blur remains consistent across frame rates.")]
		[SerializeField] bool _frameRateIndependent = true;

		[Tooltip("The strength of the effect. Zero means the effect is not applied.  Greater than one means the effect is exagerated.")]
		[SerializeField, Range(0f, 4f)] float _strength = 1f;

		[Tooltip("The shader to use for the additive pass")]
		[SerializeField] Shader _shaderAdd = null;

		[Tooltip("The shader to use for the resolve pass")]
		[SerializeField] Shader _shaderResolve = null;

		// Graphic geometry
		private bool _isPrimed;
		private int _graphicActiveVertexCount;
		private List<UIVertex> _graphicVerticesNow;
		private UIVertex[] _graphicVerticesPast;
		private Matrix4x4 _localToWorldPast;
		private Matrix4x4 _worldToCameraPast;
		private Camera _trackingCamera;

		// Blur geometry
		private int _blurVertexCount;
		private Vector3[] _blurVertexPositions;
		private Vector2[] _blurVertexUV0s;
		private Color[] _blurVertexColors;
		private int[] _blurVertexIndices;
		private bool _isBlurredLastFrame;
		private Mesh _blurMesh;
		private Bounds _blurMeshWorldBounds;

		// Rendering params
		private float _screenWidth, _screenHeight;
		private int _textureWidth, _textureHeight;
		private float _worldHeight;
		private Vector3 _worldCenter;
		private Rect _clampedScreenRect;
		private Bounds _screenBounds;
		private Canvas _canvas;
		private Vector3[] _boundsPoint = new Vector3[8];

		// Rendering
		private Material _materialAdd;
		private Material _materialResolve;
		private CommandBuffer _cb;
		private RenderTexture _rt;

		private Graphic _graphic;
		private Graphic GraphicComponent { get { if (_graphic == null) { _graphic = GetComponent<Graphic>(); } return _graphic; } }

		private MaskableGraphic _maskableGraphic;
		private MaskableGraphic MaskableGraphicComponent { get { if (_maskableGraphic == null) { _maskableGraphic = GraphicComponent as MaskableGraphic; } return _maskableGraphic; } }

		private CanvasRenderer _canvasRenderer;
		private CanvasRenderer CanvasRenderComponent { get { if (_canvasRenderer == null) { if (GraphicComponent) { _canvasRenderer = _graphic.canvasRenderer; } else { _canvasRenderer = GetComponent<CanvasRenderer>(); } } return _canvasRenderer; } }

		private readonly static Vector4 Alpha8TextureAdd = new Vector4(1f, 1f, 1f, 0f);
		private readonly static Color32 WhiteColor32 = new Color32(255, 255, 255, 255);

		static class ShaderProp
		{
			public readonly static int MainTex2 = Shader.PropertyToID("_MainTex2");
			public readonly static int InvSampleCount = Shader.PropertyToID("_InvSampleCount");
		}

		/// <summary>Property <c>UpdateMode</c> sets which vertex modifiers are used to calculate the motion blur</summary>
		/// <value>Set to <c>Mode.Transform</c> by default</value>
		public VertexModifierSource UpdateMode { get { return _mode; } set { _mode = value; ForceMeshModify(); } }

		/// <summary>Property <c>SampleCount</c> sets the number of motion blur steps to calculate.  The higher the number the more expensive the effect.</summary>
		/// <value>Set to 16 by default</value>
		public int SampleCount { get { return _sampleCount; } set { _sampleCount = value; ForceMeshModify(); } }

		/// <summary>Interpolate texture coordinates. Disable this if you are changing the characters of text.</summary>
		public bool LerpUV { get { return _lerpUV; } set { _lerpUV = value; } }

		/// <summary>Property <c>FrameRateIndependent</c> allows frame-rate independent blur length.  This is unrealistic but may be more artistically pleasing as the visual appearance of the motion blur remains consistent across frame rates.</summary>
		public bool FrameRateIndependent { get { return _frameRateIndependent; } set { _frameRateIndependent = value; } }

		/// <summary>Property <c>Strength</c> controls how large the motion blur effect is.</summary>
		/// <value>Set to 1.0 by default.  Zero means the effect is not applied.  Greater than one means the effect is exagerated.</value>
		public float Strength { get { return _strength; } set { _strength = value; ForceMeshModify(); } }
	
		/// <summary>Global debugging option to tint the colour of the motion blur mesh to magenta.  Can be used to tell when the effect is being applied</summary>
		/// <value>Set to <c>false</c> by default</value>
		public static bool GlobalDebugTint = false;

		/// <summary>Global option to freeze updating of the mesh, useful for seeing the motion blur</summary>
		/// <value>Set to <c>false</c> by default</value>
		public static bool GlobalDebugFreeze = false;

		/// <summary>Global option to disable this effect from being applied</summary>
		/// <value>Set to <c>false</c> by default</value>
		public static bool GlobalDisabled = false;

		void CreateComponents()
        {
        }

        void DestroyComponents()
        {
        }

        private void RenderMeshToTexture()
        {
        }

        [UnityInternal.ExcludeFromDocs]
        protected override void Start()
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

        private void OnCullingChanged(bool culled)
        {
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
        }
#endif

        protected override void OnDidApplyAnimationProperties()
        {
        }

        /// <summary>
        /// OnCanvasHierarchyChanged() is called when the Canvas is enabled/disabled
        /// </summary>
        protected override void OnCanvasHierarchyChanged()
        {
        }

        private void ForceMeshModify()
        {
        }

        private void ForceMaterialModify()
        {
        }

        private enum DirtySource : byte
        {
            None = 0,
            Transform = 0x01,
            Vertices = 0x02,
            SelfForced = 0x04,
        }

        private DirtySource _dirtySource = DirtySource.None;

        private bool IsDirtyTransform { get { return (_dirtySource & DirtySource.Transform) != 0; } set { _dirtySource |= DirtySource.Transform; } }
        private bool IsDirtyVertices { get { return (_dirtySource & DirtySource.Vertices) != 0; } set { _dirtySource |= DirtySource.Vertices; } }
        private bool IsDirtySelfForced { get { return (_dirtySource & DirtySource.SelfForced) != 0; } set { _dirtySource |= DirtySource.SelfForced; } }

        void OnDirtyVertices()
        {
        }

        void LateUpdate()
        {
        }

        void LateUpdate5()
        {
        }

        private bool HasGeneratedMesh()
        {
            return default;
        }

        private bool HasRendered()
        {
            return default;
        }

        private bool CanApply()
        {
            return default;
        }

        private Canvas GetCanvas()
        {
            return default;
        }

        private Camera GetRenderCamera()
        {
            return default;
        }

        /// This is where the geometry is gathered
        /// Draw the geometry (amplified) to addivitive RT
        /// Draw a quad to screen sampling this buffer..
        [UnityInternal.ExcludeFromDocs]
        public void ModifyMesh(VertexHelper vh)
        {
        }

        private void GenerateRenderMetrics(Camera camera)
        {
        }

        private void GenerateQuad(VertexHelper vh, Camera camera)
        {
        }

        [UnityInternal.ExcludeFromDocs]
        public Material GetModifiedMaterial(Material baseMaterial)
        {
            return default;
        }

        /// <summary>
        /// Reset the motion blur to begin again at the current state (transform/vertex positions).
        /// This is useful when reseting the transform to prevent motion blur drawing erroneously between
        /// the last position and the new position.
        /// </summary>
        public void ResetMotion()
        {
        }

        private bool PrepareBuffers(VertexHelper vh)
        {
            return default;
        }

        private bool IsTrackingTransform()
        {
            return default;
        }

        private bool IsTrackingVertices()
        {
            return default;
        }

        private void UpdateCanvasCamera()
        {
        }

        private void CacheState()
        {
        }

        private float GetLerpFactorUnclamped(float t)
        {
            return default;
        }

        private void CreateMotionBlurMesh(VertexHelper vh)
        {
        }

        [UnityInternal.ExcludeFromDocs]
        [System.Obsolete("use IMeshModifier.ModifyMesh (VertexHelper verts) instead", false)]
        public void ModifyMesh(Mesh mesh)
        {
        }
    }
}