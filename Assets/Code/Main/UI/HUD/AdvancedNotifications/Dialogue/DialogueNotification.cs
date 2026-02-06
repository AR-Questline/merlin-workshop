using Awaken.TG.MVC;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.Dialogue {
    public partial class DialogueNotification : AdvancedNotification {
        public readonly DialogueData dialogueData;

        public DialogueNotification(DialogueData dialogueData) {
            this.dialogueData = dialogueData;
        }

        protected override void OnInitialize() {
            var story = dialogueData.story;
            story.ListenTo(Events.AfterDiscarded, _ => Discard(), this);
        }
        
        public override void Show() {
            World.SpawnView<VDialogueNotification>(this, true);
        }
    }
}