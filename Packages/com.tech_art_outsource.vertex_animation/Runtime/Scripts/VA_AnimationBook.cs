using System.Collections.Generic;
using TAO.VertexAnimation.Editor;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace TAO.VertexAnimation
{
	[CreateAssetMenu(fileName = "NewAnimationBook", menuName = "VertexAnimation/AnimationBook", order = 400)]
	public class VA_AnimationBook : ScriptableObject
	{
		public Texture2DArray positionMap;
		public float frameTime;
		public List<VA_Animation> animations = new();
		public List<Material> materials = new();

		public VA_AnimationBook(Texture2DArray a_positionMap)
		{
			positionMap = a_positionMap;
		}

		public VA_AnimationBook(Texture2DArray a_positionMap, List<VA_Animation> a_animations)
		{
			positionMap = a_positionMap;

			foreach (var a in a_animations)
			{
				TryAddAnimation(a);
			}
		}

		public BlobAssetReference<VA_AnimationBookData> GetBlobAssetRef() {
			// Blob builder to build.
			using BlobBuilder blobBuilder = new(Allocator.Temp);
			// Construct the root.
			ref VA_AnimationBookData animationDataBlobAsset =
				ref blobBuilder.ConstructRoot<VA_AnimationBookData>();

			// Set all the data.
			BlobBuilderArray<VA_AnimationData> animationDataArray = blobBuilder.Allocate(
				ref animationDataBlobAsset.animationsDatas,
				animations.Count);

			for (int i = 0; i < animationDataArray.Length; i++) {
				// Copy data.
				animationDataArray[i] = animations[i].Data;
			}
			return blobBuilder.CreateBlobAssetReference<VA_AnimationBookData>(Allocator.Persistent);
		}
		
		public bool TryAddAnimation(VA_Animation animation)
		{
			if (animations != null && animations.Count != 0)
			{
				if (!animations.Contains(animation))
				{
					animations.Add(animation);
					OnValidate();
					return true;
				}
			}
			else
			{
				// Add first animation.
				animations.Add(animation);
				// Set maxFrames for this animation book.
				OnValidate();

				return true;
			}

			return false;
		}

		public bool TryAddMaterial(Material material)
		{
			if (material != null)
			{
				if (materials == null)
				{
					materials = new List<Material>();
				}

				if (!materials.Contains(material))
				{
					if (material.HasProperty("_PositionMap") && material.HasProperty("_AnimationsOffsets"))
					{
						materials.Add(material);
						return true;
					}
				}
			}

			return false;
		}

		private void OnValidate()
		{
			if (animations?.Count >= VA_ModelBaker.MaxAnimationClipsCount) {
				Debug.LogError($"Max animations count is {VA_ModelBaker.MaxAnimationClipsCount}");
				int toRemoveCount = VA_ModelBaker.MaxAnimationClipsCount - animations.Count + 1;
				animations.RemoveRange(animations.Count - toRemoveCount, toRemoveCount);
			}
			if (positionMap != null)
			{
				if (positionMap.depth < animations.Count)
				{
					Debug.LogWarning(string.Format("More animations ({0}) than positionMaps in {1}!", animations.Count, this.name));
				}
			}
		}
	}
}