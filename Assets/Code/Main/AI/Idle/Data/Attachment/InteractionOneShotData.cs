using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Idle.Data.Runtime;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Utility.Attributes.Tags;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Data.Attachment {
    [Serializable]
    public struct InteractionOneShotData {
        [SerializeField] int startHour;
        [SerializeField] int startMinutes;
        [Tags(TagsCategory.InteractionID)] public string uniqueID;
        [SerializeField] public bool canBePaused;

        public float Hour => startHour + startMinutes / 60f;
        
        public void AppendOneShots(IdleDataElement dataElement, List<InteractionOneShotData> intervals) {
            intervals.Add(this);
        }
        
        public DeterministicInteractionFinder CreateFinder() {
            return new InteractionUniqueFinder(uniqueID);
        }
        
        public DateTime ThisDayStartTime(DateTime currentTime, bool withDeviation = false) {
            var date = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, startHour, startMinutes, 0);
            return date;
        }
    }
}