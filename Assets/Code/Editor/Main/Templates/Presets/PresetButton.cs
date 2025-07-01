using System;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Templates.Presets {
    public interface ITemplatePreset {
        void Draw(Component t);
    }
    
    public class PresetButton : ITemplatePreset {
        public string name;
        public Action<Component> onClick;
        
        public PresetButton(string name, Action<Component> onClick) {
            this.name = name;
            this.onClick = onClick;
        }

        public void Draw(Component t) {
            if (GUILayout.Button(name, GUILayout.Width(100), GUILayout.Height(45))) {
                onClick(t);
                EditorUtility.SetDirty(t);
                EditorUtility.SetDirty(t.gameObject);
            }
        }
    }
}