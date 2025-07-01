using System;
using Awaken.TG.Main.AudioSystem.Notifications;
using Awaken.TG.Main.Stories.Quests.Objectives;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Objective {
    [Serializable]
    public struct ObjectiveAudio {
        [field: SerializeField] public int SoundPriority { get; private set; }
        [field: SerializeField] public EventReference ObjectiveChangedSound { get; private set; }
        [field: SerializeField] public EventReference ObjectiveCompletedSound { get; private set; }
        [field: SerializeField] public EventReference ObjectiveFailedSound { get; private set; }

        public NotificationSoundEvent GetSound(ObjectiveState objectiveState) {
            return objectiveState switch {
                ObjectiveState.Completed => new NotificationSoundEvent(SoundPriority, ObjectiveCompletedSound),
                ObjectiveState.Failed => new NotificationSoundEvent(SoundPriority, ObjectiveFailedSound),
                _ => new NotificationSoundEvent(SoundPriority, ObjectiveChangedSound)
            };
        }
    }
}