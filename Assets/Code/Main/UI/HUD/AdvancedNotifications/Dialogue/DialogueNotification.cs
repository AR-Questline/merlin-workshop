using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.Dialogue {
    public partial class DialogueNotification : Element<DialogueNotificationBuffer>, IAdvancedNotification {
        public sealed override bool IsNotSaved => true;

        public readonly DialogueData dialogueData;

        public DialogueNotification(DialogueData dialogueData) {
            this.dialogueData = dialogueData;
        }

        protected override void OnInitialize() {
            var story = dialogueData.story;
            story.ListenTo(Events.AfterDiscarded, _ => Discard(), this);
        }
        
        public void Show() {
            World.SpawnView<VDialogueNotification>(this, true);
        }
    }
}