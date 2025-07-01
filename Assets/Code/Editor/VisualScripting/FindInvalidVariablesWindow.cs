using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Editor.VisualScripting {
    public class FindInvalidVariablesWindow : OdinEditorWindow {
        [SerializeField] List<GameObject> _invalidVariables = new List<GameObject>();

        [Button]
        void Find() {
            _invalidVariables.Clear();
            FindInVariablesScript();

            FindInScriptMachines();
        }

        void FindInVariablesScript() {
            var allVariables = FindObjectsByType<Variables>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var variable in allVariables) {
                if (HasInvalidVariable(variable)) {
                    _invalidVariables.Add(variable.gameObject);
                }
            }
        }

        void FindInScriptMachines() {
            var allMachines = FindObjectsByType<ScriptMachine>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var machine in allMachines) {
                var graph = machine.graph;
                if (HasInvalidVariable(graph.variables)) {
                    _invalidVariables.Add(machine.gameObject);
                    continue;
                }
                var asset = machine.nest.macro;
                if (asset != null) {
                    var dependencies = AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(asset));
                    foreach (var dependency in dependencies) {
                        if (dependency.EndsWith(".prefab")) {
                            _invalidVariables.Add(machine.gameObject);
                            break;
                        }
                    }
                }
            }
        }

        static bool HasInvalidVariable(Variables variables) {
            var isInvalid = false;
            foreach (var declaration in variables.declarations) {
                if (HasInvalidVariable(declaration)) {
                    isInvalid = true;
                    break;
                }
            }
            return isInvalid;
        }

        static bool HasInvalidVariable(VariableDeclarations variables) {
            var isInvalid = false;
            foreach (var declaration in variables) {
                if (HasInvalidVariable(declaration)) {
                    isInvalid = true;
                    break;
                }
            }
            return isInvalid;
        }

        static bool HasInvalidVariable(VariableDeclaration declaration) {
            var value = declaration.value;
            if (value is GameObject gameObject && gameObject != null && PrefabUtility.IsPartOfPrefabAsset(gameObject)) {
                return true;
            }

            if (value is VisualEffectAsset vfxAsset && vfxAsset != null) {
                return true;
            }

            if (value is Object unityObject && unityObject != null && PrefabUtility.IsPartOfPrefabAsset(unityObject)) {
                return true;
            }
            return false;
        }

        [MenuItem("TG/Visual Scripting/Find Invalid Variables")]
        static void ShowWindow() {
            var window = GetWindow<FindInvalidVariablesWindow>();
            window.titleContent = new GUIContent("Find Invalid Variables");
            window.Find();
            window.Show();
        }
    }
}
