using Awaken.TG.Main.Heroes;
using Awaken.Utility.Extensions;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Graphics.ScriptedEvents.Triggers {
    [RequireComponent(typeof(IHeroTrigger))]
    public class HeroTriggerVS : MonoBehaviour {
        [SerializeField] string heroEnter = "HeroEntered";
        [SerializeField] string heroExit = "HeroExited";
        
        void Awake() {
            var trigger = GetComponent<IHeroTrigger>();
            if (heroEnter.IsNullOrWhitespace()) {
                trigger.OnHeroEnter += () => EventBus.Trigger(heroEnter, gameObject, Hero.Current);
            }
            if (heroExit.IsNullOrWhitespace()) {
                trigger.OnHeroExit += () => EventBus.Trigger(heroExit, gameObject, Hero.Current);
            }
        }
    }
}