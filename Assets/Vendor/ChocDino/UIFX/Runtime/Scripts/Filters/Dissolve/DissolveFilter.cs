//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	public enum DissolveEdgeColorMode
	{
		None,
		Color,
		Ramp,
	}
	/// <summary>
	/// </summary>
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Filters/UIFX - Dissolve Filter")]
	public class DissolveFilter : FilterBase
	{
		[Range(0f, 1f)]
		[SerializeField] float _dissolve = 0.25f;
		[SerializeField] Texture _texture = null;
		[SerializeField] ScaleMode _textureScaleMode = ScaleMode.ScaleAndCrop;
		[SerializeField] float _scale = 1f;
		[SerializeField] bool _invert = false;

		[Range(0f, 1f)]
		[SerializeField] float _edgeLength = 0.1f;
		[SerializeField] DissolveEdgeColorMode _edgeColorMode;
		[ColorUsageAttribute(showAlpha: false, hdr: false)]
		[SerializeField] Color _edgeColor = Color.black;
		[SerializeField] Texture _edgeTexture = null;
		[Range(0f, 100f)]
		[SerializeField] float _edgeEmissive = 0f;

		public float Dissolve { get { return _dissolve; } set { ChangeProperty(ref _dissolve, value); } }
		public Texture Texture { get { return _texture; } set { ChangePropertyRef(ref _texture, value); } }
		public ScaleMode TextureScaleMode { get { return _textureScaleMode; } set { ChangeProperty(ref _textureScaleMode, value); } }
		public float TextureScale { get { return _scale; } set { ChangeProperty(ref _scale, value); } }
		public bool TextureInvert { get { return _invert; } set { ChangeProperty(ref _invert, value); } }
		public float EdgeLength { get { return _edgeLength; } set { ChangeProperty(ref _edgeLength, value); } }
		public DissolveEdgeColorMode EdgeColorMode { get { return _edgeColorMode; } set { ChangeProperty(ref _edgeColorMode, value); } }
		public Color EdgeColor { get { return _edgeColor; } set { ChangeProperty(ref _edgeColor, value); } }
		public Texture EdgeTexture { get { return _edgeTexture; } set { ChangePropertyRef(ref _edgeTexture, value); } }
		public float EdgeEmissive { get { return _edgeEmissive; } set { ChangeProperty(ref _edgeEmissive, value); } }

		static new class ShaderProp
		{
			public readonly static int Dissolve = Shader.PropertyToID("_Dissolve");
			public readonly static int FillTex = Shader.PropertyToID("_FillTex");
			public readonly static int EdgeTex = Shader.PropertyToID("_EdgeTex");
			public readonly static int EdgeColor = Shader.PropertyToID("_EdgeColor");
			public readonly static int EdgeEmissive = Shader.PropertyToID("_EdgeEmissive");
			public readonly static int InvertFactor = Shader.PropertyToID("_InvertFactor");
		}
		static class ShaderKeyword
		{
			public const string EdgeColor = "EDGE_COLOR";
			public const string EdgeRamp = "EDGE_RAMP";
		}

		private const string BlendShaderPath = "Hidden/ChocDino/UIFX/Blend-Dissolve";
		private readonly static Vector4 InvertFalse = new Vector4(0f, -1f, 0f, 0f);
		private readonly static Vector4 InvertTrue = new Vector4(1f, 1f, 0f, 0f);

		protected override string GetDisplayShaderPath()
        {
            return default;
        }

        protected override bool DoParametersModifySource()
        {
            return default;
        }

        protected override void OnEnable()
        {
        }

        protected override void SetupDisplayMaterial(Texture source, Texture result)
        {
        }
    }
}