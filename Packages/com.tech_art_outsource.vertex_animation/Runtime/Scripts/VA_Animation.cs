using UnityEngine;

namespace TAO.VertexAnimation
{
    public class VA_Animation : ScriptableObject
    {
		public VA_AnimationData Data;
#if UNITY_EDITOR
		public int EDITOR_animationIndex;
#endif
		public void SetData(VA_AnimationData a_data
#if UNITY_EDITOR
			,int animationIndex
#endif
			)
		{
			Data = a_data;
#if UNITY_EDITOR
			this.EDITOR_animationIndex = animationIndex;
#endif
		}
	}
}