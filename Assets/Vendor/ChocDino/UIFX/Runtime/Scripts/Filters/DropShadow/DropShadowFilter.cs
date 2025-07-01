//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	public enum DropShadowMode
	{
		Default,
		Inset,
		Glow,
		Cutout,
	}

	/// <summary>
	/// A drop shadow filter for uGUI components
	/// </summary>
	[ExecuteInEditMode]
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Filters/UIFX - Drop Shadow Filter")]
	public class DropShadowFilter : FilterBase
	{
		[Tooltip("How much to downsample before blurring")]
		[SerializeField] Downsample _downSample = Downsample.Auto;

		[Tooltip("The maximum size of the blur kernel as a fraction of the diagonal length.  So 0.01 would be a kernel with pixel dimensions of 1% of the diagonal length.")]
		[Range(0f, 256f)]
		[SerializeField] float _blur = 8f;
		
		[Tooltip("The transparency of the source content. Set to zero to make only the outline show.")]
		[Range(0f, 1f)]
		[SerializeField] float _sourceAlpha = 1.0f;

		[Tooltip("The clockwise angle the shadow is cast at. Range is [0..360]. Default is 135.0")]
		[Range(0f, 360f)]
		[SerializeField] float _angle = 135f;

		[Tooltip("The distance the shadow is cast. Range is [0..1]. Default is 0.03")]
		[Range(0f, 256f)]
		[SerializeField] float _distance = 8f;

		[Tooltip("")]
		[Range(-128f, 128f)]
		[SerializeField] float _spread = 0f;

		[Tooltip("The hardness of the shadow [0..4]. Default is 1.0")]
		[Range(0f, 4f)]
		[SerializeField] float _hardness = 1f;

		[Tooltip("The color of the shadow")]
		[SerializeField] Color _color = Color.black;

		[Tooltip("The mode to use for rendering. Default casts the shadow outside, Inset casts the shadow inside.")]
		[SerializeField] DropShadowMode _mode = DropShadowMode.Default;

		/// <summary>How much to downsample before blurring</summary>
		public Downsample Downsample { get { return _downSample; } set { ChangeProperty(ref _downSample, value); } }

		/// <summary>The maximum size of the blur kernel as a fraction of the diagonal length.  So 0.01 would be a kernel with pixel dimensions of 1% of the diagonal length.</summary>
		public float Blur { get { return _blur; } set { ChangeProperty(ref _blur, value); } }

		/// <summary>The transparency of the source content. Set to zero to make only the outline show. Range is [0..1] Default is 1.0</summary>
		public float SourceAlpha { get { return _sourceAlpha; } set { ChangeProperty(ref _sourceAlpha, Mathf.Clamp01(value)); } }

		/// <summary>The clockwise angle the shadow is cast at. Range is [0..360]. Default is 135.0</summary>
		public float Angle { get { return _angle; } set { ChangeProperty(ref _angle, value); } }

		/// <summary>The distance the shadow is cast. Range is [0..1]. Default is 0.03</summary>
		public float Distance { get { return _distance; } set { ChangeProperty(ref _distance, value); } }

		/// <summary></summary>
		public float Spread { get { return _spread; } set { ChangeProperty(ref _spread, value); } }

		/// <summary>The hardness of the shadow [0..2]. Default is 0.5</summary>
		public float Hardness { get { return _hardness; } set { ChangeProperty(ref _hardness, value); } }

		/// <summary>The color of the shadow</summary>
		public Color Color { get { return _color; } set { ChangeProperty(ref _color, value); } }

		/// <summary>The mode to use for rendering. Default casts the shadow outside, Inset casts the shadow inside.</summary>
		public DropShadowMode Mode { get { return _mode; } set { ChangeProperty(ref _mode, value); } }

		private const string BlendDropShadowShaderPath = "Hidden/ChocDino/UIFX/Blend-DropShadow";

		private BoxBlurReference _blurfx = null;
		private ErodeDilate _erodeDilate = null;

		static new class ShaderProp
		{
			public readonly static int SourceAlpha = Shader.PropertyToID("_SourceAlpha");
			public readonly static int ShadowOffset = Shader.PropertyToID("_ShadowOffset");
			public readonly static int ShadowHardness = Shader.PropertyToID("_ShadowHardness");
			public readonly static int ShadowColor = Shader.PropertyToID("_ShadowColor");
		}
		static class ShaderKeyword
		{
			public const string Inset = "INSET";
			public const string Glow = "GLOW";
			public const string Cutout = "CUTOUT";
		}

		internal override bool CanApplyFilter()
        {
            return default;
        }

        protected override bool DoParametersModifySource()
        {
            return default;
        }

        protected override string GetDisplayShaderPath()
        {
            return default;
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

        private static Vector2 AngleToOffset(float angle, Vector2 scale)
        {
            return default;
        }

        protected override void GetFilterAdjustSize(ref Vector2Int leftDown, ref Vector2Int rightUp)
        {
        }

        protected override void SetupDisplayMaterial(Texture source, Texture result)
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