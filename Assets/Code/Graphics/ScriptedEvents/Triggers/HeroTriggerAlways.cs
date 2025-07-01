using System;
using UnityEngine;

namespace Awaken.TG.Graphics.ScriptedEvents.Triggers {
    public class HeroTriggerAlways : MonoBehaviour, IHeroTrigger {
        public event Action OnHeroEnter;
#pragma warning disable CS0067 // Never used
        public event Action OnHeroExit;
#pragma warning restore CS0067 // Never used

        void Start() {
            OnHeroEnter?.Invoke();
        }
    }
}