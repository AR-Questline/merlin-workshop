using System;
using System.Collections.Generic;
using Awaken.TG.Main.Locations.Setup;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging {
    public class StaticRenderersWindow : OdinEditorWindow {
        [MenuItem("TG/Assets/Find non static", priority = -100)]
        static void OpenWindow() {
            var window = GetWindow<StaticRenderersWindow>();
            window.Show();
        }

        [ShowInInspector, ListDrawerSettings(IsReadOnly = true), SerializeField]
        InvalidEntry[] invalidEntries = Array.Empty<InvalidEntry>();

        [Button]
        public void Find() {
            var allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            var invalidRenderers = new List<InvalidEntry>();
            foreach (var renderer in allRenderers) {
                if (renderer.GetComponentInParent<LocationSpec>()) {
                    continue;
                }
                if (!renderer.gameObject.isStatic) {
                    invalidRenderers.Add(new() { renderer = renderer });
                }
            }
            invalidEntries = invalidRenderers.ToArray();
        }
        
        [InlineProperty, HideLabel, HideReferenceObjectPicker, Serializable]
        public class InvalidEntry {
            [ShowInInspector, InlineButton(nameof(Focus))]
            public Renderer renderer;

            public void Focus() {
                Selection.activeObject = renderer;
                EditorGUIUtility.PingObject(renderer);

                var sceneView = SceneView.lastActiveSceneView;
                if (!sceneView) {
                    return;
                }
                sceneView.Focus();
                sceneView.FrameSelected();
            }
            
            [Button(ButtonStyle.FoldoutButton, Expanded = true)]
            public void MakeStatic(bool occluder = true, bool occludee = true) {
                var flags = StaticEditorFlags.BatchingStatic |
                            StaticEditorFlags.ReflectionProbeStatic;
                if (occluder) {
                    flags |= StaticEditorFlags.OccluderStatic;
                }
                if (occludee) {
                    flags |= StaticEditorFlags.OccludeeStatic;
                }
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, flags);
                EditorUtility.SetDirty(renderer.gameObject);
            }
        }
    }
}
