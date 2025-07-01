using System;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Saving: Allow|Disallow")]
    public class SEditorSaveChangeAllowed : EditorStep {
        [Tooltip("Use the same id in allow/disallow steps.")]
        public string id;
        public bool allowed;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SSaveChangeAllowed {
                id = id,
                allowed = allowed
            };
        }
    }

    public partial class SSaveChangeAllowed : StoryStep {
        public string id;
        public bool allowed;
        
        public override StepResult Execute(Story story) {
            if (string.IsNullOrWhiteSpace(id)) {
                throw new ArgumentException("You need to specify ID of the saving allowed change");
            }

            if (allowed) {
                World.All<SaveBlocker>().FirstOrDefault(b => b.SourceID == id)?.Discard();
            } else {
                if (World.All<SaveBlocker>().All(b => b.SourceID != id)) {
                    World.Add(new SaveBlocker(id));
                }
            }

            return StepResult.Immediate;
        }
    }
}