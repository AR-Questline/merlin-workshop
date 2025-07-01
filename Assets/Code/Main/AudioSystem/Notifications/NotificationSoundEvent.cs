using System;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.AudioSystem.Notifications {
    [Serializable]
    public struct NotificationSoundEvent {
        [SerializeField] public int priority;
        [SerializeField] public EventReference eventReference;

        public NotificationSoundEvent(int priority, EventReference eventReference) {
            this.priority = priority;
            this.eventReference = eventReference;
        }
    }
}