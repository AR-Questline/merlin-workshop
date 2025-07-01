//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	public enum ExtrudeFillMode
	{
		Color,
		BiColor,
		Gradient,
		Texture,
	}

	public enum ExtrudeFillBlendMode
	{
		Replace,
		Multiply,
	}

	public enum ExtrudeProjection
	{
		Perspective,
		Orthographic,
	}

	/// <summary>
	/// An extrude filter for uGUI components
	/// </summary>
	[ExecuteInEditMode]
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Filters/UIFX - Extrude Filter")]
	public class ExtrudeFilter : FilterBase
	{
		[Tooltip("The projection mode used for extrusion.")]
		[SerializeField] ExtrudeProjection _projection = ExtrudeProjection.Perspective;

		[Tooltip("The clockwise angle the shadow is cast at. Range is [0..360]. Default is 135.0.")]
		[Range(0f, 360f)]
		[SerializeField] float _angle = 135f;

		[Tooltip("")]
		[Range(0f, 512f)]
		[SerializeField] float _perspectiveDistance = 0f;

		[Tooltip("The distance the shadow is cast. Range is [0..512]. Default is 32.0.")]
		[Range(0f, 512f)]
		[SerializeField] float _distance = 32f;

		[Tooltip("The composite mode to use for rendering.")]
		[SerializeField] ExtrudeFillMode _fillMode = ExtrudeFillMode.BiColor;

		[Tooltip("The color of the main/front of the shadow.")]
		[SerializeField] Color _colorFront = Color.grey;

		[Tooltip("The color of the back of the shadow in BiColor mode.")]
		[SerializeField] Color _colorBack = Color.clear;

		[Tooltip("The color of the back of the extrusion.")]
		[SerializeField] Texture _gradientTexture = null;

		[Tooltip("The color of the back of the extrusion.")]
		[SerializeField] Gradient _gradient = ColorUtils.GetBuiltInGradient(BuiltInGradient.SoftRainbow);

		[Tooltip("The speed to scroll the gradient.")]
		[SerializeField] float _scrollSpeed = 0f;

		[Tooltip("Reverse the fill direction.")]
		[SerializeField] bool _reverseFill = false;

		[Tooltip("")]
		[SerializeField] ExtrudeFillBlendMode _fillBlendMode = ExtrudeFillBlendMode.Multiply;

		[Tooltip("The transparency of the source content. Set to zero to make only the outline show.")]
		[Range(0f, 1f)]
		[SerializeField] float _sourceAlpha = 1.0f;

		[Tooltip("The composite mode to use for rendering.")]
		[SerializeField] LongShadowCompositeMode _compositeMode = LongShadowCompositeMode.Normal;

		/// <summary>The projection mode used for extrusion.</summary>
		public ExtrudeProjection Projection { get { return _projection; } set { ChangeProperty(ref _projection, value); } }

		/// <summary>The clockwise angle the shadow is cast at. Range is [0..360]. Default is 135.0</summary>
		public float Angle { get { return _angle; } set { ChangeProperty(ref _angle, value); } }

		/// <summary></summary>
		public float PerspectiveDistance { get { return _perspectiveDistance; } set { ChangeProperty(ref _perspectiveDistance, value); } }

		/// <summary>The distance the shadow is cast. Range is [-512..512]. Default is 32</summary>
		public float Distance { get { return _distance; } set { ChangeProperty(ref _distance, value); } }

		/// <summary>The composite mode to use for rendering.</summary>
		public ExtrudeFillMode FillMode { get { return _fillMode; } set { _fillMode = value; ForceUpdate(); } }

		/// <summary>The color of the main/front of the shadow</summary>
		public Color ColorFront { get { return _colorFront; } set { ChangeProperty(ref _colorFront, value); } }

		/// <summary>The color of the back of the shadow</summary>
		public Color ColorBack { get { return _colorBack; } set { ChangeProperty(ref _colorBack, value); } }

		/// <summary>The color of the back of the extrusion</summary>
		public Texture GradientTexture { get { return _gradientTexture; } set { _gradientTexture = value; ForceUpdate(); } }

		/// <summary>The color of the back of the extrusion</summary>
		public Gradient Gradient { get { return _gradient; } set { _gradient = value; ForceUpdate(); } }

		/// <summary>The speed to scroll the gradient.</summary>
		public float ScrollSpeed { get { return _scrollSpeed; } set { ChangeProperty(ref _scrollSpeed, value); } }

		/// <summary>Reverse the fill direction.</summary>
		public bool ReverseFill { get { return _reverseFill; } set { ChangeProperty(ref _reverseFill, value); } }

		/// <summary></summary>
		public ExtrudeFillBlendMode FillBlendMode { get { return _fillBlendMode; } set { ChangeProperty(ref _fillBlendMode, value); } }

		/// <summary>The transparency of the source content. Set to zero to make only the outline show. Range is [0..1] Default is 1.0</summary>
		public float SourceAlpha { get { return _sourceAlpha; } set { ChangeProperty(ref _sourceAlpha, value); } }

		/// <summary>The composite mode to use for rendering.</summary>
		public LongShadowCompositeMode CompositeMode { get { return _compositeMode; } set { ChangeProperty(ref _compositeMode, value); } }

		internal bool IsPreviewScroll { get; set; }

		private GradientTexture _textureFromGradient = new GradientTexture(256);
		private Extrude _effect = null;
		private float _scroll = 0f;

		private const string DisplayShaderPath = "Hidden/ChocDino/UIFX/Blend-Extrude";

		static new class ShaderProp
		{
			public readonly static int SourceAlpha = Shader.PropertyToID("_SourceAlpha");
		}
		static class ShaderKeyword
		{
			public const string StyleNormal = "STYLE_NORMAL";
			public const string StyleCutout = "STYLE_CUTOUT";
			public const string StyleShadow = "STYLE_SHADOW";
		}

		protected override string GetDisplayShaderPath()
        {
            return default;
        }

        internal override bool CanApplyFilter()
        {
            return default;
        }

        protected override bool DoParametersModifySource()
        {
            return default;
        }

        public void ResetScroll()
        {
        }

        protected override void OnEnable()
        {
        }

        protected override void OnDisable()
        {
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
        }
#endif


        protected override void GetFilterAdjustSize(ref Vector2Int leftDown, ref Vector2Int rightUp)
        {
        }

        protected override void SetupDisplayMaterial(Texture source, Texture result)
        {
        }

        protected override void Update()
        {
        }

        private void SetupFilterParams()
        {
        }

        protected override RenderTexture RenderFilters(RenderTexture source)
        {
            return default;
        }
    }
}