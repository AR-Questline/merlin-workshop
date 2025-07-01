using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Stories;
using Awaken.Utility;

namespace Awaken.TG.Main.Locations.Clearing {
    public partial class StartStoryOnCondition : ActionOnConditionBase, IRefreshedByAttachment<StartStoryOnConditionAttachment> {
        public override ushort TypeForSerialization => SavedModels.StartStoryOnCondition;

        StartStoryOnConditionAttachment _spec;
        
        protected override int AllConditionsToFulfil => _spec.Conditions.Length;
        protected override Condition[] Conditions => _spec.Conditions;
        
        public void InitFromAttachment(StartStoryOnConditionAttachment spec, bool isRestored) {
            _spec = spec;
        }
        
        protected override void OnAllConditionsFulfilled() {
            Story.StartStory(StoryConfig.Base(_spec.Story, typeof(VDialogue)));
        } 
    }
}