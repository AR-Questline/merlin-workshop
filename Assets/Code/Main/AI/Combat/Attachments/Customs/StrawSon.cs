using Awaken.Utility;
using System;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Statuses.BuildUp;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.AI.Combat.Attachments.Customs {
    [Serializable]
    public partial class StrawSon : CustomCombatBaseClass {
        public override ushort TypeForSerialization => SavedModels.StrawSon;

        protected override void OnInitialize() {
            base.OnInitialize();
            NpcElement.Statuses.ListenTo(CharacterStatuses.Events.AddedStatus, s => OnStatusAdded(s).Forget(), this);
        }

        async UniTaskVoid OnStatusAdded(Status status) {
            if (status is BuildupStatus buildupStatus && buildupStatus.BuildupStatusStatusType == BuildupStatusType.Burn) {
                if (await AsyncUtil.DelayFrame(this)) {
                    ParentModel.Kill();
                }
            }
        }
    }
}
