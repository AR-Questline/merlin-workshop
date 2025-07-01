using Awaken.TG.Graphics.ScriptedEvents.Triggers;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Tutorials {
    [RequireComponent(typeof(Collider))]
    public class TutorialTrigger : MonoBehaviour {
        [SerializeField] TutKeys tutorial;

        void Awake() {
            gameObject.AddComponent<HeroTrigger>().OnHeroEnter += OnHeroTriggerEnter;
        }

        protected void OnEnable() {
            gameObject.layer = RenderLayers.TriggerVolumes;
        }

        void OnHeroTriggerEnter() {
            if (!TutorialKeys.IsConsumed(tutorial)) {
                TutorialMaster.Trigger(tutorial);
            }
        }

#if UNITY_EDITOR
        void Reset() {
            gameObject.layer = RenderLayers.TriggerVolumes;
        }
#endif
    }
}