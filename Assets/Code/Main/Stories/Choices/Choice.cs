using System;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Choices.ChoicePreviews;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Stories.Choices {
    [SpawnsView(typeof(VChoice))]
    public partial class Choice : Element<Story> {
        // === Properties
        public ShareableSpriteReference IconReference { get; }
        public string IconName { get; }
        public string ButtonText { get; }
        public string Tooltip { get; set; }
        public string EffectAndCost { get; }
        public bool Enable { get; }
        public bool IsMainChoice { get; }
        public bool IsHighlighted { get; }
        public StructList<IHoverInfo> HoverInfos { get; }
        public Action Callback { get; }
        public Story Story { get; }
        public Choice[] Choices => _choices ?? ParentModel.Elements<Choice>().ToArraySlow();
        Choice[] _choices;
        public int CurrentIndex => Choices.IndexOf(this);
        
        // === Initialization
        public Choice(ChoiceConfig choiceConfig, Story story) {
            Story = story;
            IconReference = choiceConfig.SpriteIcon();
            IconName = choiceConfig.IconName(story);
            ButtonText = StoryText.FormatVariables(story, choiceConfig.DisplayText() ?? "");
            Tooltip = StoryText.FormatVariables(story, choiceConfig.Tooltip());
            EffectAndCost = choiceConfig.KnownEffects(story);
            Enable = choiceConfig.ChoiceEnabled(story);
            IsMainChoice = choiceConfig.IsMainChoice();
            IsHighlighted = choiceConfig.IsHighlighted;
            HoverInfos = choiceConfig.CollectHoverInfos(story);
            Callback = choiceConfig.Callback() ?? (() => {
                Story.Clear();
                Story.JumpTo(choiceConfig.TargetChapter());
            });
        }

        protected override void OnInitialize() {
            if (HoverInfos.Count > 0) {
                AddElement(new ChoicePreview(HoverInfos));
            }
        }
    }
}
