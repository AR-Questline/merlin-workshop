using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Universal;

namespace Awaken.TG.Main.Stories.Debugging {
    [SpawnsView(typeof(VModalBlocker), false)]
    [SpawnsView(typeof(VDebugStartStory))]
    public partial class DebugStartStory : Model {
        public override Domain DefaultDomain => Domain.Gameplay;
        public sealed override bool IsNotSaved => true;

        public Story Story { get; }
        
        public DebugStartStory(Story story) {
            Story = story;
        }

        protected override void OnInitialize() {
            Story.IsDebugPaused = true;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            Story.IsDebugPaused = false;
        }
    }
}