//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	public class Extrude
	{
		public ExtrudeProjection Projection { get { return _projection; } set { ChangeProperty(ref _projection, value); } }
		public float Angle { get { return _angle; } set { ChangeProperty(ref _angle, value); } }
		public float Distance { get { return _distance; } set { ChangeProperty(ref _distance, value); } }
		public float PerspectiveDistance { get { return _perspectiveDistance; } set { ChangeProperty(ref _perspectiveDistance, value); } }
		public Color Color1 { get { return _color1; } set { ChangeProperty(ref _color1, value); } }
		public Color Color2 { get { return _color2; } set { ChangeProperty(ref _color2, value); } }
		public bool UseGradientTexture { get { return _useGradientTexture; } set { _useGradientTexture = value; _materialsDirty = true; } }
		public Texture GradientTexture { get { return _gradientTexture; } set { _gradientTexture = value; _materialsDirty = true; } }
		public bool ReverseFill { get { return _reverseFill; } set { ChangeProperty(ref _reverseFill, value); } }
		public float Scroll { get { return _scroll; } set { ChangeProperty(ref _scroll, value); } }
		public bool MultiplySource { get { return _multiplySource; } set { ChangeProperty(ref _multiplySource, value); } }

		public Rect RectRatio { get { return _rectRatio; } set { ChangeProperty(ref _rectRatio, value); } }

		private ExtrudeProjection _projection = ExtrudeProjection.Perspective;
		private float _angle = 135f;
		private float _distance = 8f;
		private float _perspectiveDistance = 0f;
		private bool _useGradientTexture = false;
		private Color _color1 = Color.black;
		private Color _color2 = Color.black;
		private Texture _gradientTexture;
		private bool _reverseFill = false;
		private float _scroll = 0f;
		private bool _multiplySource = true;
		private Rect _rectRatio;

		private Material _material;
		private RenderTexture _rt;
		private int _sourceTextureId;
		private Vector2Int _sourceTextureSize;

		private bool _materialsDirty = true;
		private FilterBase _parentFilter = null;
		
		const string ShaderId = "Hidden/ChocDino/UIFX/Extrude";

		static class ShaderProp
		{
			public static readonly int Length = Shader.PropertyToID("_Length");
			public static readonly int PixelStep = Shader.PropertyToID("_PixelStep");
			public static readonly int ColorFront = Shader.PropertyToID("_ColorFront");
			public static readonly int ColorBack = Shader.PropertyToID("_ColorBack");
			public static readonly int GradientTex = Shader.PropertyToID("_GradientTex");
			public static readonly int VanishingPoint = Shader.PropertyToID("_VanishingPoint");
			public static readonly int Ratio = Shader.PropertyToID("_Ratio");
			public static readonly int ReverseFill = Shader.PropertyToID("_ReverseFill");
			public static readonly int Scroll = Shader.PropertyToID("_Scroll");
		}
		static class ShaderKeyword
		{
			public const string UseGradientTexture = "USE_GRADIENT_TEXTURE";
			public const string MultiplySourceColor = "MULTIPLY_SOURCE_COLOR";
		}
		static class ShaderPass
		{
			public const int Perspective = 0;
			public const int Orthographic = 1;
		}

		private Extrude() {
        }

        public Extrude(FilterBase parentFilter)
        {
        }

        private void ChangeProperty<T>(ref T backing, T value) 	where T : struct
        {
        }

        protected void ChangePropertyRef<T>(ref T backing, T value) where T : class
        {
        }

        internal static Vector2 AngleToOffset(float angle, Vector2 scale)
        {
            return default;
        }

        private Rect _sourceRect;

		public bool GetAdjustedBounds(Rect rect, ref Vector2Int leftDown, ref Vector2Int rightUp)
        {
            return default;
        }

        public RenderTexture Process(RenderTexture sourceTexture)
        {
            return default;
        }

        public void FreeResources()
        {
        }

        void SetupResources(RenderTexture sourceTexture)
        {
        }

        private float GetScaledDistance(float distance)
        {
            return default;
        }

        void UpdateMaterials()
        {
        }

        static Material CreateMaterialFromShader(string shaderName)
        {
            return default;
        }

        void CreateShaders()
        {
        }

        void CreateTextures(RenderTexture sourceTexture)
        {
        }

        void FreeShaders()
        {
        }

        void FreeTextures()
        {
        }
    }
}