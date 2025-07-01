using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Memories;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Effectors {
    public partial class FlagEffector : Element<Objective>, IObjectiveEffector, IRefreshedByAttachment<FlagEffectorAttachment> {
        public override ushort TypeForSerialization => SavedModels.FlagEffector;

        [Saved] public ObjectiveState RunOnState { get; private set; }
        string Flag { get; set; }

        public void InitFromAttachment(FlagEffectorAttachment spec, bool isRestored) {
            Flag = spec.Flag;
            RunOnState = spec.RunOnState;
        }

        public void OnStateUpdate(QuestUtils.ObjectiveStateChange stateChange) {
            if (stateChange.newState == RunOnState) {
                Services.Get<GameplayMemory>().Context().Set(Flag, true);
                Discard();
            }
        }
    }
}