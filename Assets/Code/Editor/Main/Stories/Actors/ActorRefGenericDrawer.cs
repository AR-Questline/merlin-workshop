using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Helpers;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Steps;
using Awaken.Utility.Collections;
using Awaken.Utility.Editor.MoreGUI;
using Awaken.Utility.Enums;
using Awaken.Utility.UI;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Stories.Actors {
    public class ActorRefGenericDrawer {
        static GUIContent s_xContent = new("X", "Delete");
        static GameObject s_prefab;
        static ActorsRegister s_actorRegister;
        static string[] s_possiblePaths;
        static string[] s_possiblePathsOnlyWithStates;
        
        string[] _possiblePaths;
        GUIContent[] _possiblePathsAsContent;
        GUIContent[] _namesAsContent;
        int? _filterHash;

        public void Draw(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            // label
            if (label != null) {
                position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            }
            // remove indent
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // extract prefab
            if (!ExtractPrefab(property)) {
                // haven't found prefab
                EditorGUILayout.HelpBox($"Can't find Actors prefab.", MessageType.Error);
                return;
            }

            var removeHeroAfter = FilterPaths(property, false, true);

            // get property
            SerializedProperty guidProp = property.FindPropertyRelative(nameof(ActorRef.guid));
            string currentPath = s_actorRegister.Editor_GetPathFromGUID(guidProp.stringValue);
            int index = Array.IndexOf(_possiblePaths, currentPath);
            bool isValidValueForCurrentContext = true;
            if (index == -1) {
                var globalIndex = Array.IndexOf(s_possiblePaths, currentPath);
                if (globalIndex == -1) {
                    index = 0;
                    AssignProperGUIDToProperty(guidProp, _possiblePaths[index]);
                } else {
                    isValidValueForCurrentContext = false;
                }
            }
            

            // draw popup
            if (isValidValueForCurrentContext) {
                DrawSelectionControl(position, guidProp, index);
            } else {
                DrawInvalidActor(position, guidProp);
            }

            if (removeHeroAfter) {
                RemoveHero();
            }

            // clean up
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        void DrawSelectionControl(Rect position, SerializedProperty property, int index) {
            using var mixedValues = new EditorGUI.MixedValueScope(property.hasMultipleDifferentValues);
            
            EditorGUI.BeginChangeCheck();
            index = AREditorPopup.Draw(in position, index, _possiblePathsAsContent, _namesAsContent);
            if (EditorGUI.EndChangeCheck()) {
                OnNewValueSelected(property, index);
            }
        }

        void OnNewValueSelected(SerializedProperty property, int index) {
            if (index >= _possiblePaths.Length) {
                // REFRESH has been clicked
                s_actorRegister = null;
                s_prefab = null;
                _possiblePaths = null;
            } else {
                AssignProperGUIDToProperty(property, _possiblePaths[index]);
            }
        }

        void DrawInvalidActor(Rect position, SerializedProperty guidProp) {
            PropertyDrawerRects rect = position;
            var oldColor = GUI.color;
            GUI.color = Color.red;

            var propertyLabel = s_actorRegister.Editor_GetPathFromGUID(guidProp.stringValue);
            var notAllowedMessage = $"{propertyLabel} is not allowed here";
            var content = new GUIContent(notAllowedMessage, notAllowedMessage);
            var deleteSize = EditorStyles.miniButton.CalcSize(s_xContent);

            GUI.Label(rect.AllocateWithRest(deleteSize.x), content);
            GUI.color = oldColor;
            if (GUI.Button((Rect)rect, s_xContent)) {
                AssignProperGUIDToProperty(guidProp, _possiblePaths[0]);
            }
        }

        bool ExtractPrefab(SerializedProperty prop) {
            if (s_prefab == null) {
                s_prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ActorsRegister.Path);
                s_possiblePaths = GetPossiblePaths(s_prefab?.transform).ToArray();
                s_possiblePathsOnlyWithStates = s_possiblePaths
                    .Where(p => ActorStateRefPropertyDrawer.GetPossibleStates(p, s_prefab).Any() || p == DefinedActor.None.ActorPath)
                    .ToArray();
            }
            
            s_actorRegister = s_prefab == null ? null : s_prefab.GetComponent<ActorsRegister>();

            if (_possiblePaths == null) {
                FilterPaths(prop, true, false);
            }

            return s_prefab != null;
        }

        void RemoveHero() {
            if (_possiblePaths.Length < 1 || !ActorRefUtils.IsHeroGuid(_possiblePaths[^1])) {
                return;
            }

            Array.Resize(ref _possiblePaths, _possiblePaths.Length - 1);
            _possiblePathsAsContent[^2] = _possiblePathsAsContent[^1];
            Array.Resize(ref _possiblePathsAsContent, _possiblePathsAsContent.Length - 1);
            _namesAsContent[^2] = _namesAsContent[^1];
            Array.Resize(ref _namesAsContent, _namesAsContent.Length - 1);
        }

        void AssignProperGUIDToProperty(SerializedProperty prop, string path) {
            ActorsRegister actorsRegister = s_prefab.GetComponent<ActorsRegister>();
            string guid = actorsRegister.Editor_GetGuidFromActorPath(path);
            prop.stringValue = guid;
        }

        bool FilterPaths(SerializedProperty property, bool forceUpdate, bool appendHero) {
            int? newFilterHash = null;
            ActorRef[] filter = Array.Empty<ActorRef>();
            if (property.serializedObject.targetObject is NodeElement nodeElement) {
                var graph = nodeElement.genericParent.Graph;
                filter = graph.allowedActors;
                newFilterHash = Hashify(filter);
            }

            if (newFilterHash == _filterHash && !forceUpdate) {
                if (appendHero && property.ExtractAttribute<ActorAsListenerAttribute>() != null) {
                    return AppendHero();
                }

                return false;
            }

            _filterHash = newFilterHash;

            FillAllPossiblePaths(property);
            if (!filter.IsNullOrEmpty()) {
                _possiblePaths = _possiblePaths.Where(p =>
                        p == DefinedActor.None.ActorPath || Array.FindIndex(filter, a => p == s_actorRegister.Editor_GetPathFromGUID(a.guid)) > -1)
                    .ToArray();
            }

            GenerateGUIContentPaths(!filter.IsNullOrEmpty());
            if (appendHero && property.ExtractAttribute<ActorAsListenerAttribute>() != null) {
                return AppendHero();
            }

            return false;
        }

        void FillAllPossiblePaths(SerializedProperty prop) {
            bool showOnlyActorsWithStates = prop.serializedObject.targetObject is SEditorActorState;
            _possiblePaths = showOnlyActorsWithStates ? s_possiblePathsOnlyWithStates : s_possiblePaths;
        }

        void GenerateGUIContentPaths(bool useShortcuts) {
            GUIContent GenerateContent(string path) {
                return new(useShortcuts ? path.Split('/').Last() : path, path);
            }

            _possiblePathsAsContent = _possiblePaths.Select(GenerateContent)
                .Append(new("REFRESH"))
                .ToArray();
            
            _namesAsContent = new GUIContent[_possiblePathsAsContent.Length];
            for (int i = 0; i < _possiblePathsAsContent.Length; i++) {
                GUIContent path = _possiblePathsAsContent[i];
                int lastSlash = path.text.LastIndexOf('/');
                if (lastSlash == -1) {
                    _namesAsContent[i] = path;
                } else {
                    _namesAsContent[i] = new(path.text[(lastSlash+1)..]);
                }
            }
        }

        bool AppendHero() {
            var missingHero = Array.Find(_possiblePaths, ActorRefUtils.IsHeroGuid) == null;
            if (!missingHero || _possiblePaths.Length > 2) {
                return false;
            }

            Array.Resize(ref _possiblePaths, _possiblePaths.Length + 1);
            _possiblePaths[^1] = DefinedActor.Hero.ActorPath;
            Array.Resize(ref _possiblePathsAsContent, _possiblePathsAsContent.Length + 1);
            _possiblePathsAsContent[^1] = _possiblePathsAsContent[^2];
            _possiblePathsAsContent[^2] = new(DefinedActor.Hero.ActorPath);
            Array.Resize(ref _namesAsContent, _namesAsContent.Length + 1);
            _namesAsContent[^1] = _namesAsContent[^2];
            _namesAsContent[^2] = new(DefinedActor.Hero.ActorName);
            return true;
        }

        IEnumerable<string> GetPossiblePaths(Transform parent, string path = "") {
            if (parent == null) {
                yield break;
            }

            // defined actors first
            if (string.IsNullOrWhiteSpace(path)) {
                foreach (var definedActor in RichEnum.AllValuesOfType<DefinedActor>()) {
                    yield return definedActor.ActorPath;
                }
            }

            // sorting
            List<Transform> children = new();
            foreach (Transform child in parent) {
                children.Add(child);
            }

            children.Sort((a, b) => String.Compare(a.name, b.name, StringComparison.Ordinal));

            // enumerate children
            foreach (Transform child in children) {
                string relativePath = $"{path}{child.name}";
                if (child.GetComponent<ActorSpec>() != null) {
                    yield return relativePath;
                }

                foreach (string newPath in GetPossiblePaths(child, $"{relativePath}/")) {
                    yield return newPath;
                }
            }
        }

        // ReSharper disable once IdentifierTypo
        static int Hashify(ActorRef[] actorRefs) {
            var hashCode = actorRefs.Length;
            foreach (ActorRef actorRef in actorRefs) {
                hashCode = (hashCode * 397) ^ (actorRef.guid?.GetHashCode() ?? 0);
            }

            return hashCode;
        }

        public void Reset() {
            s_actorRegister = null;
            s_prefab = null;
            _possiblePaths = null;
        }
    }
}