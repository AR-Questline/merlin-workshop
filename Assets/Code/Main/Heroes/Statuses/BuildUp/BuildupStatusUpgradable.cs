using Awaken.TG.Main.Heroes.Statuses.Attachments;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Skills;

namespace Awaken.TG.Main.Heroes.Statuses.BuildUp {
    public partial class BuildupStatusUpgradable : BuildupStatus {
        readonly StatusTemplate _nextStatusTemplate;

        public BuildupStatusUpgradable(BuildupAttachment data, StatusTemplate statusTemplate, StatusSourceInfo sourceInfo) : base(data, statusTemplate, sourceInfo) {
            _nextStatusTemplate = data.NextStatusTemplate;
        }
        
        protected override void OnBuildupComplete() {
            var nextStatusData = _nextStatusTemplate.GetComponent<BuildupAttachment>();
            var sourceInfo = StatusSourceInfo.FromStatus(_nextStatusTemplate);
            if (SourceInfo.GetSourceCharacter is { } character) {
                sourceInfo.WithCharacter(character);
            }
            
            if (nextStatusData == null) {
                ParentModel.AddStatus(_nextStatusTemplate, sourceInfo);
                return;
            }

            BuildupStatus.CreateBuildupStatus(nextStatusData, ParentModel, _nextStatusTemplate, sourceInfo, CurrentBuildup);
            Discard();
        }
    }
}