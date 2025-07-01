//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	public class GaussianBlurReference : ITextureBlur
	{
		internal class BlurShader
		{
			internal const string Id = "Hidden/ChocDino/UIFX/GaussianBlur-Reference";

			internal static class Prop
			{
				internal static readonly int KernelRadius = Shader.PropertyToID("_KernelRadius");
				internal static readonly int Weights = Shader.PropertyToID("_Weights");

			}
			internal static class Pass
			{
				internal const int Horizontal = 0;
				internal const int Vertical = 1;
			}
		}

		public BlurAxes2D BlurAxes2D { get { return _blurAxes2D; } set { _blurAxes2D = value; } }
		public Downsample Downsample { get { return _downSample; } set { if (_downSample != value) { _downSample = value; _kernelDirty = _materialDirty = true; } } }

		private Downsample _downSample = Downsample.Auto;
		private float _blurSize = 0.05f;
		private Material _material;
		private int _sourceTextureId;
		private RenderTexture _rtBlurH;
		private RenderTexture _rtBlurV;
		private BlurAxes2D _blurAxes2D = BlurAxes2D.Default;
		private const int MaxRadius = 512;
		private float[] _weights;

		private bool _kernelDirty = true;
		private bool _materialDirty = true;

		public void ForceDirty()
        {
        }

        public void SetBlurSize(float diagonalPercent)
        {
        }

        public void AdjustBoundsSize(ref Vector2Int leftDown, ref Vector2Int rightUp)
        {
        }

        public RenderTexture Process(RenderTexture sourceTexture)
        {
            return default;
        }

        public void FreeResources()
        {
        }

        private static uint CreateTextureHash(int width, int height)
        {
            return default;
        }

        private void RecreateTexture(ref RenderTexture rt, uint desiredHash, RenderTexture sourceTexture)
        {
        }

        void SetupResources(RenderTexture sourceTexture)
        {
        }

        private float GetScaledRadius()
        {
            return default;
        }

        void UpdateMaterial()
        {
        }

        static Material CreateMaterialFromShader(string shaderName)
        {
            return default;
        }

        void CreateShaders()
        {
        }

        void FreeShaders()
        {
        }

        void FreeTextures()
        {
        }

        private int GetDownsampleFactor()
        {
            return default;
        }

        // NOTE full kernel size is double this, plus one for the center coordinate
        static int GetHalfKernelSize(float sigma)
        {
            return default;
        }

        static float GetSigmaFromKernelRadius(float radius)
        {
            return default;
        }

        static float GetWeight(int x, float sigma)
        {
            return default;
        }

        void UpdateKernel()
        {
        }
    }
}