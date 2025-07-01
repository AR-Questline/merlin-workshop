//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityInternal = UnityEngine.Internal;

namespace ChocDino.UIFX
{
	// TODO: make the blur factor (transparency in our case) relative to the amount of motion in SCREEN space..(1 / (1 + d))

	/// <summary>
	/// The MotionBlurSimple component is a visual effect that can be applied to any UI (uGUI) components 
	/// to create an approximate motion blur effect when the UI components are in motion.
	/// </summary>
	/// <remark>
	/// How it works:
	/// 1. Store the mesh and transforms for a UI component for the previous and current frames.
	/// 2. Generates a new mesh containing multiple copies of the stored meshes interpolated from previous to current mesh.
	/// 3. Replace the UI component mesh with the new motion blur mesh with a reduced per-vertex alpha (BlendStrength).
	/// 5. If no motion is detected then the effect is disabled.
	///
	/// Comparison between MotionBlurSimple and MotionBlurReal:
	/// 1. MotionBlurSimple is much less expensive to render than MotionBlurReal.
	/// 2. MotionBlurReal produces a much more accurate motion blur than MotionBlurSimple.
	/// 3. MotionBlurReal handles transparency much better than MotionBlurSimple.
	/// 4. MotionBlurReal can become very slow when the motion traveled in a single frame is very large on screen.
	/// 5. MotionBlurReal renders with 1 frame of latency, MotionBlurSimple renders immediately with no latency.
	///
	/// Since this is just an approximation, care must be taken to get the best results.  Some notes:
	/// 1. BlendStrength needs to be set based on the brightness of the object being rendered and the color of the background. 
	/// 2. The above is more important when rendering transparent UI objects, requiring a lower value for BlendStrength
	/// 3. When using the built-in Shadow component, it will cause problems due to dark transparent layer under opaque layer, which causes
	///    flickering.  Therefore when using Shadow component it's better to put it after this component.
	/// </remark>
	//[ExecuteAlways]
	[RequireComponent(typeof(Graphic))]
	[HelpURL("https://www.chocdino.com/products/unity-assets/")]
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Effects/UIFX - Motion Blur (Simple)")]
	public class MotionBlurSimple : UIBehaviour, IMeshModifier
	{
		[Tooltip("Which vertex modifiers are used to calculate the motion blur.")]
		[SerializeField] VertexModifierSource _mode = VertexModifierSource.Transform;

		[Tooltip("The number of motion blur steps to calculate.  The higher the number the more expensive the effect.  Set to 1 means the effect is not applied.")]
		[SerializeField, Range(1f, 64f)] int _sampleCount = 16;

		[Tooltip("How transparent the motion blur is.")]
		[SerializeField, Range(1f, 6f)] float _blendStrength = 2.5f;

		[Tooltip("Interpolate texture coordinates. Disable this if you are changing the characters of text.")]
		[SerializeField] bool _lerpUV = false;

		[Tooltip("Allows frame-rate independent blur length.  This is unrealistic but may be more artistically pleasing as the visual appearance of the motion blur remains consistent across frame rates.")]
		[SerializeField] bool _frameRateIndependent = true;

		[Tooltip("The strength of the effect. Zero means the effect is not applied.  Greater than one means the effect is exagerated.")]
		[SerializeField, Range(0f, 4f)] float _strength = 1f;

		/// <summary>Property <c>UpdateMode</c> sets which vertex modifiers are used to calculate the motion blur</summary>
		public VertexModifierSource UpdateMode { get { return _mode; } set { _mode = value; ForceMeshModify(); } }

		/// <summary>Property <c>SampleCount</c> sets the number of motion blur steps to calculate.  The higher the number the more expensive the effect.</summary>
		public int SampleCount { get { return _sampleCount; } set { _sampleCount = value; ForceMeshModify(); } }

		/// <summary>Property <c>BlendStrength</c> controls how transparent the motion blur is.</summary>
		public float BlendStrength { get { return _blendStrength; } set { _blendStrength = value; ForceMeshModify(); } }

		/// <summary>Interpolate texture coordinates. Disable this if you are changing the characters of text.</summary>
		public bool LerpUV { get { return _lerpUV; } set { _lerpUV = value; } }

		/// <summary>Property <c>FrameRateIndependent</c> allows frame-rate independent blur length.  This is unrealistic but may be more artistically pleasing as the visual appearance of the motion blur remains consistent across frame rates.</summary>
		public bool FrameRateIndependent { get { return _frameRateIndependent; } set { _frameRateIndependent = value; } }

		/// <summary>Property <c>Strength</c> controls how large the motion blur effect is.</summary>
		/// <value>Set to 1.0 by default.  Zero means the effect is not applied.  Greater than one means the effect is exagerated.</value>
		public float Strength { get { return _strength; } set { _strength = value; ForceMeshModify(); } }

		private Graphic _graphic;
		private bool _isPrimed;
		private int _activeVertexCount;
		private List<UIVertex> _currVertices;
		private List<UIVertex> _vertices;
		private UIVertex[] _prevVertices;
		private Matrix4x4 _prevLocalToWorld;
		private Matrix4x4 _prevWorldToCamera;
		private Camera _trackingCamera;
		private bool _blurredLastFrame;

		private Graphic GraphicComponent { get { if (_graphic == null) { _graphic = GetComponent<Graphic>(); } return _graphic; } }

		private MaskableGraphic _maskableGraphic;
		private MaskableGraphic MaskableGraphicComponent { get { if (_maskableGraphic == null) { _maskableGraphic = GraphicComponent as MaskableGraphic; } return _maskableGraphic; } }

		private CanvasRenderer _canvasRenderer;
		private CanvasRenderer CanvasRenderComponent { get { if (_canvasRenderer == null) { if (GraphicComponent) { _canvasRenderer = _graphic.canvasRenderer; } else { _canvasRenderer = GetComponent<CanvasRenderer>(); } } return _canvasRenderer; } }

		/// <summary>Global debugging option to tint the colour of the motion blur mesh to magenta.  Can be used to tell when the effect is being applied</summary>
		public static bool GlobalDebugTint = false;

		/// <summary>Global option to freeze updating of the mesh, useful for seeing the motion blur</summary>
		public static bool GlobalDebugFreeze = false;

		/// <summary>Global option to disable this effect from being applied</summary>
		public static bool GlobalDisabled = false;

		// NOTE: Pre-allocate function delegates to prevent garbage
		private UnityEngine.Events.UnityAction _cachedOnDirtyVertices;

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

        private List<UIVertex> CreateMotionBlurMesh(VertexHelper vh)
        {
            return default;
        }

        private bool CanApply()
        {
            return default;
        }

        [UnityInternal.ExcludeFromDocs]
        public void ModifyMesh(VertexHelper vh)
        {
        }

        [UnityInternal.ExcludeFromDocs]
        [System.Obsolete("use IMeshModifier.ModifyMesh (VertexHelper verts) instead", false)]
        public void ModifyMesh(Mesh mesh)
        {
        }
    }
}