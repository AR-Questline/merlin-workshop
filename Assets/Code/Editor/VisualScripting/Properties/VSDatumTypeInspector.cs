using Awaken.TG.Editor.Utility.VSDatums;
using Awaken.TG.Main.Utility.VSDatums;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.VisualScripting.Properties {
    [Inspector(typeof(VSDatumType))]
    public class VSDatumTypeInspector : Inspector {
        public VSDatumTypeInspector(Metadata metadata) : base(metadata) { }
        
        protected override float GetHeight(float width, GUIContent label) {
            return EditorGUIUtility.singleLineHeight;
        }

        public override float GetAdaptiveWidth() {
            return 120;
        }
        
        protected override void OnGUI(Rect position, GUIContent label) {
            Inspector.BeginBlock(metadata, position);
            VSDatumTypeDrawer.Draw(position, (VSDatumType)metadata.value, out var value, out var changed);
            if (Inspector.EndBlock(metadata) && changed) {
                metadata.RecordUndo();
                metadata.value = value;
            }
        }
    }
}