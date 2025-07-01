using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Effectors {
    public partial class StoryGraphEffector : Element<Objective>, IObjectiveEffector, IRefreshedByAttachment<StoryGraphEffectorAttachment> {
        public override ushort TypeForSerialization => SavedModels.StoryGraphEffector;

        [Saved] public ObjectiveState RunOnState { get; private set; }
        StoryBookmark Story { get; set; }

        public void InitFromAttachment(StoryGraphEffectorAttachment spec, bool isRestored) {
            Story = spec.StoryBookmark;
            RunOnState = spec.RunOnState;
        }
        
        public void OnStateUpdate(QuestUtils.ObjectiveStateChange stateChange) {
            if (stateChange.newState == RunOnState) {
                if (Story == null) {
                    Log.Important?.Error($"Story is null for {LogUtils.GetDebugName(this)}");
                    Discard();
                    return;
                }
                // anti loop protection
                if (World.All<Story>().Any(s => s.Guid == Story?.GUID)) {
                    Log.Important?.Error($"Prevented story from launching to prevent infinite loop. Story {Story?.GUID} from quest {LogUtils.GetDebugName(this)}");
                    Discard();
                    return;
                }
                Stories.Story.StartStory(StoryConfig.Base(Story, null));
                Discard();
            }
        }
    }
}