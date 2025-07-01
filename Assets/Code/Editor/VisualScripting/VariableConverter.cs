using System.Linq;
using System.Text.RegularExpressions;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Editor.VisualScripting {
    public class VariableConverter : OdinEditorWindow {
        public string variable = ".*";
        public VariableKind from;
        public VariableKind to;

        [MenuItem("TG/Visual Scripting/Convert Variable")]
        static void OpenWindow() {
            GetWindow<VariableConverter>().Show();
        }

        [Button]
        void Convert() {
            foreach (var unit in VGConverterUtils.AllUnits().OfType<UnifiedVariableUnit>()) {
                if (unit.name.hasValidConnection) {
                    Log.Important?.Error("GetVariable with name connected. Cannot convert");
                } else if (Regex.IsMatch(unit.name.DefaultValue<string>(), variable)) {
                    if (unit.kind == from) {
                        unit.kind = to;
                    }
                }
            }
            Close();
        }
    }
}