using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.CharacterSheet.Journal.Tabs;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Tutorials;
using Awaken.TG.Main.Utility.Video;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using JetBrains.Annotations;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Journal.Content {
    public partial class JournalTutorialContent : Element<IJournalCategoryUI> {
        public sealed override bool IsNotSaved => true;

        Video _video;

        public LocString Title { [UnityEngine.Scripting.Preserve] get; }
        public string Text => TutorialDataOwner.GetTranslatedContentText();
        public ShareableSpriteReference Icon { get; }
        [CanBeNull] public ShareableSpriteReference Graphic { get; }
        [CanBeNull] public LoadingHandle VideoHandle { get; }
        ITutorialDataOwner TutorialDataOwner { get; }
        
        public JournalTutorialContent(TutorialConfig.GraphicTutorial tutorialConfig) {
            TutorialDataOwner = tutorialConfig;
            Title = tutorialConfig.title;
            Graphic = tutorialConfig.graphic;
            Icon = tutorialConfig.icon;
        }
        
        public JournalTutorialContent(TutorialConfig.VideoTutorial tutorialConfig) {
            TutorialDataOwner = tutorialConfig;
            Title = tutorialConfig.title;
            VideoHandle = tutorialConfig.video;
            Icon = tutorialConfig.icon;
        }
        
        public void InitVideo() {
            _video = Video.Custom(View<VJournalTutorialVideoContent>(), VideoHandle, new Video.Config {
                allowSkip = false,
                hideCursor = false,
                loop = true,
            }, this);
            World.Add(_video);
        }

        public void EndVideo() {
            _video?.Discard();
        }
    }
}
