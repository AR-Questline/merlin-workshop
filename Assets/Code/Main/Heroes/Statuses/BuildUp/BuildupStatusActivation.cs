using Awaken.TG.Main.Heroes.Statuses.Attachments;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Skills;

namespace Awaken.TG.Main.Heroes.Statuses.BuildUp {
    public partial class BuildupStatusActivation : BuildupStatus {
        public BuildupStatusActivation(BuildupAttachment data, StatusTemplate statusTemplate, StatusSourceInfo sourceInfo) : base(data, statusTemplate, sourceInfo) { }
        
        protected override void OnBuildupComplete() {
            ActivateStatus();
        }
    }
}