using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Actors;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.Dialogue {
    public readonly struct DialogueData {
        public readonly Story story;
        public readonly Actor actor;
        public readonly string dialogueToDisplay;

        public DialogueData(Story story, Actor actor, string dialogueToDisplay) {
            this.story = story;
            this.actor = actor;
            this.dialogueToDisplay = dialogueToDisplay;
        }
    }
}