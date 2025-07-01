//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	public enum FillTextureWrapMode
	{
		Default,
		Clamp,
		Repeat,
		Mirror,
	}

	/// <summary>
	/// A visual filter that fills a uGUI component using a texture.
	/// </summary>
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Filters/UIFX - Fill Texture Filter")]
	public class FillTextureFilter : FilterBase
	{
		[Tooltip("The texture to fill with.")]
		[SerializeField] Texture _texture = null;

		[Tooltip("The default scale mode to use for a new texture.")]
		[SerializeField] ScaleMode _textureScaleMode = ScaleMode.ScaleToFit;

		[SerializeField] FillTextureWrapMode  _textureWrapMode = FillTextureWrapMode.Default;

		[SerializeField] Color _color = Color.white;

		[Range(0f, 32f)]
		[Tooltip("The ammount to scale the texture by.")]
		[SerializeField] float _textureScale = 1f;

		[Range(0f, 360f)]
		[Tooltip(">The ammount to rotate the texture by in degrees.")]
		[SerializeField] float _textureRotation = 0f;

		[Tooltip("The ammount to offset/translate the texture by.")]
		[SerializeField] Vector2 _textureOffset = Vector2.zero;

		[Tooltip("The speed to scroll the gradient. XY is 2D offset and Z is 2D rotation.")]
		[SerializeField] Vector3 _scrollSpeed = Vector2.zero;

		[Tooltip("How to composite the fill with the source graphic.")]
		[SerializeField] FillGradientBlendMode _blendMode = FillGradientBlendMode.AlphaBlend;

		/// <summary>The texture to fill with.</summary>
		public Texture Texture { get { return _texture; } set { ChangePropertyRef(ref _texture, value); } }

		/// <summary>The default scale mode to use for a new texture.</summary>
		public ScaleMode ScaleMode { get { return _textureScaleMode; } set { ChangeProperty(ref _textureScaleMode, value); } }

		public FillTextureWrapMode WrapMode { get { return _textureWrapMode; } set { ChangeProperty(ref _textureWrapMode, value); } }

		public Color Color { get { return _color; } set { ChangeProperty(ref _color, value); } }

		/// <summary>The ammount to scale the texture by.</summary>
		public float Scale { get { return _textureScale; } set { ChangeProperty(ref _textureScale, value); } }

		/// <summary>The ammount to rotate the texture by in degrees.</summary>
		public float Rotation { get { return _textureRotation; } set { ChangeProperty(ref _textureRotation, value); } }

		/// <summary>The ammount to offset/translate the texture by.</summary>
		public Vector2 Offset { get { return _textureOffset; } set { ChangeProperty(ref _textureOffset, value); } }

		/// <summary>The speed to scroll the gradient. XY is 2D offset and Z is 2D rotation.</summary>
		public Vector3 ScrollSpeed { get { return _scrollSpeed; } set { ChangeProperty(ref _scrollSpeed, value); } }

		/// <summary>How to composite the fill with the source graphic.</summary>
		public FillGradientBlendMode BlendMode { get { return _blendMode; } set { ChangeProperty(ref _blendMode, value); } }

		internal bool IsPreviewScroll { get; set; }

		private Vector3 _scroll = Vector3.zero;

		static new class ShaderProp
		{
			public readonly static int Color = Shader.PropertyToID("_Color");
			public readonly static int FillTex = Shader.PropertyToID("_FillTex");
			public readonly static int FillTexMatrix = Shader.PropertyToID("_FillTex_Matrix");
		}
		static class ShaderKeyword
		{
			public const string WrapClamp = "WRAP_CLAMP";
			public const string WrapRepeat = "WRAP_REPEAT";
			public const string WrapMirror = "WRAP_MIRROR";

			public const string BlendAlphaBlend = "BLEND_ALPHABLEND";
			public const string BlendMultiply = "BLEND_MULTIPLY";
			public const string BlendDarken = "BLEND_DARKEN";
			public const string BlendLighten = "BLEND_LIGHTEN";
			public const string BlendReplaceAlpha = "BLEND_REPLACE_ALPHA";
			public const string BlendBackground = "BLEND_BACKGROUND";
		}

		private const string BlendShaderPath = "Hidden/ChocDino/UIFX/Blend-Fill-Texture";

		protected override string GetDisplayShaderPath()
        {
            return default;
        }

        protected override bool DoParametersModifySource()
        {
            return default;
        }

        internal bool HasScrollSpeed()
        {
            return default;
        }

        public void ResetScroll()
        {
        }

        protected override void OnEnable()
        {
        }

        protected override void Update()
        {
        }

        protected override void SetupDisplayMaterial(Texture source, Texture result)
        {
        }
    }
}