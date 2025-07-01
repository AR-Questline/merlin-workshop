using System;
using System.Collections.Generic;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Times;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Initial skills for the location that are present only when time condition is met.")]
    public class TimeOfDayDependentSkillsAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] TimeOfDayDependentSkill[] timeBasedSkills = Array.Empty<TimeOfDayDependentSkill>();
        
        public TimeOfDayDependentSkill[] TimeBasedSkills => timeBasedSkills;
        
        // === Implementation
        public Element SpawnElement() {
            return new TimeOfDayDependentSkills();
        }

        public bool IsMine(Element element) {
            return element is TimeOfDayDependentSkills;
        }

        [Serializable]
        public struct TimeOfDayDependentSkill {
            [SerializeField] ARTimeOfDay from;
            [SerializeField] ARTimeOfDay to;
            [SerializeField] List<SkillReference> skills;

            public TimeSpan FromTime() => from.GetTime();
            public TimeSpan ToTime() => to.GetTime();
            public List<SkillReference> Skills => skills;
            public bool IsInTimeCycle(ARDateTime currentTime) {
                var currentDay = new ARDateTime(currentTime.Date.Date);
                var fromTime = currentDay + from.GetTime();
                var toTime = currentDay + to.GetTime();
                if (toTime < fromTime) {
                    toTime += TimeSpan.FromDays(1);
                }
                return currentTime >= fromTime && currentTime <= toTime;
            }
        }
    }
}
