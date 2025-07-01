using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Trackers {
    public abstract class BaseSimpleTrackerAttachment : BaseTrackerAttachment {
        [FoldoutGroup("Setup")]
        public float maxProgress = 1f;
        [FoldoutGroup("Setup")]
        public float initialProgress;
        
        protected override string DisplayPatternDescription => base.DisplayPatternDescription +
                                                               "\n{cur} - current numeric progress (f.e. 2 rats killed = 2)" +
                                                               "\n{max} - number of progress that is required (f.e. kill 5 rats = 5)" +
                                                               "\n{percent} - current divided by max";
    }
}