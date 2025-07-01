using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Interfaces;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.MVC;
using Awaken.Utility.Times;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Steps {
    /// <summary>
    /// One step that supports adding multiple choices - mainly for ease of use.
    /// </summary>
    [Element("Branch: Choice")]
    public class SEditorChoice : EditorStep, IStoryTextRef, IOncePer {
        
        // === Fields
        public SingleChoice choice;
        public EventReference audioClip;

        [HideInInspector]
        public string spanFlag;
        
        [LabelText("Once Per")]
        public TimeSpans span = TimeSpans.None;
        
        public ChoiceIcon choiceIcon;

        // === Properties
        public LocString Text => choice.text;
        public override bool MayHaveContinuation => true;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SChoice {
                choice = choice.ToRuntimeChoice(parser),
                audioClip = audioClip,
                spanFlag = spanFlag,
                span = span,
                choiceIcon = choiceIcon,
                targetChapter = parser.GetChapter(TargetNode() as ChapterEditorNode),
            };
        }

        string IOncePer.SpanFlag {
            get => spanFlag;
            set => spanFlag = value;
        }
        TimeSpans IOncePer.Span => span;
    }
    
    public partial class SChoice : StoryStep, IOncePer {
        public RuntimeChoice choice;
        public EventReference audioClip;
        public string spanFlag;
        public TimeSpans span = TimeSpans.None;
        public ChoiceIcon choiceIcon;
        public bool shouldDisplayExitIcon;
        public bool shouldDisplayShopIcon;
        
        public StoryChapter targetChapter;

        public SChoice() {
            AutoPerformed = false;
        }
        
        public override StepResult Execute(Story story) {
            choice.targetChapter = targetChapter;
            
            if (ShouldExecuteInstantly(story)) {
                PerformChoice();
                return StepResult.Immediate;
            }
            
            var choiceConfig = ChoiceConfig.WithEverything(choice, string.Empty, PerformChoice);
            TryToSetAdditionalChoiceIcon(choiceConfig);
            
            story.OfferChoice(choiceConfig);
            if (!audioClip.IsNull) {
                // TODO: Implement system that allows skipping dialogue lines
                //RuntimeManager.PlayOneShot(audioClip);
            }

            return StepResult.Immediate;

            void PerformChoice() {
                story.Clear();
                story.JumpTo(choice.targetChapter);
                StoryUtilsRuntime.StepPerformed(story, this);
            }
        }
        
        protected virtual bool ShouldExecuteInstantly(Story story) => DebugReferences.ImmediateStory;
        public virtual bool ShouldBeAvailable(Story story) => true;
        
        void TryToSetAdditionalChoiceIcon(ChoiceConfig choiceConfig) {
            if (choiceIcon == ChoiceIcon.Exit) {
                choiceConfig.WithSpriteIcon(World.Services.Get<CommonReferences>().ExitDialogIcon);
            } else if (choiceIcon == ChoiceIcon.Shop) {
                choiceConfig.IsHighlighted = true;
                choiceConfig.WithSpriteIcon(World.Services.Get<CommonReferences>().ShopDialogIcon);
            }
        }

        string IOncePer.SpanFlag {
            get => spanFlag;
            set => spanFlag = value;
        }
        TimeSpans IOncePer.Span => span;
    }

    public enum ChoiceIcon : byte {
        None,
        Exit,
        Shop,
    }
}