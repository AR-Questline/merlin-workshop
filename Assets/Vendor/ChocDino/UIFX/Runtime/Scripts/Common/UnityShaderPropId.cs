//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	internal static class UnityShaderProp
	{
		public readonly static int TextureAddSample = Shader.PropertyToID("_TextureSampleAdd");
		public readonly static int ScreenParams = Shader.PropertyToID("_ScreenParams");
		public readonly static int UIVertexColorAlwaysGammaSpace = Shader.PropertyToID("_UIVertexColorAlwaysGammaSpace");
	
		public readonly static int Stencil = Shader.PropertyToID("_Stencil");
		public readonly static int StencilComp = Shader.PropertyToID("_StencilComp");
		public readonly static int StencilOp = Shader.PropertyToID("_StencilOp");
		public readonly static int StencilWriteMask = Shader.PropertyToID("_StencilWriteMask");
		public readonly static int StencilReadMask = Shader.PropertyToID("_StencilReadMask");

		public readonly static int BlendSrc = Shader.PropertyToID("_BlendSrc");
		public readonly static int BlendDst = Shader.PropertyToID("_BlendDst");

		public static void ResetStencilProperties(Material material)
        {
        }

        public static void CopyStencilProperties(Material src, Material dst)
        {
        }
    }
}