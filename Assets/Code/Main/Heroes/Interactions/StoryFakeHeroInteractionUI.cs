using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI.Keys.Components;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.Interactions {
    [SpawnsView(typeof(VStoryFakeHeroInteractionUI))]
    public class StoryFakeHeroInteractionUI : Element<Story>, IUniqueKeyProvider {
        readonly LocString _title;
        readonly LocString _description;
        readonly StepResult _stepResult;
        
        public string DisplayName => _title.Translate();
        public bool IsIllegal => false;
        public InfoFrame ActionFrame => new(DefaultActionName, true);
        public InfoFrame InfoFrame1 => InfoFrame.Empty;
        public InfoFrame InfoFrame2 => InfoFrame.Empty;
        string DefaultActionName => _description.Translate();
        
        public KeyIcon.Data UniqueKey => new(KeyBindings.Gameplay.Interact, false);

        public StoryFakeHeroInteractionUI(LocString title, LocString description, StepResult stepResult) {
            _title = title;
            _description = description;
            _stepResult = stepResult;
        }

        protected override void OnInitialize() { }
        
        public void OnInteraction() {
            _stepResult.Complete();
            Discard();
        }
    }
}