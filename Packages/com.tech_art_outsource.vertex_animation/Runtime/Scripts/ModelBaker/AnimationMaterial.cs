using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace TAO.VertexAnimation
{
	public static class AnimationMaterial
	{
		public static Material Create(string name, Shader shader)
		{
			Material material = new Material(shader)
			{
				name = name,
				enableInstancing = true
			};

			return material;
		}

		public static Material Create(string name, Shader shader, Texture2DArray positionMap, bool useNormalA, bool useInterpolation, 
			float4x4 animationsOffsets, int textureWidth, int textureHeight, int fps, int verticesCount)
		{
			Material material = Create(name, shader);

			UpdateMaterial(material, name, shader, positionMap, useNormalA, useInterpolation, animationsOffsets,
				textureWidth, textureHeight, fps, verticesCount);

			return material;
		}

		public static void UpdateMaterial(Material material, string name, Shader shader, Texture2DArray positionMap, bool useNormalA,
			bool useInterpolation, float4x4 animationsOffsets, int textureWidth, int textureHeight, int fps, int verticesCount)
		{
			material.name = name;

			if (material.shader != shader)
			{
				material.shader = shader;
			}

			material.SetTexture("_PositionMap", positionMap);
			for (int i = 0; i < 4; i++) {
				material.SetVector($"_AnimationsOffsets{i}", animationsOffsets[i]);
			}
			material.SetInt("_TextureWidth", textureWidth);
			material.SetInt("_TextureHeight", textureHeight);
			material.SetInt("_Fps", fps);
			material.SetInt("_VerticesCount", verticesCount);
			if (useNormalA)
			{
				material.EnableKeyword("_USE_NORMALA_ON");
			}
			else
			{
				material.DisableKeyword("_USE_NORMALA_ON");
			}

			if (useInterpolation)
			{
				material.EnableKeyword("_USE_INTERPOLATION_ON");
			}
			else
			{
				material.DisableKeyword("_USE_INTERPOLATION_ON");
			}
		}
	}
}