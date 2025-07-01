using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Main.Stories.Actors;
using Awaken.Utility.Collections;
using Awaken.Utility.Enums;
using Awaken.Utility.GameObjects;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Stories.Actors {
    [CustomPropertyDrawer(typeof(ActorStateRef))]
    public class ActorStateRefPropertyDrawer : PropertyDrawer {

        GameObject _prefab;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            // label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            // remove indent
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // extract prefab
            if (!ExtractPrefab()) {
                // haven't found prefab
                EditorGUILayout.HelpBox($"Can't find Actors prefab.", MessageType.Error);
                return;
            }

            string actorPropName = property.ExtractAttribute<ActorRefAttribute>().propertyName;
            string actorGuid = property.serializedObject.FindProperty(actorPropName).FindPropertyRelative(nameof(ActorRef.guid)).stringValue;
            string actorPath = _prefab.GetComponent<ActorsRegister>().Editor_GetPathFromGUID(actorGuid);
            string[] possibleStates = GetPossibleStates(actorPath, _prefab).ToArray();

            // get property
            SerializedProperty pathProp = property.FindPropertyRelative("stateName");
            string currentPath = pathProp.stringValue;
            int index = possibleStates.IndexOf(currentPath);

            // draw popup
            EditorGUI.BeginChangeCheck();
            index = EditorGUI.Popup(position, index, possibleStates);
            if (EditorGUI.EndChangeCheck()) {
                pathProp.stringValue = possibleStates[index];
            }
            
            // clean up
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        bool ExtractPrefab() {
            if (_prefab == null) {
                _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ActorsRegister.Path);
            }

            return _prefab != null;
        }

        public static IEnumerable<string> GetPossibleStates(string actorPath, GameObject prefab) {
            if (string.IsNullOrWhiteSpace(actorPath) || RichEnum.AllValuesOfType<DefinedActor>().Any(v => v.ActorPath == actorPath)) {
                yield break;
            }

            Transform actorTransform = prefab.transform.TryGrabChild<Transform>(actorPath.Split('/'));
            
            foreach (Transform child in actorTransform) {
                yield return child.name;
            }
        }
    }
}