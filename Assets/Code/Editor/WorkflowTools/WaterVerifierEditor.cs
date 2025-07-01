using Awaken.TG.EditorOnly.WorkflowTools;
using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace Awaken.TG.Editor.WorkflowTools {
    [CustomEditor(typeof(WaterVerifier))]
    public class WaterVerifierEditor : OdinEditor {
        WaterVerifier _waterVerifier;
        protected override void OnEnable() {
            base.OnEnable();
            _waterVerifier = (WaterVerifier) target;
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            _waterVerifier.VerifyWater();
        }
    }
}