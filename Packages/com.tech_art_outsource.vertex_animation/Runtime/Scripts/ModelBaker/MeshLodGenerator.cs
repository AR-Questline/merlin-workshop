using UnityEngine;

namespace TAO.VertexAnimation
{
	public static class MeshLodGenerator
	{
		public static Mesh[] GenerateLOD(this Mesh mesh, int lods, float[] quality)
		{
			Mesh[] lodMeshes = new Mesh[lods];

			for (int lm = 0; lm < lodMeshes.Length; lm++)
			{
				var meshCopy =  mesh.Copy();

				// Only simplify when needed.
				if (quality[lm] < 1.0f)
				{
					meshCopy = meshCopy.Simplify(quality[lm]);
					
				}
				meshCopy.name = string.Format("{0}_LOD{1}", meshCopy.name, lm);

				lodMeshes[lm] = meshCopy;
			}

			return lodMeshes;
		}

		public static Mesh[] GenerateLOD(this Mesh mesh, int lods, AnimationCurve qualityCurve)
		{
			float[] quality = new float[lods];

			for (int q = 0; q < quality.Length; q++)
			{
				quality[q] = qualityCurve.Evaluate(1f / quality.Length * q);
			}

			return GenerateLOD(mesh, lods, quality);
		}
	}
}