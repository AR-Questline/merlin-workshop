using System;
using Awaken.TG.Main.AudioSystem.Notifications;
using Awaken.TG.Main.Stories.Quests;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Quest {
    [Serializable]
    public struct QuestAudio {
        [field: SerializeField] public int SoundPriority { get; private set; }
        [field: SerializeField] public EventReference QuestTakenSound { get; private set; }
        [field: SerializeField] public EventReference QuestCompletedSound { get; private set; }
        [field: SerializeField] public EventReference QuestFailedSound { get; private set; }

        public NotificationSoundEvent GetSound(QuestState objectiveState) {
            return objectiveState switch {
                QuestState.Completed => new NotificationSoundEvent(SoundPriority, QuestCompletedSound),
                QuestState.Failed => new NotificationSoundEvent(SoundPriority, QuestFailedSound),
                _ => new NotificationSoundEvent(SoundPriority, QuestTakenSound)
            };
        }
    }
}