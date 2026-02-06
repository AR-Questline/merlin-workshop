using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Locations.Deferred {
    public abstract partial class DeferredAction {
        public abstract ushort TypeForSerialization { get; }
        
        [Saved] DeferredCondition[] _conditions;
        [Saved] SceneReference _sceneReference;

        public SceneReference SceneReference => _sceneReference;
        protected virtual bool CanBeExecuted => true;
        
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        protected DeferredAction() {}

        protected DeferredAction(IEnumerable<DeferredCondition>  conditions, SceneReference sceneReference = null) {
            this._conditions = conditions.ToArray();
            this._sceneReference = sceneReference;
        }

        public bool ConditionsFulfilled() {
            return CanBeExecuted && _conditions.All(c => c.Fulfilled());
        }

        public abstract DeferredSystem.Result TryExecute();

        public virtual bool HasSimilarConditions(DeferredAction other) { 
            if (other._conditions.Length != _conditions.Length) { 
                return false; 
            }
            for (int i = 0; i < _conditions.Length; i++) {
                if (!_conditions[i].Equals(other._conditions[i])) {
                    return false;
                }
            }
            return true;
        }
    }
}