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
	/// <summary>
	/// A gooey filter for uGUI components
	/// </summary>
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Filters/UIFX - Gooey Filter")]
	public class GooeyFilter : FilterBase
	{
		[Tooltip("The radius in pixels to dilate the edges of the graphic. Default is 2.0")]
		[Range(0f, 32f)]
		[SerializeField] float _size = 2f;
		
		[Tooltip("The radius of the blur in pixels. Default is 28.0")]
		[Range(0f, 64f)]
		[SerializeField] float _blur = 28f;

		[Tooltip("Threshold controls the value used to clip the alpha channel. Default is 0.35")]
		[Range(0f, 1f)]
		[SerializeField] float _threshold = 0.35f;

		[Tooltip("Threshold falloff controls how soft or hard the threshold is. Default is 0.5")]
		[Range(0f, 1f)]
		[SerializeField] float _thresholdFalloff = 0.5f;

		/// <summary>The radius in pixels to dilate the edges of the graphic. Default is 2.0</summary>
		public float Size { get { return _size; } set { ChangeProperty(ref _size,value); } }

		/// <summary>he radius of the blur in pixels. Default is 28.0</summary>
		public float Blur { get { return _blur; } set { ChangeProperty(ref _blur, value); } }

		/// <summary>Threshold controls the value used to clip the alpha channel. Default is 0.35</summary>
		public float Threshold { get { return _threshold; } set { ChangeProperty(ref _threshold, value); } }

		/// <summary>Threshold falloff controls how soft or hard the threshold is. Default is 0.5</summary>
		public float ThresholdFalloff { get { return _thresholdFalloff; } set { ChangeProperty(ref _thresholdFalloff, value); } }

		private const string BlendOutlineShaderPath = "Hidden/ChocDino/UIFX/Blend-Gooey";

		private ITextureBlur _blurfx = null;
		private ErodeDilate _erodeDilate = null;

		static new class ShaderProp
		{
			public readonly static int ThresholdOffset = Shader.PropertyToID("_ThresholdOffset");
			public readonly static int ThresholdScale = Shader.PropertyToID("_ThresholdScale");
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

        private void SetupFilterParams()
        {
        }

        protected override RenderTexture RenderFilters(RenderTexture source)
        {
            return default;
        }
    }
}