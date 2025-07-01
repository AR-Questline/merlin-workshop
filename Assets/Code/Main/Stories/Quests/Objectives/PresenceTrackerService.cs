using System.Collections.Generic;
using Awaken.TG.Main.Scenes;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Utility.RichLabels;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Quests.Objectives {
    public class PresenceTrackerService : MonoBehaviour, IDomainBoundService {
        public Domain Domain => Domain.Gameplay;
        public bool RemoveOnDomainChange() {
            return true;
        }
        
        [SerializeField]
        List<PresenceTrackerData> presenceTrackerData;
        public delegate void PresenceUpdateDelegate(in PresenceUpdate presenceUpdated); 
        public event PresenceUpdateDelegate PresenceUpdated;
        
        public void Init() {
            var existingTrackers = World.All<PresenceTracker>();
            for (int i = 0; i < presenceTrackerData.Count; i++) {
                PresenceTrackerData trackerData = presenceTrackerData[i];
                PresenceTracker presenceTracker = existingTrackers.FirstOrDefault(t => t.Owner == trackerData.actor);
                if (presenceTracker == null) {
                    World.Add(new PresenceTracker(trackerData, this));
                } else {
                    presenceTracker.Initialize(trackerData, this);
                }
            }
        }
        
        public void UpdatePresence(in PresenceUpdate presenceUpdated) {
            if (!presenceUpdated.enable) {
                return;
            }
            this.PresenceUpdated?.Invoke(presenceUpdated);
        }

        public static PresenceTracker TrackerFor(ActorRef actor) => World.All<PresenceTracker>().FirstOrDefault(t => t.Owner == actor);
        
        public struct PresenceUpdate {
            public string[] presence;
            
            public ActorRef actor;
            public string groupName;
            
            public bool enable;
            
            public PresenceUpdate(RichLabelUsage richLabel, bool enable) {
                int enabledCount = 0;
                for (int i = 0; i < richLabel.RichLabelUsageEntries.Length; i++) {
                    if (richLabel.RichLabelUsageEntries[i].Include) {
                        enabledCount++;
                    }
                }
                presence = new string[enabledCount];
                for (int i = 0; i < richLabel.RichLabelUsageEntries.Length; i++) {
                    if (richLabel.RichLabelUsageEntries[i].Include) {
                        presence[i] = richLabel.RichLabelUsageEntries[i].RichLabelGuid;
                    }
                }
                this.enable = enable;
                actor = default;
                groupName = null;
            }
            
            public PresenceUpdate(ActorRef actor, string groupName, bool enable) {
                this.actor = actor;
                this.groupName = groupName;
                this.enable = enable;
                presence = null;
            }
        }
    }
}
