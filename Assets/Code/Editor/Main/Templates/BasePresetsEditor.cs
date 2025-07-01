using System.Collections.Generic;
using System.Reflection;
using Awaken.TG.Editor.Main.Templates.Presets;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Templates {
    public abstract class BasePresetsEditor : OdinEditor {
        // List<ITemplatePreset> _presets = new();
        //
        // delegate ITemplatePreset ButtonRetriever(Component template);
        //
        // protected override void OnEnable() {
        //     base.OnEnable();
        //     foreach (MethodInfo method in TypeCache.GetMethodsWithAttribute<PresetAttribute>()) {
        //         PresetAttribute attribute = AttributesCache.GetCustomAttribute<PresetAttribute>(method);
        //         if (!attribute.ObjectType.IsInstanceOfType(target)) {
        //             continue;
        //         }
        //         if (!method.IsStatic) {
        //             Log.When(LogType.Important)?.Error($"Preset method must be static. Method: {method.Name}");
        //             continue;
        //         }
        //         // Cast to delegate to ensure proper signature
        //         ButtonRetriever retriever = (ButtonRetriever) method.CreateDelegate(typeof(ButtonRetriever));
        //         _presets.Add(retriever.Invoke((Component) target));
        //     }
        // }
        //
        // public override void OnInspectorGUI() {
        //     EditorGUILayout.BeginHorizontal();
        //     foreach (var preset in _presets) {
        //         preset.Draw((Component)target);
        //     }
        //     EditorGUILayout.EndHorizontal();
        //     base.OnInspectorGUI();
        // }
    }
}