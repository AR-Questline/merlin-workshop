using Awaken.CommonInterfaces.Assets;
using Awaken.Utility.Assets;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.Utility.Animations {
    [ExecuteInEditMode]
    public class UpdateAnimatorInEditor : MonoBehaviour, IEditorOnlyMonoBehaviour {
#if UNITY_EDITOR
        [SerializeField] Animator animator;

        void OnEnable() {
            animator = GetComponent<Animator>();
        }
        
        [Button]
        public void UpdateAnimator(float deltaTime = 0.1f) {
            animator.Update(0.1f);
        }
#endif
    }
}