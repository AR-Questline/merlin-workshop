//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	/*internal class BlurMaterial : FilterMaterial
	{

		static Material CreateMaterialFromShader(string shaderName)
		{
			Material result = null;
			Shader shader = Shader.Find(shaderName);
			if (shader != null)
			{
				result = new Material(shader);
			}
			return result;
		}

		internal BlurMaterial()
		{
			_material = CreateMaterialFromShader(BlurShader.Id);
		}

		internal void SetKernelRadius(float value)
		{
			_material.SetFloat(BlurShader.Prop.KernelRadius, value);
		}

		//ObjectHelper.Destroy(ref _material);


		private Material _material;


	}*/

	public class BoxBlurReference : ITextureBlur
	{
		internal class BlurShader
		{
			internal const string Id = "Hidden/ChocDino/UIFX/BoxBlur-Reference";

			internal static class Prop
			{
				internal static readonly int KernelRadius = Shader.PropertyToID("_KernelRadius");
			}
			internal static class Pass
			{
				internal const int Horizontal = 0;
				internal const int Vertical = 1;
			}
		}

		public BlurAxes2D BlurAxes2D { get { return _blurAxes2D; } set { _blurAxes2D = value; } }
		public Downsample Downsample { get { return _downSample; } set { if (_downSample != value) { _downSample = value; _materialDirty = true; } } }
		public int IterationCount { get { return _iterationCount; } set { value = Mathf.Clamp(value, 1, 6); if (_iterationCount != value) { _iterationCount = value; } } }

		private int _iterationCount = 3;
		private Downsample _downSample = Downsample.Auto;
		private float _blurSize = 0.05f;
		private Material _material;
		private int _sourceTextureId;
		private RenderTexture _rtBlurH;
		private RenderTexture _rtBlurV;
		private BlurAxes2D _blurAxes2D = BlurAxes2D.Default;

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

        /*private RenderTexture BlitCopy(RenderTexture src)
		{
			var target = GetTexture();
			Graphics.Blit(src, target);
			target.IncrementUpdateCount();
			ReturnTexture(src);
			return target;
		}

		private RenderTexture BlitBlur(RenderTexture src, int pass)
		{
			var target = GetTexture();
			Graphics.Blit(src, target, _material, pass);
			target.IncrementUpdateCount();
			ReturnTexture(src);
			return target;
		}*/

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
    }
}