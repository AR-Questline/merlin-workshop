using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Interfaces;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.RichEnums;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("UI/Popup: Choice")]
    public class SEditorPopupChoice : EditorStep, IStoryTextRef {
        public SingleChoice choice;
        [RichEnumExtends(typeof(KeyBindings))]
        public RichEnumReference keyBinding = KeyBindings.UI.Generic.Accept;
        [NodeEnum]
        public SPopupChoice.PromptType promptType;

        public LocString Text => choice.text;
        public override bool MayHaveContinuation => true;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SPopupChoice {
                choice = choice.ToRuntimeChoice(parser),
                targetChapter = parser.GetChapter(TargetNode() as ChapterEditorNode),
                keyBinding = keyBinding,
                promptType = promptType,
            };
        }
    }
    
    public partial class SPopupChoice : StoryStep {
        public RuntimeChoice choice;
        public RichEnumReference keyBinding;
        public PromptType promptType;
        public StoryChapter targetChapter;
        
        KeyBindings KeyBinding => keyBinding.Enum as KeyBindings;

        public SPopupChoice() {
            AutoPerformed = false;
        }
        
        public override StepResult Execute(Story story) {
            choice.targetChapter = targetChapter;
            
            if (ShouldExecuteInstantly(story)) {
                PerformChoice();
                return StepResult.Immediate;
            }
            
            var prompt = promptType switch {
                PromptType.Tap => Prompt.Tap(KeyBinding, choice.text.Translate(), PerformChoice),
                PromptType.Hold => Prompt.Hold(KeyBinding, choice.text.Translate(), PerformChoice),
                _ => throw new System.ArgumentOutOfRangeException(nameof(promptType), promptType, null)
            };
            prompt.AddAudio();
            prompt.SetupState(true, true);
            
            var choiceConfig = ChoiceConfig.WithPrompt(prompt);
            story.OfferChoice(choiceConfig);

            return StepResult.Immediate;

            void PerformChoice() {
                story.Clear();
                story.JumpTo(choice.targetChapter);
                StoryUtilsRuntime.StepPerformed(story, this);
            }
        }
        
        protected virtual bool ShouldExecuteInstantly(Story story) => DebugReferences.ImmediateStory;
        
        public enum PromptType : byte {
            Tap = 0,
            Hold = 1,
        }
    }
}