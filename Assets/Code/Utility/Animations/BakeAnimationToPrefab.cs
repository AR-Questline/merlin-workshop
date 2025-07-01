using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.Utility.Animations {
    public class BakeAnimationToPrefab : MonoBehaviour {
        public AnimationClip clip;
        
        [Button]
        public void BakeAnimation() {
            clip.SampleAnimation(gameObject, 0.1f);
        }
    }
}
