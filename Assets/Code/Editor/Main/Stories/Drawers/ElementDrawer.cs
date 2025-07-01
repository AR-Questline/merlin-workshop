using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Awaken.TG.Main.Stories.Core;
using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace Awaken.TG.Editor.Main.Stories.Drawers {
    public static class ElementDrawer {
        static Dictionary<NodeElement, ElementEditor> s_drawersByStep = new Dictionary<NodeElement, ElementEditor>();

        public static void DrawElement(NodeElement element) {
            // fix broken editors
            if (s_drawersByStep.TryGetValue(element, out ElementEditor editor)) {
                if (editor.target == null || editor.target != element) {
                    s_drawersByStep.Remove(element);
                }
            }

            // create not existing
            if (!s_drawersByStep.ContainsKey(element)) {
                CreateEditor(element);
            }

            // draw
            s_drawersByStep[element].DrawElementGUI();
        }

        static void CreateEditor(NodeElement element) {
            var types = TypeCache.GetTypesWithAttribute<CustomElementEditorAttribute>();

            Type stepEditorType = types.FirstOrDefault(t => IsTypeValidEditor(t, element));
            ElementEditor editor;
            if (stepEditorType == null) {
                editor = new ElementEditor();
            } else {
                editor = (ElementEditor) Activator.CreateInstance(stepEditorType);
            }
            editor.StartElementGUI(element);
            s_drawersByStep.Add(element, editor);
        }

        static bool IsTypeValidEditor(Type type, NodeElement element) {
            Type elementEditorType = type.GetCustomAttribute<CustomElementEditorAttribute>()?.type;
            return elementEditorType != null && elementEditorType.IsInstanceOfType(element);
        }
    }
}