//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using UnityInternal = UnityEngine.Internal;

namespace ChocDino.UIFX
{
	public enum FrameShape
	{
		Rectangle,
		Square,
		Circle,
	}

	[System.Serializable]
	public struct RectPadToEdge
	{
		public bool left, right;
		public bool top, bottom;
	}

	[System.Serializable]
	public struct RectEdge
	{
		public RectEdge(float value) : this()
        {
        }

        public float left;
        public float right;
        public float top;
        public float bottom;

        public Vector4 ToVector()
        {
            return default;
        }
    }

	[System.Serializable]
	public struct RectCorners
	{
		public RectCorners(float value) : this()
        {
        }

        public float topLeft;
        public float topRight;
        public float bottomLeft;
        public float bottomRight;

        public bool IsZero()
        {
            return default;
        }

        public Vector4 ToVector()
        {
            return default;
        }
    }

	public enum FrameRoundCornerMode
	{
		None,
		Small,
		Medium,
		Large,
		Circular,
		Percent,
		CustomPercent,
		Pixels,
		CustomPixels,
	}

	public enum FrameFillMode
	{
		None,
		Color,
		Texture,
		Gradient,
	}

	public enum FrameGradientShape
	{
		Horizontal,
		Vertical,
		Diagonal,
		Radial,
	}

	/// <summary>
	/// </summary>
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Filters/UIFX - Frame Filter")]
	public class FrameFilter : FilterBase
	{
		[SerializeField] FrameShape _shape = FrameShape.Rectangle;

		[Tooltip("")]
		[SerializeField] FrameFillMode _fillMode = FrameFillMode.Color;
		
		[SerializeField] Color _color = Color.black;

		[Tooltip("The texture to use in FrameFillMode.Texture mode.")]
		[SerializeField] Texture _texture = null;

		[Tooltip("")]
		[SerializeField] FrameGradientShape _gradientShape = FrameGradientShape.Horizontal;

		[Tooltip("The gradient to use in FrameFillMode.Gradient mode.")]
		[SerializeField] Gradient _gradient = ColorUtils.GetBuiltInGradient(BuiltInGradient.SoftRainbow);

		[SerializeField] float _gradientRadialRadius = 1f;

		[SerializeField] Sprite _sprite = null;

		[SerializeField] float _radiusPadding = 16f;
		[SerializeField] RectEdge _rectPadding = new RectEdge(16f);
		[SerializeField] RectPadToEdge _rectToEdge;
		[SerializeField] FrameRoundCornerMode _rectRoundCornerMode = FrameRoundCornerMode.Percent;
		[SerializeField] float _rectRoundCornersValue = 0.5f;
		[SerializeField] RectCorners _rectRoundCorners = new RectCorners(0.25f);
		[SerializeField, Min(0f)] float _softness = 0f;
		[SerializeField] bool _cutoutSource = false;
		[SerializeField] Color _borderColor = Color.white;

		[Tooltip("")]
		[SerializeField] FrameFillMode _borderFillMode = FrameFillMode.Color;
		
		[Tooltip("The texture to use in FrameFillMode.Texture mode.")]
		[SerializeField] Texture _borderTexture = null;

		[Tooltip("")]
		[SerializeField] FrameGradientShape _borderGradientShape = FrameGradientShape.Horizontal;

		[Tooltip("The gradient to use in FrameFillMode.Gradient mode.")]
		[SerializeField] Gradient _borderGradient = ColorUtils.GetBuiltInGradient(BuiltInGradient.SoftRainbow);

		[SerializeField] float _borderGradientRadialRadius = 1f;

		[SerializeField, Min(0f)] float _borderSize = 4f;
		[SerializeField, Min(0f)] float _borderSoftness = 0f;

		/// <summary>The shape of the frame.</summary>
		public FrameShape Shape { get { return _shape; } set { ChangeProperty(ref _shape, value); } }

		/// <summary></summary>
		public float Softness { get { return _softness; } set { ChangeProperty(ref _softness, Mathf.Max(0f, value)); } }

		/// <summary>The fill mode to use for the frame.</summary>
		public FrameFillMode FillMode { get { return _fillMode; } set { ChangeProperty(ref _fillMode, value); } }

		/// <summary>The color to use in FrameFillMode.Color mode.</summary>
		public Color Color { get { return _color; } set { ChangeProperty(ref _color, value); } }

		/// <summary>The texture to use in FrameFillMode.Texture mode.</summary>
		public Texture Texture { get { return _texture; } set { ChangePropertyRef(ref _texture, value); } }

		/// <summary>The shape of the gradient in FrameFillMode.Gradient mode.</summary>
		public FrameGradientShape GradientShape { get { return _gradientShape; } set { ChangeProperty(ref _gradientShape, value); } }

		/// <summary>The gradient to use in FrameFillMode.Gradient mode.</summary>
		public Gradient Gradient { get { return _gradient; } set { ChangePropertyRef(ref _gradient, value); } }

		/// <summary></summary>
		public float GradientRadialRadius { get { return _gradientRadialRadius; } set { ChangeProperty(ref _gradientRadialRadius, value); } }

		/// <summary></summary>
		public float RadiusPadding { get { return _radiusPadding; } set { ChangeProperty(ref _radiusPadding, value); } }

		/// <summary></summary>
		public RectEdge RectPadding { get { return _rectPadding; } set { ChangeProperty(ref _rectPadding, value); } }

		/// <summary></summary>
		public RectPadToEdge RectToEdge { get { return _rectToEdge; } set { ChangeProperty(ref _rectToEdge, value); } }

		/// <summary></summary>
		public FrameRoundCornerMode RectRoundCornerMode { get { return _rectRoundCornerMode; } set { ChangeProperty(ref _rectRoundCornerMode, value); } }

		/// <summary></summary>
		public float RectRoundCornersValue { get { return _rectRoundCornersValue; } set { ChangeProperty(ref _rectRoundCornersValue, value); } }

		/// <summary></summary>
		public RectCorners RectRoundCorners { get { return _rectRoundCorners; } set { ChangeProperty(ref _rectRoundCorners, value); } }

		/// <summary></summary>
		public bool CutoutSource { get { return _cutoutSource; } set { ChangeProperty(ref _cutoutSource, value); } }

		/// <summary>The fill mode to use for the frame border.</summary>
		public FrameFillMode BorderFillMode { get { return _borderFillMode; } set { ChangeProperty(ref _borderFillMode, value); } }

		/// <summary></summary>
		public float BorderSize { get { return _borderSize; } set { ChangeProperty(ref _borderSize, Mathf.Max(0f, value)); } }

		/// <summary></summary>
		public float BorderSoftness { get { return _borderSoftness; } set { ChangeProperty(ref _borderSoftness, Mathf.Max(0f, value)); } }

		/// <summary></summary>
		public Color BorderColor { get { return _borderColor; } set { ChangeProperty(ref _borderColor, value); } }

		/// <summary>The texture to use for the border in FrameFillMode.Texture mode.</summary>
		public Texture BorderTexture { get { return _borderTexture; } set { ChangePropertyRef(ref _borderTexture, value); } }

		/// <summary>The shape of the border gradient in FrameFillMode.Gradient mode.</summary>
		public FrameGradientShape BorderGradientShape { get { return _borderGradientShape; } set { ChangeProperty(ref _borderGradientShape, value); } }

		/// <summary>The gradient to use in FrameFillMode.Gradient mode.</summary>
		public Gradient BorderGradient { get { return _borderGradient; } set { ChangePropertyRef(ref _borderGradient, value); } }

		/// <summary></summary>
		public float BorderGradientRadialRadius { get { return _borderGradientRadialRadius; } set { ChangeProperty(ref _borderGradientRadialRadius, value); } }

		internal class FrameShader
		{
			internal const string Id = "Hidden/ChocDino/UIFX/Blend-Frame";

			internal static class Prop
			{
				internal static readonly int CutoutAlpha = Shader.PropertyToID("_CutoutAlpha");
				internal static readonly int Rect_ST = Shader.PropertyToID("_Rect_ST");
				internal static readonly int EdgeRounding = Shader.PropertyToID("_EdgeRounding");
				internal static readonly int FillColor = Shader.PropertyToID("_FillColor");
				internal static readonly int FillTex = Shader.PropertyToID("_FillTex");
				internal static readonly int GradientAxisParams = Shader.PropertyToID("_GradientAxisParams");
				internal static readonly int GradientParams = Shader.PropertyToID("_GradientParams");
				internal static readonly int FillSoft = Shader.PropertyToID("_FillSoft");
				internal static readonly int BorderColor = Shader.PropertyToID("_BorderColor");
				internal static readonly int BorderFillTex = Shader.PropertyToID("_BorderFillTex");
				internal static readonly int BorderGradientAxisParams = Shader.PropertyToID("_BorderGradientAxisParams");
				internal static readonly int BorderSizeSoft = Shader.PropertyToID("_BorderSizeSoft");
				internal static readonly int BorderGradientParams = Shader.PropertyToID("_BorderGradientParams");
				
			}
			internal static class Keyword
			{
				internal const string Cutout = "CUTOUT";
				internal const string Border = "BORDER";
				internal const string ShapeCircle = "SHAPE_CIRCLE";
				internal const string ShapeRoundRect = "SHAPE_ROUNDRECT";
				internal const string UseTexture = "USE_TEXTURE";
				internal const string UseBorderTexture = "USE_BORDER_TEXTURE";
				internal const string GradientRadial = "GRADIENT_RADIAL";
				internal const string GradientRadialBorder = "GRADIENT_RADIAL_BORDER";
			}
		}

		private GradientTexture _textureFromGradient = new GradientTexture(128);
		private GradientTexture _borderTextureFromGradient = new GradientTexture(128);
		//private FrameRoundCornerMode _previousRectRoundCornerMode = FrameRoundCornerMode.None;

		protected override string GetDisplayShaderPath()
        {
            return default;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
        }
#endif

        /*protected void LateUpdate()
		{
			var rt = this.GetComponent<RectTransform>();
			var result = _screenRect.GetRect();
			Debug.Log(result);
			Debug.Log(ResolutionScalingFactor);
			rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, result.width * ResolutionScalingFactor);
			rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, result.height * ResolutionScalingFactor);
			//rt.sizeDelta = new Vector2(result.width, result.height);
		}*/

        /*
		void OnRectRoundCornerModeChanged()
		{
			if (_previousRectRoundCornerMode != FrameRoundCornerMode.None)
			{
				bool fromPercent = false;
				switch (_previousRectRoundCornerMode)
				{
					case FrameRoundCornerMode.Small:
					case FrameRoundCornerMode.Medium:
					case FrameRoundCornerMode.Large:
					case FrameRoundCornerMode.Circular:
					case FrameRoundCornerMode.Percent:
					case FrameRoundCornerMode.CustomPercent:
					fromPercent = true;
					break;
				}
				bool toPercent = false;
				switch (_rectRoundCornerMode)
				{
					case FrameRoundCornerMode.Small:
					case FrameRoundCornerMode.Medium:
					case FrameRoundCornerMode.Large:
					case FrameRoundCornerMode.Circular:
					case FrameRoundCornerMode.Percent:
					case FrameRoundCornerMode.CustomPercent:
					toPercent = true;
					break;
				}
				if (fromPercent && !toPercent)
				{
					// TODO: finish this code
					Rect geometryRect = _screenRect.GetRect();
					float size = Mathf.Min(geometryRect.width - _borderSize * 2f, geometryRect.height - _borderSize * 2f) * 0.5f;
					_rectRoundCorners.topLeft = _rectRoundCorners.topLeft * size;
					_rectRoundCorners.topRight = _rectRoundCorners.topRight * size;
					_rectRoundCorners.bottomLeft = _rectRoundCorners.bottomLeft * size;
					_rectRoundCorners.bottomRight = _rectRoundCorners.bottomRight * size;
					_rectRoundCornersValue = _rectRoundCorners.Average;
				}
				else if (!fromPercent && toPercent)
				{
					// TODO: finish this code
					Rect geometryRect = _screenRect.GetRect();
					float size = Mathf.Min(geometryRect.width - _borderSize * 2f, geometryRect.height - _borderSize * 2f) * 0.5f;
					_rectRoundCorners.topLeft = _rectRoundCorners.topLeft * size;
					_rectRoundCorners.topRight = _rectRoundCorners.topRight * size;
					_rectRoundCorners.bottomLeft = _rectRoundCorners.bottomLeft * size;
					_rectRoundCorners.bottomRight = _rectRoundCorners.bottomRight * size;
					_rectRoundCornersValue = _rectRoundCorners.Average;	
				}
			}
			_previousRectRoundCornerMode = _rectRoundCornerMode;
		}*/

        protected override bool DoParametersModifySource()
        {
            return default;
        }

        protected override void OnEnable()
        {
        }

        protected override void OnDisable()
        {
        }

        protected override void GetFilterAdjustSize(ref Vector2Int leftDown, ref Vector2Int rightUp)
        {
        }

        private bool IsBorderVisible()
        {
            return default;
        }

        private bool HasRoundCorners()
        {
            return default;
        }

        protected override void SetupDisplayMaterial(Texture source, Texture result)
        {
        }

#if false
		private Mesh _slicedMesh;
		private CommandBuffer _cb;
		private VertexHelper _vh;
		private RenderTexture _rt;

		protected override RenderTexture RenderFilters(RenderTexture source)
		{
			/*RenderTextureHelper.ReleaseTemporary(ref _rt);
			_rt = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);

			if (_slicedMesh == null)
			{
				_slicedMesh = new Mesh();
			}

			RectInt textureRect = _screenRect.GetTextureRect();

			_vh = new VertexHelper();
			_vh.Clear();
			SlicedSprite.Generate9SliceGeometry_Tile(_sprite, new Rect(0f, 0f, textureRect.width, textureRect.height), true, Color.white, 1f, _vh);
			_vh.FillMesh(_slicedMesh);
			_vh.Dispose(); _vh = null;

			Graphic.defaultGraphicMaterial.mainTexture = _sprite.texture;

			if (_cb == null)
			{
				_cb = new CommandBuffer();
			}
			_cb.Clear();
			_cb.SetRenderTarget(new RenderTargetIdentifier(_rt));
			_cb.ClearRenderTarget(false, true, Color.clear, 0f);
			_cb.SetViewMatrix(Matrix4x4.identity);
			var projectionMatrix = Matrix4x4.Ortho(0f, textureRect.width, 0f, textureRect.height, -1000f, 1000f);
			projectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, false);
			_cb.SetProjectionMatrix(projectionMatrix);
			_cb.DrawMesh(_slicedMesh, Matrix4x4.identity, Graphic.defaultGraphicMaterial);
			Graphics.ExecuteCommandBuffer(_cb);

			return _rt;*/

			return source;
		}
#endif

    }
}