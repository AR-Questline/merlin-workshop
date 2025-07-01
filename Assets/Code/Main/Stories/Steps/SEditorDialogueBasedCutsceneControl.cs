using Awaken.Utility;
using System;
using Awaken.TG.Graphics.Cutscenes;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Sirenix.OdinInspector;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Cutscenes/Cutscene: Dialogue Control"), NodeSupportsOdin]
    public class SEditorDialogueBasedCutsceneControl : EditorStep {
        public ActionData[] actions = Array.Empty<ActionData>();

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SDialogueBasedCutsceneControl {
                actions = actions,
            };
        }
    }

    public partial class SDialogueBasedCutsceneControl : StoryStep {
        public ActionData[] actions;
        
        public override StepResult Execute(Story story) {
            var view = World.Any<Cutscene>()?.View<VCutsceneDialogueControlled>();
            if (view == null) {
                return StepResult.Immediate;
            }

            foreach (var action in actions) {
                view.PerformAction(action.action, action.index, action.toggle, story);
            }
            return StepResult.Immediate;
        }
    }

    [System.Serializable]
    public partial struct ActionData {
        public ushort TypeForSerialization => SavedTypes.ActionData;

        [Saved] public VCutsceneDialogueControlled.Action action;
        [Saved, ShowIf(nameof(ShowIndex))] public int index;
        [Saved, ShowIf(nameof(ShowToggle))] public bool toggle;
        
        bool ShowIndex => action is VCutsceneDialogueControlled.Action.JumpTo;
        bool ShowToggle => action is VCutsceneDialogueControlled.Action.ToggleSync or VCutsceneDialogueControlled.Action.ToggleAutoForward;
    }
}