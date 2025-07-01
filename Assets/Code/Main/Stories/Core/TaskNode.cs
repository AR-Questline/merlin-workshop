using System;
using Awaken.TG.Main.Utility.RichLabels;
using Awaken.TG.Main.Utility.RichLabels.SO;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Core {
    [NodeWidth(300)]
    public class TaskNode : StoryNode, IRichLabelUser {
        public override Type GenericType => typeof(TaskNode);
        
        public RichLabelSet taskLabels;
        
        [TextArea(1, 200), HideLabel]
        public string comment;

        // === IRichLabelUser ===
        public RichLabelSet RichLabelSet => taskLabels;
        public RichLabelConfigType RichLabelConfigType => RichLabelConfigType.StoryTask;
        bool IRichLabelUser.DisplayDropdown => false;
        bool IRichLabelUser.AutofillEnabled => false;
        void IRichLabelUser.Editor_Autofill() { }
    }
}