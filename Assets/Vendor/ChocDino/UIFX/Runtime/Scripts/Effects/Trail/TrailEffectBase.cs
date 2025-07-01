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
	/// <summary>The mode to use for fading out the trail when `Strength` is less than 1.0</summary>
	public enum TrailStrengthMode
	{
		/// <summary>`Damping` - Reduce damping so that when strength == 0.0 there is no lag in the trail.</summary>
		Damping,
		/// <summary>`Layers` - Remove each layer, starting from the back so that when strength == 0 there are no layers visible.</summary>
		Layers,
		/// <summary>`FadeLayers` - Same as `Layers` but with fading instead of a hard cut.</summary>
		FadeLayers,
		/// <summary>`Fade` - Fade the entire trail down at the same time.</summary>
		Fade,
	}

	/// <summary>
	/// Base class for TrailEffect
	/// </summary>
	[RequireComponent(typeof(Graphic))]
	[HelpURL("https://www.chocdino.com/products/uifx/")]
	public abstract class TrailEffectBase : UIBehaviour, IMeshModifier
	{
		[UnityInternal.ExcludeFromDocs]
		[Tooltip("The number of trail layers")]
		[SerializeField, Range(0f, 64f)] protected int _layerCount = 16;

		[UnityInternal.ExcludeFromDocs]
		[UnityEngine.Serialization.FormerlySerializedAs("_damping")]
		[Tooltip("The rate at which the front of the trail catches up with the movement.  Higher value results in a less laggy trail.  Default value is 50, range is [0..250].")]
		[SerializeField, Range(0f, 250f)] protected float _dampingFront = 50f;

		[UnityInternal.ExcludeFromDocs]
		[Tooltip("The rate at which the back of the trail catches up with the movement.  Higher value results in a less laggy trail.  Default value is 50, range is [0..250].")]
		[SerializeField, Range(0f, 250f)] protected float _dampingBack = 50f;

		[UnityInternal.ExcludeFromDocs]
		[Tooltip("Optional curve to control transparency. Transparency can also be controlled by the gradient property, but having this secondary control is useful when the gradient is animated but you still want apply a static transparency falloff.")]
		[SerializeField] protected AnimationCurve _alphaCurve = new AnimationCurve(new Keyframe(0f, 1f, -1f, -1f), new Keyframe(1f, 0f, -1f, -1f));

		[UnityInternal.ExcludeFromDocs]
		[Tooltip("Which vertex modifiers affect are used to calculate the vertex modifier effect. TranformAndVertex is the most expensive.")]
		[SerializeField] protected VertexModifierSource _vertexModifierSource = VertexModifierSource.Transform;

		[UnityInternal.ExcludeFromDocs]
		[Tooltip("The gradient colors used by the trail")]
		[SerializeField] protected Gradient _gradient = ColorUtils.GetBuiltInGradient(BuiltInGradient.SoftRainbow);

		[UnityInternal.ExcludeFromDocs]
		[Tooltip("The offset applied to the gradient. The gradient will wrap using mirrored repeating.")]
		[SerializeField] protected float _gradientOffset = 0f;

		[UnityInternal.ExcludeFromDocs]
		[Tooltip("The scaling applied to the gradient. The gradient will wrap using mirrored repeating.")]
		[SerializeField] protected float _gradientScale = 1f;

		[UnityInternal.ExcludeFromDocs]
		[Tooltip("The animation speed for the offset property of the gradient. Allows easy simple scrolling animation without scripting. Set to zero for no animation.")]
		[SerializeField] protected float _gradientOffsetSpeed = 0f;

		[UnityInternal.ExcludeFromDocs]
		[Tooltip("Only show the trail, hide the original UI Graphic")]
		[SerializeField] protected bool _showTrailOnly = false;

		[UnityInternal.ExcludeFromDocs]
		[Tooltip("Which color blending mode to use to mix the original vertex colors with the gradient colors")]
		[SerializeField] protected BlendMode _blendMode = BlendMode.Multiply;

		[UnityInternal.ExcludeFromDocs]
		[Tooltip("The mode to use for fading out the trail when strength < 1.0")]
		[SerializeField] protected TrailStrengthMode _strengthMode = TrailStrengthMode.FadeLayers;

		[UnityInternal.ExcludeFromDocs]
		[Tooltip("Strength of the effect. Range [0..1]")]
		[SerializeField, Range(0f, 1f)] protected float _strength = 1f;

		/// <summary>The number of trail layers</summary>
		public int LayerCount { get { return _layerCount; } set { _layerCount = Mathf.Max(0, value); } }

		/// <summary>The rate at which the front of the trail catches up with the movement.  Higher value results in a less laggy trail.  Default value is 50, range is [0..250].</summary>
		public float DampingFront { get { return _dampingFront; } set { _dampingFront = Mathf.Max(0f, value); } }

		/// <summary>The rate at which the back of the trail catches up with the movement.  Higher value results in a less laggy trail.  Default value is 50, range is [0..250].</summary>
		public float DampingBack { get { return _dampingBack; } set { _dampingBack = Mathf.Max(0f, value); } }

		/// <summary>Optional curve to control transparency. Transparency can also be controlled by the gradient property, but having this secondary control is useful when the gradient is animated but you still want apply a static transparency falloff.</summary>
		public AnimationCurve AlphaCurve { get { return _alphaCurve; } set { _alphaCurve = value; } }

		/// <summary>Which vertex modifiers affect are used to calculate the vertex modifier effect. TranformAndVertex is the most expensive.</summary>
		public VertexModifierSource VertexModifierSource { get { return _vertexModifierSource; } set { _vertexModifierSource = value; OnChangedVertexModifier(); } }

		/// <summary>The gradient colors used by the trail</summary>
		public Gradient Gradient { get { return _gradient; } set { _gradient = value; } }

		/// <summary>The offset applied to the gradient. The gradient will wrap using mirrored repeating.</summary>
		public float GradientOffset { get { return _gradientOffset; } set { _gradientOffset = value; } }

		/// <summary>The scaling applied to the gradient. The gradient will wrap using mirrored repeating.</summary>
		public float GradientScale { get { return _gradientScale; } set { _gradientScale = value; } }

		/// <summary>The animation speed for the offset property of the gradient. Allows easy simple scrolling animation without scripting. Set to zero for no animation.</summary>
		public float GradientOffsetSpeed { get { return _gradientOffsetSpeed; } set { _gradientOffsetSpeed = value; } }

		/// <summary>Only show the trail, hide the original UI Graphic</summary>
		public bool ShowTrailOnly { get { return _showTrailOnly; } set { _showTrailOnly = value; } }

		/// <summary>Which color blending mode to use to mix the original vertex colors with the gradient colors</summary>
		public BlendMode BlendMode { get { return _blendMode; } set { _blendMode = value; } }

		/// <summary>The mode to use for fading out the trail when strength is less than 1.0</summary>
		public TrailStrengthMode StrengthMode { get { return _strengthMode; } set { _strengthMode = value; } }

		/// <summary>The strength of the effect. Range [0..1]</summary>
		public float Strength { get { return _strength; } set { _strength = Mathf.Clamp01(value); } }

		[UnityInternal.ExcludeFromDocs]
		protected Graphic _graphic;
		protected Graphic GraphicComponent { get { if (_graphic == null) { _graphic = GetComponent<Graphic>(); } return _graphic; } }

		[UnityInternal.ExcludeFromDocs]
		protected MaskableGraphic _maskableGraphic;
		protected MaskableGraphic MaskableGraphicComponent { get { if (_maskableGraphic == null) { _maskableGraphic = GraphicComponent as MaskableGraphic; } return _maskableGraphic; } }

		[UnityInternal.ExcludeFromDocs]
		protected CanvasRenderer _canvasRenderer;
		protected CanvasRenderer CanvasRenderComponent { get { if (_canvasRenderer == null) { if (GraphicComponent) { _canvasRenderer = _graphic.canvasRenderer; } else { _canvasRenderer = GetComponent<CanvasRenderer>(); } } return _canvasRenderer; } }

		/// <summary>If Time scale is ignored then Time.unscaledDeltaTime will be used for updating animation</summary>
		public static bool IgnoreTimeScale { get; set ; }

		protected static float DeltaTime { get { return (IgnoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime); } }

		protected virtual void SetDirty()
        {
        }

        /// <summary>
        /// Reset the trail to begin again at the current state (transform/vertex positions).
        /// This is useful when reseting the transform to prevent trail drawing erroneously between
        /// the last position and the new position.
        /// </summary>
        public virtual void ResetMotion()
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

        protected bool CanApply()
        {
            return default;
        }

        protected bool IsTrackingTransform()
        {
            return default;
        }

        protected bool IsTrackingVertices()
        {
            return default;
        }

        [UnityInternal.ExcludeFromDocs]
        public virtual void ModifyMesh(VertexHelper vh)
        {
        }

        protected abstract void OnChangedVertexModifier();

        [UnityInternal.ExcludeFromDocs]
        [System.Obsolete("use IMeshModifier.ModifyMesh (VertexHelper verts) instead", false)]
        public void ModifyMesh(Mesh mesh)
        {
        }
    }
}