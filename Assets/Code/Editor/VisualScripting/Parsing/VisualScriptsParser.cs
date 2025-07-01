using Awaken.TG.Editor.VisualScripting.Parsing.Scripts;
using Unity.VisualScripting;
using UnityEditor;

namespace Awaken.TG.Editor.VisualScripting.Parsing {
    public class VisualScriptsParser : ScriptableWizard {

        public bool parseRecursively = false;
        public int depth = 100;
        public bool forceOverride;
        
        [MenuItem("Assets/Visual Scripting/Parse")]
        static void CreateWizard() {
            ScriptableWizard.DisplayWizard<VisualScriptsParser>("Visual Scripts Parser", "Parse");
        }

        void OnWizardCreate() {
            foreach (var selected in Selection.objects) {
                if (selected is ScriptGraphAsset graphAsset) {
                    UnitMaker.MakeUnit(graphAsset, parseRecursively, depth, forceOverride);
                }
            }
        }
    }
}
