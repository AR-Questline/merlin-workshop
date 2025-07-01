using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TAO.VertexAnimation.Editor
{
	public static class AnimationPrefab
	{
#if UNITY_EDITOR
		public static GameObject Create(string path, string name, Mesh[] meshes, Material material, Material noVAMaterial, float[] lodTransitions,
			VA_AnimationBook animationBook)
		{
			GameObject prefabRoot;
			if (AssetDatabaseUtils.HasAsset(path, typeof(GameObject)))
			{
				prefabRoot = PrefabUtility.LoadPrefabContents(path);
				if (!prefabRoot.TryGetComponent(out LODGroup _))
				{
					prefabRoot.AddComponent<LODGroup>();
				}
			}
			else
			{
				prefabRoot = new GameObject(name, typeof(LODGroup));
			}
			
			// Create all LODs.
			LOD[] lods = new LOD[meshes.Length];

			for (int i = 0; i < meshes.Length; i++)
			{
				string childName = string.Format("{0}_LOD{1}", name, i);

				GameObject child;
				{
					Transform t = prefabRoot.transform.Find(childName);
					if (t)
					{
						child = t.gameObject;
					}
					else
					{
						child = new GameObject(childName, typeof(MeshFilter), typeof(MeshRenderer));
					}
				}

				if (child.TryGetComponent(out MeshFilter mf))
				{
					mf.sharedMesh = meshes[i];
				}

				if (child.TryGetComponent(out MeshRenderer mr))
				{
					mr.sharedMaterial = material;
				}

				child.transform.SetParent(prefabRoot.transform);
				lods[i] = new LOD(lodTransitions[i], new Renderer[1] { mr });
			}

			var lodGroup = prefabRoot.GetComponent<LODGroup>();
			lodGroup.SetLODs(lods);
			lodGroup.RecalculateBounds();

			// Create prefab.
			GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(prefabRoot, path, InteractionMode.AutomatedAction);
			Object.DestroyImmediate(prefabRoot);

			return prefab;
		}

		public static GameObject Create(string path, string name, Mesh[] meshes, Material material, Material noVAMaterial, AnimationCurve lodTransitions,
			VA_AnimationBook animationBook)
		{
			float[] lt = new float[meshes.Length];

			for (int i = 0; i < lt.Length; i++)
			{
				lt[i] = lodTransitions.Evaluate((1.0f / lt.Length) * (i + 1));
			}

			return Create(path, name, meshes, material, noVAMaterial, lt, animationBook);
		}
#endif
	}
}