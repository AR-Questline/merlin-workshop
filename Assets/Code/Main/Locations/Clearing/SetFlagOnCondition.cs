using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Stories;
using Awaken.Utility;

namespace Awaken.TG.Main.Locations.Clearing {
    public partial class SetFlagOnCondition : ActionOnConditionBase, IRefreshedByAttachment<SetFlagOnConditionAttachment> {
        public override ushort TypeForSerialization => SavedModels.SetFlagOnCondition;

        SetFlagOnConditionAttachment _spec;
        protected override int AllConditionsToFulfil => _spec.Conditions.Length;
        protected override Condition[] Conditions => _spec.Conditions;
        
        public void InitFromAttachment(SetFlagOnConditionAttachment spec, bool isRestored) {
            _spec = spec;
        }
        
        protected override void OnAllConditionsFulfilled() {
            StoryFlags.Set(_spec.FlagToSet, true);
        } 
    }
}