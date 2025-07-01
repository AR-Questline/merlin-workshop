using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/To Do")]
    public class SEditorToDo : EditorStep {
        [HideLabel, TextArea(1, 20), UsedImplicitly]
        public string todo;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return null;
        }
    }
}