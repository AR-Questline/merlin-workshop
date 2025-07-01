using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Utility.RichLabels;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Quests.Objectives {
    [Serializable]
    public struct PresenceTrackerData {
        public ActorRef actor;
        public List<PresenceTargetScene> presenceTargetScenes;
    }

    [Serializable]
    public struct PresenceTargetScene {
        [HorizontalGroup] public RichLabelUsage richLabelActive;

        [HorizontalGroup, ShowIf(nameof(HasNoSceneChangeIntervals))]
        public SceneReference targetScene;
        
        [HideIf("@sceneChangeIntervals != null && sceneChangeIntervals.Length == 0")]
        public InteractionSceneChangeIntervalData[] sceneChangeIntervals;

        public AttachmentGroupReliantIntervals[] attachmentGroupReliantIntervals;

        public readonly bool HasNoSceneChangeIntervals => attachmentGroupReliantIntervals.IsNullOrEmpty();

        [Button, ShowIf("@sceneChangeIntervals != null && sceneChangeIntervals.Length > 0"), GUIColor("red")]
        void MigrateSceneChangeIntervalsToAttachmentGroupReliantIntervals() {
            if (sceneChangeIntervals.IsNullOrEmpty()) {
                return;
            }

            attachmentGroupReliantIntervals = new AttachmentGroupReliantIntervals[1];
            attachmentGroupReliantIntervals[0].groupName = "";
            attachmentGroupReliantIntervals[0].sceneChangeIntervals = sceneChangeIntervals;
            sceneChangeIntervals = null;
        }
        public readonly SceneReference GetTargetScene(in DateTime currentTime, int attachmentGroupIndex) {
            if (HasNoSceneChangeIntervals) {
                return targetScene;
            }
            InteractionSceneChangeIntervalData[] groupIntervals = attachmentGroupReliantIntervals[attachmentGroupIndex].sceneChangeIntervals;
            return groupIntervals[GetCurrentIntervalIndex(currentTime, groupIntervals)].Scene;
        }

        public readonly int AttachmentGroupReliantIndex(string groupName) {
            for (int i = 0; i < attachmentGroupReliantIntervals.Length; i++) {
                if (attachmentGroupReliantIntervals[i].groupName == groupName) {
                    return i;
                }
            }

            return 0;
        }

        public readonly bool RichLabelMatches(in PresenceTrackerService.PresenceUpdate presenceUpdate) {
            // No linq contains, as the order is not guaranteed
            for (int i = 0; i < richLabelActive.RichLabelUsageEntries.Length; i++) {
                bool found = false;
                for (int j = 0; j < presenceUpdate.presence.Length; j++) {
                    if (presenceUpdate.presence[j] == richLabelActive.RichLabelUsageEntries[i].RichLabelGuid) {
                        found = true;
                        break;
                    }
                }

                bool include = richLabelActive.RichLabelUsageEntries[i].Include;
                if ((include && !found)
                    || (!include && found)) {
                    return false;
                }
            }

            return true;
        }

        static int GetCurrentIntervalIndex(in DateTime currentTime, in InteractionSceneChangeIntervalData[] dataSource) {
            for (int i = 0; i < dataSource.Length; i++) {
                var start = dataSource[i].ThisDayStartTime(currentTime);
                if (start > currentTime) {
                    return i == 0 ? dataSource.Length - 1 : i - 1;
                }
            }

            return dataSource.Length - 1;
        }

        public readonly bool TryGetNextIntervalStartTime(in DateTime currentTime, int attachmentGroupIndex, out DateTime nextIntervalStartTime) {
            var dataSource = attachmentGroupReliantIntervals[attachmentGroupIndex].sceneChangeIntervals;
            if (dataSource.IsNullOrEmpty()) {
                nextIntervalStartTime = default;
                return false;
            }
            
            for (int i = 0; i < dataSource.Length; i++) {
                nextIntervalStartTime = dataSource[i].ThisDayStartTime(currentTime);
                if (nextIntervalStartTime > currentTime) {
                    return true;
                }
            }
            nextIntervalStartTime = dataSource[0].ThisDayStartTime(currentTime).AddDays(1);;
            return true;
        }
    }

    [Serializable]
    public struct InteractionSceneChangeIntervalData {
        const string TimeGroup = "Time";
        [UnityEngine.Scripting.Preserve] public string name;
        [SerializeField, BoxGroup(TimeGroup)] int startHour;
        [SerializeField, BoxGroup(TimeGroup)] int startMinutes;
        [SerializeField, BoxGroup(TimeGroup)] int startDeviation;
        [SerializeField] SceneReference scene;
        
        public SceneReference Scene => scene;
        public DateTime ThisDayStartTime(in DateTime currentTime) => new(currentTime.Year, currentTime.Month, currentTime.Day, startHour, startMinutes, 0);
    }
    
    [Serializable]
    public struct AttachmentGroupReliantIntervals {
        [SerializeField] public string groupName;
        [SerializeField] public InteractionSceneChangeIntervalData[] sceneChangeIntervals;
    }
}