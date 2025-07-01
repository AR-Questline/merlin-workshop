using Awaken.TG.Main.Locations.Gems;
using Awaken.TG.Main.Locations.Gems.GemManagement;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Relics: Open")]
    public class SEditorOpenGemsUI : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SOpenGemsUI {
                targetChapter = parser.GetChapter(TargetNode() as ChapterEditorNode),
            };
        }
    }

    public partial class SOpenGemsUI : StoryStep {
        public StoryChapter targetChapter;
        
        bool _anyRelicAttached;
        
        public override StepResult Execute(Story story) {
            if (story.Hero != null) {
                var relicsUI = GemsUI.OpenGemsUI();
                var result = new StepResult();
                _anyRelicAttached = false;
                relicsUI.ListenTo(Model.Events.AfterDiscarded, _ => OnRelicsUIClose(story, result), story);
                World.EventSystem.ListenTo(EventSelector.AnySource, GemManagementUI.Events.GemAttached, relicsUI, _ => _anyRelicAttached = true);
                return result;
            }
            
            Log.Important?.Error($"Hero {story.Hero} is invalid");
            return StepResult.Immediate;
        }

        void OnRelicsUIClose(Story api, StepResult result) {
            if (_anyRelicAttached) {
                api.JumpTo(targetChapter);
            }
            result.Complete();
        }
        
        public override string GetKind(Story story) {
            return "RelicsUI";
        }
    }
}