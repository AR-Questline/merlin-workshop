//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	internal class Compositor
	{
		private const string CompositeShaderPath = "Hidden/ChocDino/UIFX/Composite";

		private static class ShaderPass
		{
			internal const int AlphaBlendedToPremultipliedAlpha = 0;
		}

		private RenderTexture _rtRawSource;
		private RenderTexture _composite;
		private Matrix4x4 _projectionMatrix;
		private RenderTexture _prevRT;
		private Material _compositeMaterial;
		private Camera _prevCamera;
		private Matrix4x4 _viewMatrix;

		public void FreeResources()
        {
        }

        public bool Start(Camera camera, RectInt textureRect, float canvasScale = 1f)
        {
            return default;
        }

        public void End()
        {
        }

        public void AddMesh(Transform xform, Mesh mesh, Material material, bool materialOutputPremultipliedAlpha)
        {
        }

        private void RenderMeshDirectly(Transform xform, Mesh mesh, Material material)
        {
        }

        private void RenderMeshWithAdjustment(Transform xform, Mesh mesh, Material material, int pass)
        {
        }

        private void RenderMeshToActiveTarget(Transform xform, Mesh mesh, Material material)
        {
        }

        public RenderTexture GetTexture()
        {
            return default;
        }
    }
}