//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	public class ColorAdjust
	{
		const string ShaderId = "Hidden/ChocDino/UIFX/ColorAdjust";

		static class ShaderProp
		{
			public static readonly int ColorMatrix = Shader.PropertyToID("_ColorMatrix");
			public static readonly int BCPO = Shader.PropertyToID("_BCPO");
			public static readonly int BrightnessRGBA = Shader.PropertyToID("_BrightnessRGBA");
			public static readonly int ContrastRGBA = Shader.PropertyToID("_ContrastRGBA");
			public static readonly int PosterizeRGBA = Shader.PropertyToID("_PosterizeRGBA");
		}
		private static class ShaderKeyword
		{
			public const string Posterize = "POSTERIZE";
		}

		private float _hue = 0.0f;
		private float _saturation = 0.0f;
		private float _value = 0.0f;
		private float _brightness = 0.0f;
		private float _contrast = 0.0f;
		private float _posterize = 255.0f;
		private float _opacity = 1f;
		private Vector4 _brightnessRGBA = Vector4.zero;
		private Vector4 _contrastRGBA = Vector4.zero;
		private Vector4 _posterizeRGBA = new Vector4(255f, 255f, 255, 255f);

		public float Hue { get { return _hue; } set { value = Mathf.Clamp(value, 0f, 360f); if (_hue != value) { _hue = value; _matrixDirty = true; } } }
		public float Saturation { get { return _saturation; } set { value = Mathf.Clamp(value, -2f, 2f); if (_saturation != value) { _saturation = value; _matrixDirty = true; } } }
		public float Value { get { return _value; } set { value = Mathf.Clamp(value, -1f, 1f); if (_value != value) { _value = value; _matrixDirty = true; } } }
		public float Brightness { get { return _brightness; } set { value = Mathf.Clamp(value, -2f, 2f); if (_brightness != value) { _brightness = value; _materialsDirty = true; } } }
		public float Contrast { get { return _contrast; } set { value = Mathf.Clamp(value, -2f, 2f); if (_contrast != value) { _contrast = value; _materialsDirty = true; } } }
		public float Posterize { get { return _posterize; } set { value = Mathf.Clamp(value, 0.01f, 255f); if (_posterize != value) { _posterize = value; _materialsDirty = true; } } }
		public float Opacity { get { return _opacity; } set { value = Mathf.Clamp01(value); if (_opacity != value) { _opacity = value; _materialsDirty = true; } } }
		public Vector4 BrightnessRGBA { get { return _brightnessRGBA; } set { if (_brightnessRGBA != value) { _brightnessRGBA = value; _materialsDirty = true; } } }
		public Vector4 ContrastRGBA { get { return _contrastRGBA; } set { if (_contrastRGBA != value) { _contrastRGBA = value; _materialsDirty = true; } } }
		public Vector4 PosterizeRGBA { get { return _posterizeRGBA; } set { if (_posterizeRGBA != value) { _posterizeRGBA = value; _materialsDirty = true; } } }

		private float _strength = 1f;
		public float Strength { get { return _strength; } set { value = Mathf.Clamp01(value); if (_strength != value) { _strength = value; _matrixDirty = true; } } }

		private Material _material;
		private RenderTexture _resultTexture;
		private int _sourceTextureId;
		private Vector2Int _sourceTextureSize;
		private Matrix4x4 _colorMatrix;

		private bool _matrixDirty = true;
		#pragma warning disable 0414		// suppress warnings for "The field XYZ is assigned but its value is never used"
		private bool _materialsDirty = true;
		#pragma warning restore 0414

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

        private static void BuildHSVMatrix(float hue, float saturation, float value, ref Matrix4x4 result)
        {
        }
    }
}