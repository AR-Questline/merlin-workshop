//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	public enum LongShadowCompositeMode
	{
		Normal,
		Cutout,
		Shadow,
	}

	public enum LongShadowMethod
	{
		Normal,
		DistanceMap,
	}

	public class LongShadow
	{
		public float Angle { get { return _angle; } set { ChangeProperty(ref _angle, value); } }
		public float Distance { get { return _distance; } set { ChangeProperty(ref _distance, value); } }
		public float StepSize { get { return _stepSize; } set { ChangeProperty(ref _stepSize, value); } }
		public float Pivot { get { return _pivot; } set { ChangeProperty(ref _pivot, value); } }
		public Color Color1 { get { return _color1; } set { ChangeProperty(ref _color1, value); } }
		public Color Color2 { get { return _color2; } set { ChangeProperty(ref _color2, value); } }
		public LongShadowMethod Method { get { return _method; } set { ChangeProperty(ref _method, value); } }
		public RenderTexture DistanceTexture { get { return _distanceTexture; } set { ChangePropertyRef(ref _distanceTexture, value); } }

		private float _angle = 135f;
		private float _distance = 8f;
		private float _stepSize = 1f;
		private float _pivot = 0f;
		private Color _color1 = Color.black;
		private Color _color2 = Color.black;
		private LongShadowMethod _method = LongShadowMethod.Normal;
		private RenderTexture _distanceTexture;

		private Material _material;
		private RenderTexture _rt;
		private int _sourceTextureId;
		private Vector2Int _sourceTextureSize;

		private bool _materialsDirty = true;
		private FilterBase _parentFilter = null;
		
		const string ShaderId = "Hidden/ChocDino/UIFX/LongShadow";

		static class ShaderProp
		{
			public static readonly int SourceAlpha = Shader.PropertyToID("_SourceAlpha");
			public static readonly int OffsetStart = Shader.PropertyToID("_OffsetStart");
			public static readonly int Length = Shader.PropertyToID("_Length");
			public static readonly int PixelStep = Shader.PropertyToID("_PixelStep");
			public static readonly int ColorFront = Shader.PropertyToID("_ColorFront");
			public static readonly int ColorBack = Shader.PropertyToID("_ColorBack");
			public static readonly int DistanceTex = Shader.PropertyToID("_DistanceTex");
		}
		private static class ShaderPass
		{
			internal const int Normal = 0;
			internal const int DistanceMap = 1;
		}

		private LongShadow() {
        }

        public LongShadow(FilterBase parentFilter)
        {
        }

        private void ChangeProperty<T>(ref T backing, T value) 	where T : struct
        {
        }

        protected void ChangePropertyRef<T>(ref T backing, T value) where T : class
        {
        }

        private static Vector2 AngleToOffset(float angle, Vector2 scale)
        {
            return default;
        }

        public bool GetAdjustedBounds(ref Vector2Int leftDown, ref Vector2Int rightUp)
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

        internal Vector2 GetTextureOffset()
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