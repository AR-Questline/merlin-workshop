using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Survey: Open")]
    public class SEditorOpenSurvey : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SOpenSurvey();
        }
    }
    
    public partial class SOpenSurvey : StoryStep {
        const string SurveyLink = "https://store.steampowered.com/app/1199030/Tainted_Grail/";
        
        public override StepResult Execute(Story story) {
            Application.OpenURL(SurveyLink);
            return StepResult.Immediate;
        }
    }
}