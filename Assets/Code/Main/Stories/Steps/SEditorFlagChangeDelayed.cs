using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility;
using Awaken.Utility.Times;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Delayed/Flag: Change Delayed")]
     public class SEditorFlagChangeDelayed : EditorStep {
         public ARTimeSpan timeSpan;
         
         [HideLabel][Tags(TagsCategory.Flag)]
         public string flag = "";
         public bool newState = true;

         protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
             return new SFlagChangeDelayed {
                 timeSpan = timeSpan,
                 flag = flag,
                 newState = newState
             };
         }
     }

    public partial class SFlagChangeDelayed : StoryStepWithTimeRequirement {
        public string flag = "";
        public bool newState = true;

        public override DeferredStepExecution GetStepExecution(Story story) {
            return new StepExecution(flag, newState);
        }

        public partial class StepExecution : DeferredStepExecution {
            public override ushort TypeForSerialization => SavedTypes.StepExecution;

            [Saved] string _flag;
            [Saved] bool _newState;
            
            public StepExecution(string flag, bool newState) {
                _flag = flag;
                _newState = newState;
            }
            
            public override void Execute() {
                StoryFlags.Set(_flag, _newState);
            }
        }
    }
}