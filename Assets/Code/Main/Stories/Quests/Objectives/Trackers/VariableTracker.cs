using Awaken.Utility;
using System.Linq;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Utility.Attributes;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Trackers {
    public partial class VariableTracker : BaseSimpleTracker<VariableTrackerAttachment> {
        public override ushort TypeForSerialization => SavedModels.VariableTracker;

        [Saved] public string Key { get; private set; }
        string[] _contexts;

        public override void InitFromAttachment(VariableTrackerAttachment spec, bool isRestored) {
            base.InitFromAttachment(spec, isRestored);
            Key = spec.key;
            _contexts = spec.contexts.ToArray();
        }

        protected override void OnInitialize() {
            this.GetOrCreateTimeDependent().WithLateUpdate(ProcessLateUpdate);
        }

        void ProcessLateUpdate(float _) {
            float val = Services.Get<GameplayMemory>().Context(_contexts).Get(Key, 0f);
            SetTo(val);
        }
    }
}