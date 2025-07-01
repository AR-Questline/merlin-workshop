using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace TAO.VertexAnimation
{
	[MaterialProperty("_AnimationData")]
	public struct VA_AnimationMaterialPropertyData : IComponentData
	{
		//AnimationTime, AnimationIndex, AnimationTimeNext, AnimationIndexNext
		public float4 Value;

		public static VA_AnimationMaterialPropertyData Construct(float animationTime, int animationIndex,
			float nextAnimationTime, int nextAnimationIndex) {
			return new VA_AnimationMaterialPropertyData() {
				Value = new float4() {
					x = animationTime,
					y = animationIndex,
					z = nextAnimationTime,
					w = nextAnimationIndex
				}
			};
		}
	}
}