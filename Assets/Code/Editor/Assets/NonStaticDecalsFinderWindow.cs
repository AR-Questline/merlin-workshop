using System;
using System.Collections.Generic;
using Awaken.TG.Editor.Utility;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Editor.Assets {
    public class NonStaticDecalsFinderWindow : OdinEditorWindow {
        public List<NonStaticDecal> nonStaticDecals;

        [Button]
        void ConvertAllToStatic() {
            foreach (var nonStaticDecal in nonStaticDecals) {
                nonStaticDecal.MakeStatic();
            }
            nonStaticDecals.Clear();
        }

        [Button]
        void FindAllNonStaticDecals() {
            nonStaticDecals = new List<NonStaticDecal>();
            var decalProjectors = FindObjectsByType<DecalProjector>(FindObjectsSortMode.None);
            foreach (var decalProjector in decalProjectors) {
                if (!decalProjector.gameObject.isStatic && !ScenesStaticSubdivision.IsNonStaticObject(decalProjector.gameObject)) {
                    var nonStaticDecal = new NonStaticDecal {
                        decalProjector = decalProjector
                    };
                    nonStaticDecals.Add(nonStaticDecal);
                }
            }
        }

        [MenuItem("TG/Assets/Non static decals finder")]
        private static void ShowWindow() {
            var window = GetWindow<NonStaticDecalsFinderWindow>();
            window.titleContent = new GUIContent("Non static decals finder");
            window.Show();
        }

        [Serializable]
        public struct NonStaticDecal {
            [HorizontalGroup]
            public DecalProjector decalProjector;

            [HorizontalGroup, Button]
            public void MakeStatic() {
                decalProjector.gameObject.isStatic = true;
                EditorUtility.SetDirty(decalProjector);
                EditorUtility.SetDirty(decalProjector.gameObject);
            }

            [ShowInInspector, HideLabel]
            public string ScenePath => decalProjector.gameObject.HierarchyPath();
        }
    }
}
