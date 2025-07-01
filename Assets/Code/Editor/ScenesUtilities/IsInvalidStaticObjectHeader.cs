using Awaken.TG.Editor.Utility;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.ScenesUtilities {
    public class IsInvalidStaticObjectHeader {
        static GUIStyle s_labelStyle;

        [InitializeOnLoadMethod]
        static void InitHeader() {
            UnityEditor.Editor.finishedDefaultHeaderGUI -= OnPostHeaderGUI;
            UnityEditor.Editor.finishedDefaultHeaderGUI += OnPostHeaderGUI;
        }

        static void OnPostHeaderGUI(UnityEditor.Editor editor) {
            var targets = editor.targets;
            GameObject onlyTarget = null;
            if (targets.Length == 1) {
                onlyTarget = targets[0] as GameObject;
            } else {
                foreach (var target in targets) {
                    if (target is GameObject gameObject) {
                        if (onlyTarget) {
                            onlyTarget = null;
                            break;
                        } else {
                            onlyTarget = gameObject;
                        }
                    }
                }
            }

            if (onlyTarget == null) {
                return;
            }

            var parent = onlyTarget.transform.parent;
            while (parent != null) {
                if (ScenesStaticSubdivision.IsObjectHierarchyImportant(parent.gameObject)) {
                    return;
                }

                parent = parent.parent;
            }

            if (ScenesStaticSubdivision.IsByDesignRootGameObject(onlyTarget)) {
                return;
            }

            if (ScenesStaticSubdivision.IsStaticObject(onlyTarget)) {
                if (ScenesStaticSubdivision.IsInvalidStaticObject(onlyTarget)) {
                    s_labelStyle ??= new GUIStyle(GUI.skin.label) {
                        normal = { textColor = Color.red },
                        hover = { textColor = Color.red },
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 16,
                    };
                    GUILayout.Label("Invalid Static Object", s_labelStyle);
                }
            }
        }
    }
}
