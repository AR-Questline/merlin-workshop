using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.MVC.Attributes;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.Dialogue {
    [SpawnsView(typeof(VDialogueNotificationBuffer))]
    public partial class DialogueNotificationBuffer : AdvancedNotificationBuffer {
        public sealed override bool IsNotSaved => true;

        protected override int MaxVisibleNotifications => 2;
        protected override bool StrictMaxVisibleNotifications => true;

        DialogueNotification FindDialogue(Story story, Actor actor) {
            return Elements<DialogueNotification>().FirstOrDefault(d => {
                bool storyMatch = d.dialogueData.story == story;
                bool actorMatch = !actor.IsSet || !d.dialogueData.actor.IsSet || d.dialogueData.actor.Equals(actor);
                return storyMatch && actorMatch;
            });
        }

        [UnityEngine.Scripting.Preserve]
        public bool TryToUpdateExistingDialogueNotification(Story story, Actor actor, string dialogueToDisplay) {
            var dialogueNotification = FindDialogue(story, actor);
            if (dialogueNotification != null) {
                dialogueNotification.View<VDialogueNotification>().UpdateDialogueToDisplay(dialogueToDisplay);
                return true;
            }
            return false;
        }

        public void RemoveOldNotification(Story story, Actor actor) {
            var dialogueNotification = FindDialogue(story, actor);
            dialogueNotification?.Discard();
        }
    }
}