using Awaken.TG.Main.Stories.Steps;

namespace Awaken.TG.Main.Stories.Runtime.Nodes {
    public class StartNode : StoryChapter {
        public bool enableChoices;
        public bool involveHero;
        public bool involveAI;

        public SStoryStartChoice[] choices;
    }
}