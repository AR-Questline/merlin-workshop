//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	/// <summary>How to composite the FillColorFilter fill with the source graphic.</summary>
	public enum FillColorComposite
	{
		/// <summary>Replace the foreground with the fill.</summary>
		ForegroundReplace,
		/// <summary>Multiply the foreground with the fill.</summary>
		ForegroundMultiply,
		/// <summary>Replace the background with the fill and cutout the foreground.</summary>
		BackgroundReplace,
		/// <summary>Replace the background with the fill and cutout the foreground.</summary>
		Cutout,
	}

	/// <summary>Fill modes for the FillColorFilter component.</summary>
	public enum FillColorMode
	{
		/// <summary>A single solid color.</summary>
		Solid,
		/// <summary>Two colors - one on the left edge, and one on the right edge.</summary>
		Horizontal,
		/// <summary>Two colors - one on the top edge, and one on the bottom edge.</summary>
		Vertical,
		/// <summary>Four colors at each of the corners of the rectangle, creating a gradient fill effect.</summary>
		Corners,
	}

	/// <summary>
	/// A visual filter that fills a uGUI component with a color.
	/// </summary>
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Filters/UIFX - Fill Color Filter")]
	public class FillColorFilter : FilterBase
	{
		[Tooltip("The fill mode to use.")]
		[SerializeField] FillColorMode _mode = FillColorMode.Solid;

		[Tooltip("The color of the fill in Solid mode.")]
		[SerializeField] Color _color = Color.red;

		[Tooltip("The color of top edge in Vertical mode. The color of the left edge in Horizontal mode.")]
		[SerializeField] Color _colorA = Color.red;

		[Tooltip("The color of bottom edge in Vertical mode. The color of the right edge in Horizontal mode.")]
		[SerializeField] Color _colorB = Color.blue;

		[Tooltip("The color of top-left corner in Corners mode.")]
		[SerializeField] Color _colorTL = Color.red;

		[Tooltip("The color of top-right corner in Corners mode.")]
		[SerializeField] Color _colorTR = Color.green;

		[Tooltip("The color of bottom-left corner in Corners mode.")]
		[SerializeField] Color _colorBL = Color.blue;

		[Tooltip("The color of bottom-right corner in Corners mode.")]
		[SerializeField] Color _colorBR = Color.magenta;

		[Tooltip("Used to scale the positions of the edge/corner colors in Horizonal/Vertical/Corners mode. Higher values move the colors towards the center of the rectangle.")]
		[SerializeField] float _colorScale = 1f;

		[Tooltip("Used to bias the position of the edge colors in Horizonal/Vertical mode.")]
		[Range(-1f, 1f)]
		[SerializeField] float _colorBias = 0f;

		[Tooltip("How to composite the fill with the source graphic.")]
		[SerializeField] FillColorComposite _compositeMode = FillColorComposite.ForegroundReplace;

		/// <summary>The fill mode to use.</summary>
		public FillColorMode Mode { get { return _mode; } set { ChangeProperty(ref _mode, value); } }

		/// <summary>The color of the fill in Solid mode.</summary>
		public Color Color { get { return _color; } set { ChangeProperty(ref _color, value); } }

		/// <summary>The color of top edge in Vertical mode. The color of the left edge in Horizontal mode.</summary>
		public Color ColorA { get { return _colorA; } set { ChangeProperty(ref _colorA, value); } }

		/// <summary>The color of bottom edge in Vertical mode. The color of the right edge in Horizontal mode.</summary>
		public Color ColorB { get { return _colorB; } set { ChangeProperty(ref _colorB, value); } }

		/// <summary>The color of top-left corner in Corners mode.</summary>
		public Color ColorTL { get { return _colorTL; } set { ChangeProperty(ref _colorTL, value); } }

		/// <summary>The color of top-right corner in Qorners mode.</summary>
		public Color ColorTR { get { return _colorTR; } set { ChangeProperty(ref _colorTR, value); } }

		/// <summary>The color of bottom-left corner in Corners mode.</summary>
		public Color ColorBL { get { return _colorBL; } set { ChangeProperty(ref _colorBL, value); } }

		/// <summary>The color of bottom-right corner in Corners mode.</summary>
		public Color ColorBR { get { return _colorBR; } set { ChangeProperty(ref _colorBR, value); } }

		/// <summary>Used to scale the positions of the corner colors in Horizonal/Vertical/Corners mode. Higher values move the colors towards the center of the rectangle.</summary>
		public float ColorScale { get { return _colorScale; } set { ChangeProperty(ref _colorScale, value); } }

		/// <summary>Used to bias the position of the edge colors in Horizonal/Vertical mode.</summary>
		public float ColorBias { get { return _colorScale; } set { ChangeProperty(ref _colorBias, value); } }

		/// <summary>How to composite the fill with the source graphic.</summary>
		public FillColorComposite Composite { get { return _compositeMode; } set { ChangeProperty(ref _compositeMode, value); } }

		static new class ShaderProp
		{
			public readonly static int FillColor = Shader.PropertyToID("_FillColor");
			public readonly static int FillColorA = Shader.PropertyToID("_FillColorA");
			public readonly static int FillColorB = Shader.PropertyToID("_FillColorB");
			public readonly static int FillColorTL = Shader.PropertyToID("_FillColorTL");
			public readonly static int FillColorTR = Shader.PropertyToID("_FillColorTR");
			public readonly static int FillColorBL = Shader.PropertyToID("_FillColorBL");
			public readonly static int FillColorBR = Shader.PropertyToID("_FillColorBR");
			public readonly static int ColorScaleBias = Shader.PropertyToID("_ColorScaleBias");
		}
		static class ShaderKeyword
		{
			public const string CompFgMultiply = "COMP_FG_MULTIPLY";
			public const string CompFgReplace = "COMP_FG_REPLACE";
			public const string CompBgReplace = "COMP_BG_REPLACE";
			public const string CompCutout = "COMP_CUTOUT";
			public const string ModeHorizontal = "MODE_HORIZONTAL";
			public const string ModeVertical = "MODE_VERTICAL";
			public const string ModeCorners = "MODE_CORNERS";
		}

		private const string BlendShaderPath = "Hidden/ChocDino/UIFX/Blend-Fill-Color";

		protected override string GetDisplayShaderPath()
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