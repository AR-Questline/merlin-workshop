using System.Collections.Generic;
using Awaken.TG.Main.Stories;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Memories.Journal.Conditions.Models {
    public class FlagsRuntime : ConditionRuntime {
        public override bool IsNotSaved => true;

        Dictionary<string, List<FlagCondition>> _flagAndConditions = new(10);

        protected override void OnInitialize() {
            base.OnInitialize();
            World.EventSystem.ListenTo(EventSelector.AnySource, StoryFlags.Events.FlagChanged, this, OnFlagChange);
        }

        public void RegisterCondition(FlagCondition condition) {
            foreach (var flag in condition.Flags) {
                if (_flagAndConditions.TryGetValue(flag, out var flagConditions)) {
                    flagConditions.Add(condition);
                } else {
                    _flagAndConditions[flag] = new List<FlagCondition> {
                        condition
                    };
                }
            }
        }

        public void UnregisterCondition(FlagCondition condition) {
            foreach (var flag in condition.Flags) {
                if (_flagAndConditions.TryGetValue(flag, out var flagConditions)) {
                    if (flagConditions.Count <= 1) {
                        _flagAndConditions.Remove(flag);
                    } else {
                        flagConditions.Remove(condition);
                    }
                }
            }

            if (_flagAndConditions.Count == 0) {
                Discard();
            }
        }

        void OnFlagChange(string flag) {
            if (_flagAndConditions.TryGetValue(flag, out var flagConditions)) {
                if (!StoryFlags.Get(flag)) {
                    return;
                }
                for (int i = flagConditions.Count - 1; i >= 0; i--) {
                    flagConditions[i].OnFlagChanged(this);
                }
            }
        }
    }
}