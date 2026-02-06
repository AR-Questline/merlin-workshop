using Awaken.TG.Main.Heroes.Statuses.Attachments;
using Awaken.TG.Main.Skills;

namespace Awaken.TG.Main.Character {
    public struct TrialBuildupData {
        public ICharacter buildupReceiver;
        public BuildupAttachment buildupAttachment;
        public StatusSourceInfo sourceInfo;
        public ICharacter buildupDealer;
        
        public TrialBuildupData(ICharacter buildupReceiver, BuildupAttachment buildupAttachment, StatusSourceInfo sourceInfo) {
            this.buildupReceiver = buildupReceiver;
            this.buildupAttachment = buildupAttachment;
            this.sourceInfo = sourceInfo;
            this.buildupDealer = sourceInfo.GetSourceCharacter;
        }
    }
}