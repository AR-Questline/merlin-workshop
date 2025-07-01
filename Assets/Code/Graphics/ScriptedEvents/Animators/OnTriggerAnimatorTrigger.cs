using Awaken.TG.Graphics.ScriptedEvents.Triggers;
using UnityEngine;

namespace Awaken.TG.Graphics.ScriptedEvents.Animators {
    [RequireComponent(typeof(IHeroTrigger))]
    public class OnTriggerAnimatorTrigger : MonoBehaviour {
        [SerializeField] Animator animator;
        [SerializeField] string triggerName;
        
        void Awake() {
            var heroTrigger = GetComponent<IHeroTrigger>();
            heroTrigger.OnHeroEnter += Trigger;
        }

        void Trigger() {
            animator.SetTrigger(triggerName);
        }
    }
}