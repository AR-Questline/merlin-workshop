using System;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Utility.Tags;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Graphics.ScriptedEvents.Triggers {
    [RequireComponent(typeof(Collider))]
    public class HeroTrigger : MonoBehaviour, IHeroTrigger {
        [SerializeReference] ITriggerCondition[] conditions = Array.Empty<ITriggerCondition>();
        
        public event Action OnHeroEnter;
        public event Action OnHeroExit;
        
        bool _heroWithin;
        bool _heroHasStayed;
        bool _shouldUpdate;

        void OnDisable() {
            if (Hero.Current == null) {
                return;
            }
            
            if (_heroWithin) {
                OnHeroExit?.Invoke();
            }
            _heroWithin = false;
            _heroHasStayed = false;
            _shouldUpdate = false;
        }

        void OnTriggerStay(Collider other) {
            _heroHasStayed |= other.gameObject.GetComponentInParent<VHeroController>() != null;
        }

        void FixedUpdate() {
            // OnTriggerXXX are called in physics loop, after that we want to refresh _heroWithin
            // but we don't want it to be done on FixedUpdate because we trigger events there and it should not affect physics
            // with high FPS physics loop may not occurred between Updates,
            // so here we only set this bool so on Update we know if we should refresh _heroWithin
            _shouldUpdate = true;
        }

        void Update() {
            if (_shouldUpdate) {
                bool shouldBeEntered = ShouldBeEntered();
                if (_heroWithin != shouldBeEntered) {
                    _heroWithin = shouldBeEntered;
                    var evt = _heroWithin ? OnHeroEnter : OnHeroExit;
                    evt?.Invoke();
                }
                _heroHasStayed = false;
                _shouldUpdate = false;
            }
        }
        
        bool ShouldBeEntered() {
            if (!_heroHasStayed) {
                return false;
            }
            foreach (var condition in conditions) {
                if (!condition.Available()) {
                    return false;
                }
            }
            return true;
        }

        void OnValidate() {
            GetComponent<Collider>().isTrigger = true;
            gameObject.layer = RenderLayers.TriggerVolumes;
        }

        interface ITriggerCondition {
            bool Available();
        }
        
        [Serializable]
        class TriggerFlagCondition : ITriggerCondition {
            [SerializeField] FlagLogic flag;
            public bool Available() => flag.Get(true);
        }
    }
}