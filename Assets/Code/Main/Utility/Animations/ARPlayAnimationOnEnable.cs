using Animancer;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.ARAnimator {
    [RequireComponent(typeof(AnimancerComponent))]
    public class ARPlayAnimationOnEnable : MonoBehaviour {
        [SerializeField] AnimancerComponent animancer;
        [SerializeField] ClipTransition clip;

        void OnEnable() {
            animancer.Play(clip);
        }
        
#if UNITY_EDITOR
        [Sirenix.OdinInspector.Button, Sirenix.OdinInspector.Title("Draw Style"), Sirenix.OdinInspector.HorizontalGroup("Drawing Style")]
        void DrawSimplified() {
            UnityEditor.EditorPrefs.SetBool("UseSimplifiedTransitionDrawer", true);
        }

        [Sirenix.OdinInspector.Button, Sirenix.OdinInspector.Title(""), Sirenix.OdinInspector.HorizontalGroup("Drawing Style")]
        void DrawFull() {
            UnityEditor.EditorPrefs.SetBool("UseSimplifiedTransitionDrawer", false);
        }
#endif
    }
}