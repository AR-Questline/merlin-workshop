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
	/// A long shadow filter for uGUI components
	/// </summary>
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Filters/UIFX - Long Shadow Filter")]
	public class LongShadowFilter : FilterBase
	{
		[Tooltip("The algorithm to use for rendering the shadow")]
		[SerializeField] LongShadowMethod _method = LongShadowMethod.Normal;

		[Tooltip("The clockwise angle the shadow is cast at. Range is [0..360]. Default is 135.0")]
		[Range(0f, 360f)]
		[SerializeField] float _angle = 135f;

		[Tooltip("The distance the shadow is cast. Range is [0..512]. Default is 32.0")]
		[Range(-1024f, 1024f)]
		[SerializeField] float _distance = 32f;

		[Tooltip("The number of pixels to step. Range is [1..64]. Default is 1.0")]
		[Range(1f, 64f)]
		[SerializeField] float _stepSize = 1f;

		[Tooltip("The pivot can be used for animating the shadow. A value of -1 means the front is located at the position of the back. A value of 0.0 means the front and back are at their furthest distance apart. A value of 1.0 means the back is located at the position of the front. Range is [-1..1]. Default is 0.0")]
		[Range(-1f, 1f)]
		[SerializeField] float _pivot = 0f;

		[Tooltip("The color of the main/front of the shadow")]
		[SerializeField] Color _colorFront = Color.black;

		[Tooltip("Enable use of a secondary color for the back")]
		[SerializeField] bool _useBackColor = false;

		[Tooltip("The color of the back of the shadow")]
		[SerializeField] Color _colorBack = Color.clear;

		[Tooltip("The transparency of the source content. Set to zero to make only the outline show.")]
		[Range(0f, 1f)]
		[SerializeField] float _sourceAlpha = 1.0f;

		[Tooltip("The composite mode to use for rendering.")]
		[SerializeField] LongShadowCompositeMode _compositeMode = LongShadowCompositeMode.Normal;

		/// <summary>The algorithm to use for rendering the shadow</summary>
		public LongShadowMethod Method { get { return _method; } set { ChangeProperty(ref _method, value); } }

		/// <summary>The clockwise angle the shadow is cast at. Range is [0..360]. Default is 135.0</summary>
		public float Angle { get { return _angle; } set { ChangeProperty(ref _angle, value); } }

		/// <summary>The distance the shadow is cast. Range is [-512..512]. Default is 32</summary>
		public float Distance { get { return _distance; } set { ChangeProperty(ref _distance, value); } }

		/// <summary>The number of pixels to step. Range is [1..64]. Default is 1.0</summary>
		public float StepSize { get { return _stepSize; } set { ChangeProperty(ref _stepSize, value); } }

		/// <summary>The pivot can be used for animating the shadow. A value of -1 means the front is located at the position of the back. A value of 0.0 means the front and back are at their furthest distance apart. A value of 1.0 means the back is located at the position of the front. Range is [-1..1]. Default is 0.0</summary>
		public float Pivot { get { return _pivot; } set { ChangeProperty(ref _pivot, value); } }

		/// <summary>The color of the main/front of the shadow</summary>
		public Color ColorFront { get { return _colorFront; } set { ChangeProperty(ref _colorFront, value); } }

		/// <summary>The color of the main/front of the shadow</summary>
		public bool UseBackColor { get { return _useBackColor; } set { ChangeProperty(ref _useBackColor, value); } }

		/// <summary>The color of the back of the shadow</summary>
		public Color ColorBack { get { return _colorBack; } set { ChangeProperty(ref _colorBack, value); } }

		/// <summary>The transparency of the source content. Set to zero to make only the outline show. Range is [0..1] Default is 1.0</summary>
		public float SourceAlpha { get { return _sourceAlpha; } set { ChangeProperty(ref _sourceAlpha, value); } }

		/// <summary>The composite mode to use for rendering.</summary>
		public LongShadowCompositeMode CompositeMode { get { return _compositeMode; } set { ChangeProperty(ref _compositeMode, value); } }

		private LongShadow _effect = null;
		private DistanceMap _distanceMap = null;

		private const string BlendOutlineShaderPath = "Hidden/ChocDino/UIFX/Blend-LongShadow";

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