using System;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Memories.Journal.Conditions;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Main.Memories.Journal.Entries {
    [Serializable]
    public class SubEntryData {
        [SerializeReference, LabelText("@" + nameof(ConditionText) + "()")]
        ConditionData condition;

        [Space] 
        [SerializeField, LocStringCategory(Category.Journal)] 
        LocString textToShow;

        public ConditionData Condition => condition;
        public LocString TextToShow => textToShow;

        public string ElementLabelText() {
            string labelText = "";
            
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode) {
                labelText = ConditionText();
                return labelText;
            }
            labelText = $"[{condition?.EDITOR_PreviewInfo() ?? "!!! No condition set !!!"}]: {textToShow.Translate().Replace("\n", " ")}";
#else
            labelText = ConditionText();
#endif
            return labelText;
        }

        public string ConditionText() {
            return ConditionData.ConditionInfo(condition);
        }
    }
}