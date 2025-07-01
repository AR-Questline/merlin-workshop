using System;
using UnityEngine;

namespace Awaken.TG.Graphics.ScriptedEvents.Triggers {
    public class HeroTriggerMaster : MonoBehaviour, IHeroTrigger {
        int _enteredChildren;
        
        public event Action OnHeroEnter;
        public event Action OnHeroExit;
        
        void Awake() {
            var triggers = GetComponentsInChildren<HeroTrigger>();
            foreach(var trigger in triggers) {
                trigger.OnHeroEnter += OnHeroEnterChild;
                trigger.OnHeroExit += OnHeroExitChild;
            }
        }

        void OnHeroEnterChild() {
            _enteredChildren++;
            if (_enteredChildren == 1) {
                OnHeroEnter?.Invoke();
            }
        }

        void OnHeroExitChild() {
            _enteredChildren--;
            if (_enteredChildren == 0) {
                OnHeroExit?.Invoke();
            }
        }
    }
}