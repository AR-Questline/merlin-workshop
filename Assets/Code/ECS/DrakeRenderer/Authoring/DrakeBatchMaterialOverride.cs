using System;
using System.Collections.Generic;
using Awaken.Utility.Assets;
using Awaken.Utility.Scenes;
using UnityEngine;
using UnityEngine.AddressableAssets;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Awaken.ECS.DrakeRenderer.Authoring {
    public class DrakeBatchMaterialOverride : ScenePreProcessorComponent {
        [SerializeField] GameObject[] roots;
        [SerializeField] Replacement[] replacements;

        public void Apply(List<DrakeMeshRenderer> toRevert = null) {
            foreach (var root in roots) {
                foreach (var renderer in root.GetComponentsInChildren<DrakeMeshRenderer>()) {
                    var materials = renderer.MaterialReferences;
                    bool applied = false;
                    for (int i = 0; i < materials.Length; i++) {
                        if (GetReplacement(materials[i], out var replacement)) {
                            materials[i] = replacement;
                            applied = true;
                        }
                    }
                    if (applied) {
                        toRevert?.Add(renderer);
                    }
#if UNITY_EDITOR
                    EditorUtility.SetDirty(renderer);
#endif
                }
            }
        }

        public static void Revert(List<DrakeMeshRenderer> renderers) {
#if UNITY_EDITOR
            foreach (var renderer in renderers) {
                PrefabUtility.RevertObjectOverride(renderer, InteractionMode.AutomatedAction);
            }
#endif
        }
        
        bool GetReplacement(AssetReference original, out AssetReference newReplacement) {
            foreach (var replacement in replacements) {
                if (AssetReferenceUtils.Equals(replacement.original,  original)) {
                    newReplacement = AssetReferenceUtils.Copy(replacement.replacement);
                    return true;
                }
            }
            newReplacement = null;
            return false;
        }
        
        public override void Process() {
            Apply();
        }

        [Serializable]
        public struct Replacement {
            public AssetReference original;
            public AssetReference replacement;
        }

#if UNITY_EDITOR
        public struct EditorAccess {
            DrakeBatchMaterialOverride _instance;
            
            public EditorAccess(DrakeBatchMaterialOverride instance) {
                _instance = instance;
            }
            
            public ref GameObject[] Roots => ref _instance.roots;
            public ref Replacement[] Replacements => ref _instance.replacements;
        }
#endif
    }
}