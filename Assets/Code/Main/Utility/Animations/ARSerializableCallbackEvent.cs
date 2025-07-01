using System;
using Animancer;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Heroes.Animations;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations {
    [Serializable]
    public class ARSerializableCallbackEvent : IARSerializableCallbackEvent {
        [SerializeField] ARAnimationEventData eventData;
        [SerializeField] ARFinisherEffectsData finisherEffectsData;
        [SerializeField] bool hasEventData;
        [SerializeField] bool hasFinisherEffectsData;

        public ARSerializableCallbackEvent() { }
        
        public ARSerializableCallbackEvent(ARAnimationEventData eventData) {
            this.eventData = eventData;
            hasEventData = true;
        }
        
        public ARSerializableCallbackEvent(ARFinisherEffectsData finisherEffectsData) {
            this.finisherEffectsData = finisherEffectsData;
            hasFinisherEffectsData = true;
        }
        
        public void Invoke() {
            if (hasEventData) {
                var data = eventData;
                var currentState = AnimancerEvent.CurrentState;
                if (currentState != null) {
                    data.restriction = currentState.Layer.Index switch {
                        (int)HeroLayerType.MainHand => WeaponRestriction.MainHand,
                        (int)HeroLayerType.OffHand => WeaponRestriction.OffHand,
                        _ => data.restriction
                    };
                }
                HeroWeaponEvents.Current.TriggerAnimancerEvent(data);
            } else if (hasFinisherEffectsData) {
                HeroWeaponEvents.Current.TriggerAnimancerEvent(finisherEffectsData);
            }
        }

        public int GetPersistentEventCount() => hasEventData || hasFinisherEffectsData ? 1 : 0;
    }
}