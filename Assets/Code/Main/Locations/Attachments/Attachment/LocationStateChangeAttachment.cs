using System;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Times;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Changes location state to next on interaction or on specific time.")]
    public class LocationStateChangeAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] int newState;
        [Title("Manual")]
        [SerializeField] bool onInteract = true;
        [Title("Automatic")]
        [SerializeField] bool afterTime;
        [SerializeField, ShowIf(nameof(afterTime))] ARTimeSpan minimumTime;
        [SerializeField] bool atSpecificTime;
        [SerializeField] SpecificTimeType specificTimeType = SpecificTimeType.Custom;
        [SerializeField, ShowIf(nameof(ShowSpecificTime))] ARTimeSpan specificTime;
        [SerializeField, ShowIf(nameof(BothTimesAreSet))] bool useLongerTime = true;

        public int NewState => newState;
        public bool OnInteract => onInteract;
        public bool AfterTime => afterTime;
        public ARTimeSpan MinimumTime => minimumTime;
        public bool AtSpecificTime => atSpecificTime;
        public ARTimeSpan SpecificTime {
            get {
                switch (specificTimeType) {
                    case SpecificTimeType.NightStart:
                        return ARDateTime.NightStartTime;
                    case SpecificTimeType.NightEnd:
                        return ARDateTime.NightEndTime;
                    case SpecificTimeType.Custom:
                        return specificTime;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public bool UseLongerTime => useLongerTime;
        
        bool BothTimesAreSet => afterTime && atSpecificTime;
        bool ShowSpecificTime => atSpecificTime && specificTimeType == SpecificTimeType.Custom;
        
        public Element SpawnElement() {
            return new LocationStateChangeElement();
        }

        public bool IsMine(Element element) {
            return element is LocationStateChangeElement;
        }
        
        enum SpecificTimeType : byte {
            NightStart,
            NightEnd,
            Custom
        }
    }
}