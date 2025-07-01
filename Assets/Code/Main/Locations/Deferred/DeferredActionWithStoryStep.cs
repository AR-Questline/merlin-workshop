using Awaken.Utility;
using System.Collections.Generic;
using Awaken.TG.Main.Stories;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Locations.Deferred {
    public sealed partial class DeferredActionWithStoryStep : DeferredAction {
        public override ushort TypeForSerialization => SavedTypes.DeferredActionWithStoryStep;

        [Saved] DeferredStepExecution _step;

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        DeferredActionWithStoryStep() {}
        
        public DeferredActionWithStoryStep(DeferredStepExecution step, IEnumerable<DeferredCondition> conditions) : base(conditions) {
            _step = step;
        }

        public override DeferredSystem.Result TryExecute() {
            _step.Execute();
            return DeferredSystem.Result.Success;
        }
    }
}