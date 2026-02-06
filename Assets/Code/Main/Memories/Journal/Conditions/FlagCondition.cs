using System;
using Awaken.TG.Main.Memories.Journal.Conditions.Models;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.Memories.Journal.Conditions {
    [Serializable]
    public class FlagCondition : Condition {
        [SerializeField, Tags(TagsCategory.Flag)]
        string[] flags = Array.Empty<string>();

        public string[] Flags => flags;
        public override bool InvalidSetup() => flags.IsEmpty();
        
        public override void Initialize(Model owner) {
            if (IsMet()) return;
            if (InvalidSetup()) {
                Log.Important?.Info("Invalid setup for FlagCondition");
                return;
            }
            
            if (CheckIfConditionsAreMet()) {
                ConditionsMet();
                return;
            }

            if (!owner.TryGetElement(out FlagsRuntime dataModel)) {
                dataModel = owner.AddElement<FlagsRuntime>();
            }
            dataModel.RegisterCondition(this);
        }

        bool CheckIfConditionsAreMet() {
            var context = World.Services.Get<GameplayMemory>().Context(); // StoryFlags.Get() micro optim
            
            foreach (var flag in flags) {
                if (!context.Get<bool>(flag)) {
                    return false;
                }
            }
            return true;
        }
        
        public void OnFlagChanged(FlagsRuntime dataModel) {
            if (CheckIfConditionsAreMet()) {
                ConditionsMet();
                dataModel.UnregisterCondition(this);
            }
        }
        
#if UNITY_EDITOR
        public override string EDITOR_PreviewInfo() {
            if (InvalidSetup()) return "!!! Invalid setup !!!: " + base.EDITOR_PreviewInfo();
            string targetNames = string.Join(", ", flags);
            return $"Have all flags set to true: {targetNames}";
        }
#endif
    }
}
