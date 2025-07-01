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
	public enum BlurAlgorithm
	{
		Box = 0,
		[InspectorName("Multi Box")]
		MultiBox = 100,
		Gaussian = 1000,
	}

	/// <summary>
	/// A blur filter for uGUI components
	/// </summary>
	[ExecuteInEditMode]
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Filters/UIFX - Blur Filter")]
	public partial class BlurFilter : FilterBase
	{
		[SerializeField] BlurAlgorithm _algorithm = BlurAlgorithm.MultiBox;

		[Tooltip("How much to downsample before blurring")]
		[SerializeField] Downsample _downSample = Downsample.Auto;

		[Tooltip("Which axes to blur")]
		[SerializeField] BlurAxes2D _blurAxes2D = BlurAxes2D.Default;

		[Tooltip("The maximum size of the blur kernel as a fraction of the diagonal length.  So 0.01 would be a kernel with pixel dimensions of 1% of the diagonal length.")]
		[Range(0f, 500f)]
		[SerializeField] float _blur = 8f;

		[Tooltip("Toggle the use of the alpha curve to fade to transparent as blur Strength increases")]
		[SerializeField] bool _applyAlphaCurve = false;

		[Tooltip("An optional curve to allow the Graphic to fade to transparent as the blur Strength property increases")]
		[SerializeField] AnimationCurve _alphaCurve = new AnimationCurve(new Keyframe(0f, 1f, -1f, -1f), new Keyframe(1f, 0f, -1f, -1f));

		/// <summary></summary>
		public BlurAlgorithm Algorithm { get { return _algorithm; } set { if (ChangeProperty(ref _algorithm, value)) { UpdateAlgorithm(); } } }

		/// <summary>How much to downsample before blurring</summary>
		public Downsample Downsample { get { return _downSample; } set { ChangeProperty(ref _downSample, value); } }

		/// <summary>Which axes to blur</summary>
		public BlurAxes2D BlurAxes2D { get { return _blurAxes2D; } set { ChangeProperty(ref _blurAxes2D, value); } }

		/// <summary>The maximum size of the blur kernel as a fraction of the diagonal length.  So 0.01 would be a kernel with pixel dimensions of 1% of the diagonal length.</summary>
		public float Blur { get { return _blur; } set { ChangeProperty(ref _blur, value); } }

		/// <summary>Toggle the use of the alpha curve to fade to transparent as blur Strength increases</summary>
		public bool ApplyAlphaCurve { get { return _applyAlphaCurve; } set { ChangeProperty(ref _applyAlphaCurve, value); } }

		/// <summary>An optional curve to allow the Graphic to fade to transparent as the blur Strength property increases</summary>
		public AnimationCurve AlphaCurve { get { return _alphaCurve; } set { ChangePropertyRef(ref _alphaCurve, value); } }

		private float _lastGlobalStrength = 1f;

		/// <summary>A global scale for Strength which can be useful to easily adjust Strength across all instances of BlurFilter.  Range [0..1] Default is 1.0</summary>
		public static float GlobalStrength = 1f;

		//private const string Keyword_BlendOver = "BLEND_OVER";
		//private const string Keyword_BlendUnder = "BLEND_UNDER";

		private BoxBlurReference _boxBlur = null;
		private GaussianBlurReference _gaussBlur = null;
		private ITextureBlur _currentBlur = null;

		internal override bool CanApplyFilter()
        {
            return default;
        }

        protected override bool DoParametersModifySource()
        {
            return default;
        }

        private void UpdateAlgorithm()
        {
        }

        private void ChangeAlgorithm()
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

        protected override void Update()
        {
        }

        /// <summary>
        /// SetGlobalStrength() allows Unity Events "Dynamic Float" to set the Global Strength static property
        /// </summary>
        public void SetGlobalStrength(float value)
        {
        }

        private float GetStrength()
        {
            return default;
        }

        protected override float GetAlpha()
        {
            return default;
        }

        /*
       protected override void SetupDisplayMaterial(Texture source, Texture result)
       {
           //_displayMaterial.EnableKeyword(Keyword_BlendOver);
           //_displayMaterial.DisableKeyword(Keyword_BlendUnder);

           _displayMaterial.DisableKeyword(Keyword_BlendOver);
           _displayMaterial.EnableKeyword(Keyword_BlendUnder);

           base.SetupDisplayMaterial(source, result);
       }
*/

        protected override void GetFilterAdjustSize(ref Vector2Int leftDown, ref Vector2Int rightUp)
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