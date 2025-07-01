using System;
using System.Linq;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;

namespace Awaken.TG.Main.Stories.Steps {
    /// <summary>
    /// Sets a variable than can be checked elsewhere.
    /// </summary>
    [Element("Variables/Variable: Set From Change Items Quantity")]
    public class SEditorVariableSetFromItemsQuantity : SEditorVariableReference {
        public override string extraInfo {
            get {
                int index = Parent.elements.IndexOf(this);
                if (index > 0) {
                    var prevNode = Parent.elements[index - 1];
                    if (prevNode is SEditorChangeItemsQuantity changeItemsQuantity) {
                        return
                            $"From {changeItemsQuantity.itemTemplateReferenceQuantityPairs.Select(p => p.itemTemplateReference.TryGet<ItemTemplate>()?.name)}";
                    }
                }
                return "No previous ChangeItemsQuantity found";
            }
        }

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SVariableSetFromItemQuantity {
                comment = comment,
                context = context,
                var = var,
                contexts = contexts
            };
        }
    }

    public partial class SVariableSetFromItemQuantity : SVariableReference {
        public override StepResult Execute(Story story) {
            var.Prepare(story, context, contexts)
                .SetValue(GetValueFromChangeItemsQuantity());
            if (!string.IsNullOrEmpty(comment)) {
                story.ShowText(TextConfig.WithTextAndStyle($"[{comment}]", StoryTextStyle.Aside));
            }
            return StepResult.Immediate;
        }
        
        int GetValueFromChangeItemsQuantity() {
            int index = Array.IndexOf(parentChapter.steps, this);
            if (index > 0) {
                var prevNode = parentChapter.steps[index - 1];
                if (prevNode is SChangeItemsQuantity changeItemsQuantity) {
                    return changeItemsQuantity.GetChangedItemsQuantity();
                }
            }
            return 0;
        }
    }
}