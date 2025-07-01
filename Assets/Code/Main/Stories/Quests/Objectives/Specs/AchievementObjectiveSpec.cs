using Awaken.TG.Main.Heroes.Stats.StatConfig;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.Main.Utility.Tags;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Specs {
    public class AchievementObjectiveSpec : ObjectiveSpecBase {
        AchievementTemplate AchievementTemplate => GetComponent<AchievementTemplate>();
        
        public override LocString Description => AchievementTemplate.description;
        public override OptionalLocString TrackerDescription => new(AchievementTemplate.description, true);
        public override StatDefinedRange ExperienceGainRange => StatDefinedRange.Custom;
        public override float ExperiencePoints => 0;
        public override bool IsMarkerRelatedToStory => false;
        public override bool CanBeCompletedMultipleTimes => false;
        public override FlagLogic RelatedStoryFlag => new();
        public override LocationReference TargetLocationReference => new();
    }
}