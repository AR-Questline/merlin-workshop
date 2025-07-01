using System;
using Awaken.TG.Main.Heroes.CharacterSheet.Journal.Tabs;
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Memories.Journal.Conditions {
    /// <summary>
    /// Make sure not to save any runtime data in this class
    /// </summary>
    [Serializable]
    public abstract class ConditionData {
        public abstract bool IsMet();
        public abstract void Initialize(Model owner);
        
        public virtual bool InvalidSetup() => false;

        public static string ConditionInfo(ConditionData condition) {
#if UNITY_EDITOR
            if (condition?.InvalidSetup() ?? false) {
                return condition.EDITOR_PreviewInfo();
            }
#endif
            return $"{StringUtil.NicifyTypeName(condition) ?? "!!! No condition set !!!"}";
        }
#if UNITY_EDITOR
        public virtual string EDITOR_PreviewInfo() => StringUtil.NicifyTypeName(this);
#endif
    }
    
    /// <inheritdoc cref="ConditionData"/>
    [Serializable]
    public abstract class Condition : ConditionData {
        [SerializeField] 
        JournalGuid guid;
        
        public JournalGuid Guid => guid;
        public sealed override bool IsMet() => World.Only<PlayerJournal>().WasEntryUnlocked(guid.GUID);
        protected void ConditionsMet() {
            World.Only<PlayerJournal>().UnlockEntry(guid.GUID, JournalSubTabType.Bestiary);
#if UNITY_EDITOR
            Log.Debug?.Info($"Conditions met: {EDITOR_PreviewInfo()}");
#endif
        }
    }
    
    [UnityEngine.Scripting.Preserve]
    public sealed class NoCondition : ConditionData {
        public override bool IsMet() => true;
        public override void Initialize(Model _) { }
    }
    
    [Serializable]
    public class DebugCondition : ConditionData {
        [SerializeField] bool isMet;
        public override bool IsMet() => isMet;
        public override void Initialize(Model _) { }
        
#if UNITY_EDITOR
        public override string EDITOR_PreviewInfo() => isMet ? "Met" : "Not met";
#endif
    }

    [Serializable, InlineProperty]
    public class ManualCondition : Condition {
        public override void Initialize(Model _) { }
    }
}
