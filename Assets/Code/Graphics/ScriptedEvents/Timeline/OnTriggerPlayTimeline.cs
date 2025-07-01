using Awaken.TG.Graphics.ScriptedEvents.Triggers;
using UnityEngine;
using UnityEngine.Playables;

namespace Awaken.TG.Graphics.ScriptedEvents.Timeline {
    [RequireComponent(typeof(IHeroTrigger))]
    public class OnTriggerPlayTimeline : MonoBehaviour {
        [SerializeField] PlayableDirector playable;
        [SerializeField] bool playOnce;
        
        void Awake() {
            var heroTrigger = GetComponent<IHeroTrigger>();
            heroTrigger.OnHeroEnter += Play;
        }

        void Play() {
            playable.Play();
            if (playOnce) {
                gameObject.SetActive(false);
            }
        }
    }
}