//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	/// <summary>
	/// </summary>
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Filters/UIFX - Pixelate Filter")]
	public class PixelateFilter : FilterBase
	{
		private const string BlendShaderPath = "Hidden/ChocDino/UIFX/Blend-Pixelate";

		[SerializeField, Range(1f, 256f)] float _size = 8f;

		static new class ShaderProp
		{
			public readonly static int Size = Shader.PropertyToID("_Size");
		}

		protected override string GetDisplayShaderPath()
        {
            return default;
        }

        protected override bool DoParametersModifySource()
        {
            return default;
        }

        protected override void SetupDisplayMaterial(Texture source, Texture result)
        {
        }
    }
}