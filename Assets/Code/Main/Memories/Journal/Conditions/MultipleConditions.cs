using System;
using System.Collections.Generic;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.Memories.Journal.Conditions {
    [Serializable]
    public class MultipleConditions : Condition {
        [field: SerializeReference] Condition[] conditions = Array.Empty<Condition>();
        [SerializeField] bool requireJustOne;
        
        public override void Initialize(Model owner) {
            if (IsMet()) return;
            if (InvalidSetup()) {
                Log.Important?.Info("Invalid setup for MultipleConditions");
                return;
            }

            foreach (var condition in conditions) {
                condition.Initialize(owner);
            }
        }

        public override bool Validate(bool validateSelf = false) {
            if (IsMet()) return !validateSelf;
            if (InvalidSetup()) {
                return false;
            }
            if (requireJustOne ? AnyCondition() : AllConditions()) {
                ConditionsMet();
                return !validateSelf;
            }
            return false;
        }

        bool AllConditions() {
            foreach (var condition in conditions) {
                condition.Validate();
                if (!condition.IsMet()) {
                    return false;
                }
            }
            return true;
        }
        
        bool AnyCondition() {
            foreach (var condition in conditions) {
                condition.Validate();
                if (condition.IsMet()) {
                    return true;
                }
            }
            return false;
        }

        public override bool InvalidSetup() {
            if (conditions.IsEmpty()) {
                return true;
            }

            foreach (var condition in conditions) {
                if (condition.InvalidSetup()) {
                    return true;
                }
            }
            
            return false;
        }
        
        public override IEnumerable<ConditionData> GetAllConditions() {
            yield return this;
            foreach (var condition in conditions) {
                foreach (var subCondition in condition.GetAllConditions()) {
                    yield return subCondition;
                }
            }
        }

#if UNITY_EDITOR
        public override string EDITOR_PreviewInfo() {
            if (InvalidSetup()) return "!!! Invalid setup !!!: " + base.EDITOR_PreviewInfo();
            string targetNames = string.Join(", ", conditions.GetType().ToString());
            return $"Have all Conditions fulfilled: {targetNames}";
        }
#endif
    }
}