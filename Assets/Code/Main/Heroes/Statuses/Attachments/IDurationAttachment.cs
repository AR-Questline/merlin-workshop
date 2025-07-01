using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Locations.Attachments;

namespace Awaken.TG.Main.Heroes.Statuses.Attachments {
    public interface IDurationAttachment : IAttachmentSpec {
        public IDuration SpawnDuration() {
            return (IDuration)this.SpawnElement();
        }
    }
}