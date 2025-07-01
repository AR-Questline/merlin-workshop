using Awaken.Utility;
using System;
using System.Collections.Generic;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Locations.Actions.Customs {
    public partial interface ITrialElement : IElement<Location> {
        public float TrialRemainingDuration { get; }
        public float TrialDuration { get; }
        public string TrialTitle { get; }

        public static class Events {
            public static readonly Event<ITrialElement, float> TrialTimeUpdate = new(nameof(TrialTimeUpdate));
            public static readonly Event<ITrialElement, bool> TrialEnded = new(nameof(TrialEnded));
        }

        public void StartTrial();
        public void FailTrial();
        public void CompleteTrial();
        public void ReactivateTrialAfterFail();
        public void ClaimReward();


        [Serializable]
        internal enum TrialState : byte {
            Available,
            Unavailable,
            InProgress,
            AwaitingReward,
        }
        
        public partial class TrialReactivateDeferredAction : DeferredAction {
            public override ushort TypeForSerialization => SavedTypes.TrialReactivateDeferredAction;

            [Saved] WeakModelRef<ITrialElement> _trial;

            [JsonConstructor, UnityEngine.Scripting.Preserve]
            TrialReactivateDeferredAction() {}

            public TrialReactivateDeferredAction(ITrialElement trial, IEnumerable<DeferredCondition> conditions) : base(conditions) {
                _trial = new WeakModelRef<ITrialElement>(trial);
            }

            public override DeferredSystem.Result TryExecute() {
                if (_trial.TryGet(out var trial)) {
                    trial.ReactivateTrialAfterFail(); 
                    return DeferredSystem.Result.Success;
                }
                return DeferredSystem.Result.Fail;
            }
        }
    }
}