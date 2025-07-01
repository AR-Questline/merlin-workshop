using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.MVC.Attributes;

namespace Awaken.TG.Main.Stories.Choices {
    [SpawnsView(typeof(VStartStoryChoice))]
    public partial class StartStoryChoice : Choice {
        public StartStoryChoice(ChoiceConfig choiceConfig, Story story) : base(choiceConfig, story) { }
    }
}
