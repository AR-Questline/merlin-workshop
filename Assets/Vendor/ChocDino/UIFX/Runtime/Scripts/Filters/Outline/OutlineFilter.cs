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
	/// <summary></summary>
	public enum OutlineMethod
	{
		/// <summary></summary>
		DistanceMap,
		/// <summary></summary>
		Dilate,
	}

	/// <summary>The direction in which the outline grows from the edge</summary>
	public enum OutlineDirection
	{
		/// <summary>Grow the outline from the edge both inside and outside.</summary>
		Both,
		/// <summary>Grow the outline from the edge only inside.</summary>
		Inside,
		/// <summary>Grow the outline from the edge only outside.</summary>
		Outside,
	}

	/// <summary>
	/// A outline filter for uGUI components
	/// </summary>
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Filters/UIFX - Outline Filter")]
	public class OutlineFilter : FilterBase
	{
		[Tooltip("The algorithm to use for generating the outline.")]
		[SerializeField] OutlineMethod _method = OutlineMethod.DistanceMap;

		[Tooltip("The radius of the outline in pixels.")]
		[Range(0f, 256f)]
		[SerializeField] float _size = 4f;

		/*[Tooltip("The maximum radius of the outline in pixels.")]
		[Range(0f, 256f)]
		[SerializeField] float _maxSize = 4f;*/

		[Tooltip("The shape that the outline grows in.")]
		[SerializeField] DistanceShape _distanceShape = DistanceShape.Circle;

		[Tooltip("The radius of the blur filter in pixels.")]
		[Range(0f, 8f)]
		[SerializeField] float _blur = 0f;

		[Tooltip("The DistanceMap softness falloff pixels.")]
		[Range(0f, 128f)]
		[SerializeField] float _softness = 2f;

		[Tooltip("The transparency of the source content. Set to zero to make only the outline show.")]
		[Range(0f, 1f)]
		[SerializeField] float _sourceAlpha = 1.0f;

		[Tooltip("The color of the outline.")]
		[SerializeField] Color _color = Color.black;

		//[SerializeReference] GradientShader _gradient = new GradientShader();

		[Tooltip("The texture of the outline.")]
		[SerializeField] Texture _texture;

		[SerializeField] Vector2 _textureOffset = Vector2.zero;
		[SerializeField] Vector2 _textureScale = Vector2.one;

		[Tooltip("The direction in which the outline grows from the edge.")]
		[SerializeField] OutlineDirection _direction = OutlineDirection.Outside;

		/// <summary>The direction in which the outline grows from the edge.</summary>
		public OutlineMethod Method { get { return _method; } set { ChangeProperty(ref _method, value); } }

		/// <summary>The radius of the outline in pixels.</summary>
		public float Size { get { return _size; } set { ChangeProperty(ref _size, value); } }

		/// <summary>The shape that the outline grows in.</summary>
		public DistanceShape DistanceShape { get { return _distanceShape; } set { ChangeProperty(ref _distanceShape, value); } }

		/// <summary>The radius of the blur filter in pixels.</summary>
		public float Blur { get { return _blur; } set { ChangeProperty(ref _blur, value); } }

		/// <summary>The DistanceMap softness falloff in pixels.</summary>
		public float Softness { get { return _softness; } set { ChangeProperty(ref _softness, value); } }

		/// <summary>The transparency of the source content. Set to zero to make only the outline show. Range is [0..1] Default is 1.0</summary>
		public float SourceAlpha { get { return _sourceAlpha; } set { ChangeProperty(ref _sourceAlpha, Mathf.Clamp01(value)); } }

		/// <summary>The color of the outline.</summary>
		public Color Color { get { return _color; } set { ChangeProperty(ref _color,value); } }

		/// <summary>The direction in which the outline grows from the edge.</summary>
		public OutlineDirection Direction { get { return _direction; } set { ChangeProperty(ref _direction, value); } }

		private ITextureBlur _blurfx = null;
		private ErodeDilate _erodeDilate = null;
		private DistanceMap _distanceMap = null;

		private const string BlendOutlineShaderPath = "Hidden/ChocDino/UIFX/Blend-Outline";

		static new class ShaderProp
		{
			public readonly static int SourceAlpha = Shader.PropertyToID("_SourceAlpha");
			public readonly static int OutlineColor = Shader.PropertyToID("_OutlineColor");
			public readonly static int Size = Shader.PropertyToID("_Size");
			//public readonly static int FillTex = Shader.PropertyToID("_FillTex");
		}
		static class ShaderKeyword
		{
			public const string Both = "DIR_BOTH";
			public const string Inside = "DIR_INSIDE";
			public const string Outside = "DIR_OUTSIDE";
			public const string DistanceMap = "DISTANCEMAP";
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