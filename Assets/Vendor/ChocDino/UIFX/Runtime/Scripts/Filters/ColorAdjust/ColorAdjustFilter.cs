//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	/// <summary>
	/// A color adjustment filter for uGUI
	/// </summary>
	[ExecuteInEditMode]
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Filters/UIFX - Color Adjust Filter")]
	public class ColorAdjustFilter : FilterBase
	{
		[Range(0f, 360f)]
		[SerializeField] float _hue = 0.0f;
		[Range(-1f, 1f)]
		[SerializeField] float _saturation = 0.0f;
		[Range(-1f, 1f)]
		[SerializeField] float _value = 0.0f;
		[Range(-1f, 1f)]
		[SerializeField] float _brightness = 0.0f;
		[Range(-2f, 2f)]
		[SerializeField] float _contrast = 0.0f;
		[Range(1f, 255f)]
		[SerializeField] float _posterize = 255f;
		[Range(0f, 1f)]
		[SerializeField] float _opacity = 1f;

		[SerializeField] Vector4 _brightnessRGBA = Vector4.zero;
		[SerializeField] Vector4 _contrastRGBA = Vector4.zero;
		[SerializeField] Vector4 _posterizeRGBA = new Vector4(255f, 255f, 255f, 255f);

		public float Hue { get { return _hue; } set { ChangeProperty(ref _hue, Mathf.Clamp(value, 0f, 360f)); } }
		public float Saturation { get { return _saturation; } set { ChangeProperty(ref _saturation, Mathf.Clamp(value, -2f, 2f)); } }
		public float Value { get { return _value; } set { ChangeProperty(ref _value, Mathf.Clamp(value, -1f, 1f)); } }
		public float Brightness { get { return _brightness; } set { ChangeProperty(ref _brightness, Mathf.Clamp(value, -2f, 2f)); } }
		public float Contrast { get { return _contrast; } set { ChangeProperty(ref _contrast, Mathf.Clamp(value, -2f, 2f)); } }
		public float Posterize { get { return _posterize; } set { ChangeProperty(ref _posterize, Mathf.Clamp(value, 1f, 255f)); } }
		public float Opacity { get { return _opacity; } set { ChangeProperty(ref _opacity, Mathf.Clamp01(value)); } }
		public Vector4 BrightnessRGBA { get { return _brightnessRGBA; } set { ChangeProperty(ref _brightnessRGBA, value); } }
		public Vector4 ContrastRGBA { get { return _contrastRGBA; } set { ChangeProperty(ref _contrastRGBA, value); } }
		public Vector4 PosterizeRGBA { get { return _posterizeRGBA; } set { ChangeProperty(ref _posterizeRGBA, value); } }

		private ColorAdjust _filter;

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

        protected override RenderTexture RenderFilters(RenderTexture source)
        {
            return default;
        }
    }
}