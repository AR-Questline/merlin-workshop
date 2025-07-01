using System;
using System.Reflection;
using Awaken.TG.Main.Fights.FPP;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.Animations;

namespace Awaken.TG.Editor.AssetManager {
    [CustomEditor(typeof(AnimatorParameter))]
    public class AnimatorParameterEditor : OdinEditor {

        AnimatorParameter Target => (AnimatorParameter) target;
        
        public override void OnInspectorGUI() {
            if (Target.controller == null) {
                Type animatorWindowType = Type.GetType("UnityEditor.Graphs.AnimatorControllerTool, UnityEditor.Graphs");
                var window = EditorWindow.GetWindow(animatorWindowType);
                var controllerField = animatorWindowType?.GetField("m_AnimatorController", BindingFlags.Instance | BindingFlags.NonPublic);
                AnimatorController controller = controllerField?.GetValue(window) as AnimatorController;
                Target.controller = controller;
            }
            base.OnInspectorGUI();
        }
    }
}
