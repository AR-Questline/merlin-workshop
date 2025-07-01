//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	public class ErodeDilate
	{
		const string ShaderId = "Hidden/ChocDino/UIFX/ErodeDilate";

		private static class ShaderProp
		{
			internal static int ErodeRadius = Shader.PropertyToID("_ErodeRadius");
			internal static int DilateRadius = Shader.PropertyToID("_DilateRadius");
		}
		private static class ShaderKeyword
		{
			public const string DistSquare = "DIST_SQUARE";
			public const string DistDiamond = "DIST_DIAMOND";
			public const string DistCircle = "DIST_CIRCLE";
		}
		private static class ShaderPass
		{
			internal const int ErodeAlpha = 0;
			internal const int DilateAlpha = 1;
			internal const int ErodeDilateAlpha = 2;
			internal const int Erode = 3;
			internal const int Dilate = 4;
			internal const int CopyAlpha = 5;
			internal const int Null = 6;
		}

		public float ErodeSize { get { return _erodeSize; } set { ChangeProperty(ref _erodeSize, value); } }
		public float DilateSize { get { return _dilateSize; } set { ChangeProperty(ref _dilateSize, value); } }
		public DistanceShape DistanceShape { get { return _distanceShape; } set { ChangeProperty(ref _distanceShape, value); } }
		public bool AlphaOnly { get { return _alphaOnly; } set { ChangeProperty(ref _alphaOnly, value); } }
		public bool UseMultiPassOptimisation { get { return _useMultiPassOptimisation; } set { ChangeProperty(ref _useMultiPassOptimisation, value); } }

		//internal RenderTexture OutputTexture { get { return _output; } }

		private float _erodeSize = 0f;
		private float _dilateSize = 0f;
		private DistanceShape _distanceShape = DistanceShape.Circle;
		private bool _alphaOnly = false;
		private bool _useMultiPassOptimisation = false;

		private Material _material;
		private RenderTexture _rtResult;
		private RenderTexture _rtResult2;
		private int _sourceTextureId;

		private bool _materialsDirty = true;

		public void ForceDirty()
        {
        }

        private void ChangeProperty<T>(ref T backing, T value) where T : struct
        {
        }

        public void AdjustBoundsSize(ref Vector2Int leftDown, ref Vector2Int rightUp)
        {
        }

        public RenderTexture Process(RenderTexture sourceTexture)
        {
            return default;
        }

        private RenderTexture DoPass(float dilateSize, float erodeSize, RenderTexture src, RenderTexture dst)
        {
            return default;
        }

        public void FreeResources()
        {
        }

        private uint _currentTextureHash;

        private static uint CreateTextureHash(int width, int height, bool alphaOnly, bool useMultiPassOptimisation)
        {
            return default;
        }

        void SetupResources(RenderTexture sourceTexture)
        {
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

        public void FreeTextures()
        {
        }
    }
}