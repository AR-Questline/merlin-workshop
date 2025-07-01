//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	public enum DistanceShape
	{
		Square,
		Diamond,
		Circle,
	}

	public enum DistanceMapResult
	{
		Outside,
		Inside,
		InOutMax,
		SDF,
	}

	/// <summary>
	/// Convert alpha channel to a distance map
	/// </summary>
	public class DistanceMap
	{
		const string ShaderId = "Hidden/ChocDino/UIFX/DistanceMap";

		static class ShaderProp
		{
			public static int StepSize = Shader.PropertyToID("_StepSize");
			public static int DownSample = Shader.PropertyToID("_DownSample");
			public static int InsideTex = Shader.PropertyToID("_InsideTex");
		}
		static class ShaderPass
		{
			public const int AlphaToUV = 0;
			public const int InvAlphaToUV = 1;
			public const int JumpFlood = 2;
			public const int JumpFloodSingleAxis = 3;
			public const int ResolveDistance = 4;
			public const int ResolveDistanceInOutMax = 5;
			public const int ResolveDistanceSDF = 6;
		}
		static class ShaderKeyword
		{
			public const string DistSquare = "DIST_SQUARE";
			public const string DistDiamond = "DIST_DIAMOND";
			public const string DistCircle = "DIST_CIRCLE";
		}

		private DistanceMapResult _resultType = DistanceMapResult.Outside;
		private DistanceShape _distanceShape = DistanceShape.Circle;
		private int _maxDistance = 8192;

		public DistanceMapResult Result { get { return _resultType; } set { ChangeProperty(ref _resultType, value); } }
		public DistanceShape DistanceShape { get { return _distanceShape; } set { ChangeProperty(ref _distanceShape, value); } }
		public int MaxDistance { get { return _maxDistance; } set { ChangeProperty(ref _maxDistance, value); } }

		private int _downSample = 1;
		private Material _material;
		private RenderTexture _rtDistance;
		private int _sourceTextureId;
		private Vector2Int _sourceTextureSize;
		private RenderTextureFormat _formatRed;
		private RenderTextureFormat _formatRedGreen;

		private bool _materialsDirty = true;

		public DistanceMap()
        {
        }

        public void ForceDirty()
        {
        }

        public bool IsMaterialDirty()
        {
            return default;
        }

        private void ChangeProperty<T>(ref T backing, T value) where T : struct
        {
        }

        public RenderTexture Process(RenderTexture sourceTexture)
        {
            return default;
        }

        private void ProcessPrime(bool isOutside, RenderTexture sourceTexture, RenderTexture targetTexture)
        {
        }

        private void ProcessResolveDistance(RenderTexture jfa)
        {
        }

        private void ProcessResolveDistanceSDF(RenderTexture jfaOut, RenderTexture jfaIn)
        {
        }

        private RenderTexture ProcessJumpFlood(RenderTexture srcTexture, RenderTexture flipTexture, RenderTexture flopTexture)
        {
            return default;
        }

        /*public RenderTexture GetOutputTexture()
		{
			return _outputTexture;
		}*/

        public void FreeResources()
        {
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

        RenderTexture GetTempJumpFloodTexture()
        {
            return default;
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