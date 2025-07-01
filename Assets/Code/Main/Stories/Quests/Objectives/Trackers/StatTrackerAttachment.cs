using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Trackers {
    [AttachesTo(typeof(ObjectiveSpecBase), AttachmentCategory.Trackers, "Used to track stats.")]
    public class StatTrackerAttachment : BaseSimpleTrackerAttachment {
        [SerializeField, RichEnumExtends(typeof(StatType))]
        RichEnumReference statRef;
        [SerializeField, InfoBox("Current - complete objective if stat reaches given amount." +
                                 "\nGain - complete objective if stat gains given amount." +
                                 "\nLoss - complete objective if stat loses given amount.")]
        StatTrackType trackType;

        public StatType StatRef => statRef.EnumAs<StatType>();
        public StatTrackType TrackType => trackType;
        
        public override Element SpawnElement() => new StatTracker();

        public override bool IsMine(Element element) => element is StatTracker st && st.TrackedStat == StatRef;
        
    }
    
    public enum StatTrackType {
        Current = 0,
        Gain = 1,
        Loss = 2,
    }
}