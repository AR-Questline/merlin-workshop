using Awaken.TG.Main.Heroes.Statuses.Attachments;

namespace Awaken.TG.Main.Character {
    public struct TrialBuildupData {
        public ICharacter buildupReceiver;
        public BuildupAttachment buildupAttachment;
        public ICharacter buildupDealer;
        
        public TrialBuildupData(ICharacter buildupReceiver, BuildupAttachment buildupAttachment, ICharacter buildupDealer) {
            this.buildupReceiver = buildupReceiver;
            this.buildupAttachment = buildupAttachment;
            this.buildupDealer = buildupDealer;
        }
    }
}