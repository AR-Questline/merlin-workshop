using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Editor.Utility.StoryGraphs;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Memories.Journal;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Reflections;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector.Editor;
using TMPro;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.PropertyVariants;
using UnityEngine.Localization.PropertyVariants.TrackedProperties;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using XNode;
using LogType = Awaken.Utility.Debugging.LogType;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Localizations {
    public static class LocalizationUtils {
        public const string TemplateCategory = "Template";

        // === Cache
        static OnDemandCache<Type, FieldInfo[]> s_typeFields = new(TypeFieldsFactory);

        public static void RenameTerm(NodeGraph graph, string fieldName, LocString loc, Object o) {
            TableEntry tableEntry = graph.StringTable.GetEntry(loc.ID);
            var tableCollection = LocalizationEditorSettings.GetStringTableCollection(graph.StringTable.TableCollectionName);
            // Update termData with new ID
            var term = $"{graph.LocalizationPrefix}/{fieldName}_{RetrieveFileId(o)}_{RetrieveFileGuid(graph)}";
            if (tableEntry != null && !string.IsNullOrWhiteSpace(tableEntry.LocalizedValue)) {
                tableCollection.SharedData.RenameKey(tableEntry.Key, term);
                EditorUtility.SetDirty(graph.StringTable);
                EditorUtility.SetDirty(tableCollection);
                EditorUtility.SetDirty(tableCollection.SharedData);
            }
            loc.ID = term;
        }

        public static void AssignNewTerm(GameObjectLocalizer localize, string newTermID, string oldTermId, StringTableCollection tableCollection) {
            var tmp = localize.GetComponent<TextMeshProUGUI>();
            var stringTable = (StringTable)tableCollection.GetTable(LocalizationSettings.ProjectLocale.Identifier);
            if (tmp != null) {
                if (tableCollection.SharedData.Contains(oldTermId)) {
                    tableCollection.SharedData.RenameKey(oldTermId, newTermID);
                } else {
                    stringTable.AddEntry(newTermID, tmp.text);
                }
                EditorUtility.SetDirty(stringTable);
                EditorUtility.SetDirty(tableCollection);
                EditorUtility.SetDirty(tableCollection.SharedData);
            }
        }

        public static void CopyTermDataToNewId(NodeGraph graph, string fieldName, LocString loc, Object o, StringTableEntry originalTableEntry = null, bool assetsSave = true) {
            TableEntry tableEntry = originalTableEntry ?? graph.StringTable.GetEntry(loc.ID);
            if (tableEntry == null) {
                return;
            }
            // Add new term and fill with old one data
            var term = $"{graph.LocalizationPrefix}/{fieldName}_{RetrieveFileId(o)}_{RetrieveFileGuid(graph)}";
            string value = tableEntry.LocalizedValue;
            var tableCollection = LocalizationEditorSettings.GetStringTableCollection(graph.StringTable.TableCollectionName);
            graph.StringTable.AddEntry(term, value);
            EditorUtility.SetDirty(graph.StringTable);
            EditorUtility.SetDirty(tableCollection);
            EditorUtility.SetDirty(tableCollection.SharedData);
            loc.ID = term;
        }

        public static void ApplyTableEntryToLocalizationTable(StringTable table, StringTableEntry originalTableEntry, LocString loc) {
            // Add new term and fill with old one data
            var term = originalTableEntry.Key;
            table.AddEntry(term, originalTableEntry.LocalizedValue);
            var tableCollection = LocalizationEditorSettings.GetStringTableCollection(table.TableCollectionName);
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(tableCollection);
            EditorUtility.SetDirty(tableCollection.SharedData);
            loc.ID = term;
        }

        public static string RetrieveFileId(Object o) {
            if (o is NodeElement || o is Node) {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out string _, out long localId);
                return localId.ToString();
            }
            return GlobalObjectId.GetGlobalObjectIdSlow(o).targetObjectId.ToString();
        }

        public static string RetrieveFileGuid(Object o) {
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage == null) {
                return RetrieveFileGuidNotInPrefabMode(o);
            } else {
                return RetrieveFileGuidInPrefabMode(o, prefabStage);
            }
        }

        static string RetrieveFileGuidNotInPrefabMode(Object o) {
            GameObject go = o is Component component ? component.gameObject : o as GameObject;
            // we are not in prefab mode
            if (go != null && !string.IsNullOrWhiteSpace(go.scene.name)) {
                // this is a GameObject and it's edited on scene
                if (PrefabUtility.IsPartOfAnyPrefab(go)) {
                    // but it's part of the prefab, so we use prefab for guid
                    string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                    Object prefab = AssetDatabase.LoadAssetAtPath<Object>(prefabPath);
                    return RetrieveFileGuidDirect(prefab);
                } else {
                    // it's not a prefab so it doesn't have GUID, let's use scene name for it's guid
                    return go.scene.name;
                }
            } else {
                return RetrieveFileGuidDirect(o);
            }
        }

        static string RetrieveFileGuidInPrefabMode(Object o, PrefabStage prefabStage) {
            GameObject go = o is Component component ? component.gameObject : o as GameObject;
            if (prefabStage.IsPartOfPrefabContents(go)) {
                // now we are sure we are editing prefab in prefab mode
                Object prefab = AssetDatabase.LoadAssetAtPath<Object>(prefabStage.assetPath);
                return RetrieveFileGuidDirect(prefab);
            } else {
                // prefab mode is opened, but we actually edit something outside of it
                return RetrieveFileGuidDirect(o);
            }
        }

        static string RetrieveFileGuidDirect(Object o) {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out string guid, out long _);
            return guid;
        }

        public static StringTableCollection DetermineStringTable(object o, bool allowFallback = true) {
            if (o is SerializedProperty serializedProperty) {
                return DetermineStringTable(serializedProperty, allowFallback);
            } else /*if (o is InspectorProperty inspectorProperty) {
                return DetermineStringTable(inspectorProperty, allowFallback);
            } else */{
                Log.Important?.Error($"Failed to determine string table! Unknown object type passe: {o}");
                return null;
            }
        }
        
        static StringTableCollection DetermineStringTable(SerializedProperty property, bool allowFallback) {
            if (property.serializedObject.targetObject is NodeElement ne) {
                return LocalizationEditorSettings.GetStringTableCollection(ne.genericParent.Graph.SourcePath);
            }
            
            if (property.serializedObject.targetObject is Node n) {
                return LocalizationEditorSettings.GetStringTableCollection(n.graph.SourcePath);
            }
            
            if (property.serializedObject.targetObject is NodeGraph ng) {
                return LocalizationEditorSettings.GetStringTableCollection(ng.SourcePath);
            }

            // default source
            return allowFallback ? LocalizationTools.PrefabCollection : null;
        }
        
        // static StringTableCollection DetermineStringTable(InspectorProperty property, bool allowFallback) {
        //     if (property.Tree.UnitySerializedObject.targetObject is NodeElement ne) {
        //         return LocalizationEditorSettings.GetStringTableCollection(ne.genericParent.Graph.SourcePath);
        //     }
        //     
        //     if (property.Tree.UnitySerializedObject.targetObject is Node n) {
        //         return LocalizationEditorSettings.GetStringTableCollection(n.graph.SourcePath);
        //     }
        //     
        //     if (property.Tree.UnitySerializedObject.targetObject is NodeGraph ng) {
        //         return LocalizationEditorSettings.GetStringTableCollection(ng.SourcePath);
        //     }
        //
        //     // default source
        //     return allowFallback ? LocalizationTools.PrefabCollection : null;
        // }
        
        public static void ChangeTextTranslation(string id, string value, StringTable stringTable, bool autoCheckSmart = false) {
            var tableCollection = LocalizationEditorSettings.GetStringTableCollection(stringTable.TableCollectionName);
            StringTableEntry entry = stringTable.AddEntry(id, value);
            if (autoCheckSmart) {
                bool shouldBeSmart = IsSmart(value);
                if (shouldBeSmart != entry.IsSmart) {
                    entry.IsSmart = shouldBeSmart;
                }
            }
            EditorUtility.SetDirty(stringTable);
            EditorUtility.SetDirty(tableCollection);
            EditorUtility.SetDirty(tableCollection.SharedData);
        }
        
        static readonly Regex SmartRegex = new(@"{global\.[^}]+}", RegexOptions.Compiled);

        public static bool IsSmart(string text) {
            return SmartRegex.IsMatch(text);
        }

        public static string RetrieveTag(string fieldName, object parentObject) {
            const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            const BindingFlags PropertyFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.NonPublic;

            var parentType = parentObject.GetType();
            var memberInfo = parentType.GetField(fieldName, FieldFlags) ??
                             parentType.GetProperty(fieldName, PropertyFlags) as MemberInfo;
            var memberValue = memberInfo?.MemberValue(parentObject) 
                              ?? parentType.GetMethod(fieldName, FieldFlags)?.MemberValue(parentObject);

            if (memberValue is string s) {
                return s;
            }
            if (memberValue is NodeGraph g) {
                return g.name;
            }

            return fieldName;
        }

        static LocalizedField GetLocalizedField(object o) {
            FieldInfo fieldInfo = null;
            object fieldValue = null;
            string propertyPath = string.Empty;
            if (o is SerializedProperty serializedProperty) {
                fieldInfo = serializedProperty.FieldInfo();
                fieldValue = serializedProperty.GetPropertyValue();
                propertyPath = serializedProperty.SerializedPropertyPath();
            } else /*if (o is InspectorProperty inspectorProperty) {
                fieldInfo = inspectorProperty.FieldInfo();
                fieldValue = inspectorProperty.ValueEntry.WeakSmartValue;
                propertyPath = inspectorProperty.SerializedPropertyPath();
            }*/
            
            if (fieldValue is LocString loc) {
                return new LocalizedField(propertyPath, loc, fieldInfo);
            }
            return null;
        }
        
        static string SerializedPropertyPath(this SerializedProperty property) => SerializedPropertyPath(property.name, property.propertyPath);
        // static string SerializedPropertyPath(this InspectorProperty property) {
        //     string name = property.Name;
        //     if (property.ParentValueProperty.ValueEntry.WeakSmartValue is IEnumerable) {
        //         name = property.Parent.Name;
        //     }
        //     return LocalizationUtils.SerializedPropertyPath(name, property.UnityPropertyPath);
        // } 
        
        public static string SerializedPropertyPath(string name, string propertyPath) {
            if (name.Equals("data")) {
                string path = propertyPath;
                int index = path.LastIndexOf("data", StringComparison.InvariantCulture);
                var subString = path.Substring(index, path.Length - index);
                var dataIndex = Regex.Match(subString, @"\[([^]]*)\]").Groups[1].Value;
                path = path.Substring(0, index);
                path += dataIndex;
                return path.Replace('.', '_').Replace("[", string.Empty).Replace("]", string.Empty);
            }
            return propertyPath.Replace('.', '_').Replace("[", string.Empty).Replace("]", string.Empty);
        }

        public static IReadOnlyCollection<LocalizedField> GetLocalizedProperties(Object o, bool allowRecurrence = true) {
            s_objectsSet.Clear();
            var list = GetLocalizedFields(o, s_objectsSet, allowRecurrence: allowRecurrence).ToList();
            return list;
        }

        static HashSet<object> s_objectsSet = new();
        static IEnumerable<LocalizedField> GetLocalizedFields(object o, HashSet<object> objectsDone, string prePath = "", bool allowRecurrence = true) {
            if (o is StoryGraph or StoryNode) {
                objectsDone.Add(o);
            }
            if (o != null && !objectsDone.Contains(o)) {
                objectsDone.Add(o);
                Type type = o.GetType();
                FieldInfo[] fields = s_typeFields[type];
                foreach (var f in fields) {
                    object fieldValue = f.GetValue(o);
                    var fieldType = f.FieldType;

                    if (fieldValue == null) {
                        continue;
                    }
                    // === Check if fieldValue is unassigned
                    try {
                        if (fieldValue is Object obj) {
                            _ = obj.name;
                        }
                    } catch (Exception) {
                        continue;
                    }

                    if (fieldValue is LocString loc) {
                        yield return new LocalizedField($"{prePath}{f.Name}", loc, f);
                    }

                    if (fieldValue is IEnumerable<LocString> listOfStrings) {
                        var list = listOfStrings.ToList();
                        for (int i = 0; i < list.Count; i++) {
                            yield return new LocalizedField($"{prePath}{f.Name}_{i}", list[i], f);
                        }
                    } else if (allowRecurrence && fieldValue is IEnumerable l and not string) {
                        var tmpType = l.GetType();
                        if (tmpType.IsGenericType) {
                            var genericTypeDef = tmpType.GetGenericTypeDefinition();
                            if (genericTypeDef == typeof(NativeList<>) || genericTypeDef == typeof(UnsafeList<>)) {
                                continue;
                            }
                        }

                        foreach (var listElement in l) {
                            foreach (var localizedField in GetLocalizedFields(listElement, objectsDone, $"{prePath}{f.Name}_")) {
                                if (localizedField != null) {
                                    yield return localizedField;
                                }
                            }
                        }
                    }

                    if (!fieldType.IsEnum && !fieldType.IsPrimitive && fieldType != typeof(string)) {
                        foreach (var localizedField in GetLocalizedFields(fieldValue, objectsDone, $"{prePath}{f.Name}_", allowRecurrence)) {
                            if (localizedField != null) {
                                yield return localizedField;
                            }
                        }
                    }
                }
            }
        }

        public static void RemoveAllLocalizedTerms(Object o, StringTable stringTable) {
            var properties = GetLocalizedProperties(o, false);
            foreach (var property in properties) {
                RemoveTableEntry(property.LocProperty.ID, stringTable);
            }
        }

        public static void RemoveTableEntry(string id, StringTable stringTable) {
            if (string.IsNullOrWhiteSpace(id)) {
                return;
            }
            
            StringTableCollection tableCollection = LocalizationEditorSettings.GetStringTableCollection(stringTable.TableCollectionName);
            // remove terms
            stringTable.RemoveEntry(id);
            tableCollection.RemoveEntry(id);
            
            // set all tables dirty
            var tablesToSetDirty = new Object[tableCollection.Tables.Count + 1];
            for (int i = 0; i < tableCollection.Tables.Count; ++i) {
                tablesToSetDirty[i] = tableCollection.Tables[i].asset;
            }
            tablesToSetDirty[tableCollection.Tables.Count] = tableCollection.SharedData;
            foreach (var table in tablesToSetDirty) {
                EditorUtility.SetDirty(table);
            }
        }

        public static void MoveEntry(SharedTableData.SharedTableEntry entry, StringTableCollection from, StringTableCollection to) {
            if (!from.SharedData.Contains(entry.Id)) {
                return;
            }
            var newEntry = to.SharedData.AddKey(entry.Key);
            foreach (var table in from.StringTables) {
                var localized = table[entry.Key];
                if (localized != null) {
                    var newTable = to.StringTables.First(t => t.LocaleIdentifier == table.LocaleIdentifier);
                    newTable.AddEntry(newEntry.Key, localized.Value);
                }
            }
            from.RemoveEntry(entry.Id);
        }
        
        static readonly OnDemandCache<(Object, FieldInfo), List<LocalizedField>> ValidateCache = new(pair =>
            GetLocalizedProperties(pair.Item1, false).Where(f => f.FieldInfo == pair.Item2).ToList());

        public static bool ValidateTerm(SerializedProperty property, string category, out string newTerm, FieldInfo fieldInfo = null) {
            if (fieldInfo == null) {
                fieldInfo = property.FieldInfoArrayAware();
            }

            if (ValidateCache.Count > 300) {
                ValidateCache.Clear();
            }
            List<LocalizedField> locFields = ValidateCache[(property.serializedObject.targetObject, fieldInfo)];
            foreach (var loc in locFields) {
                if (loc != null) {
                    string fileId = RetrieveFileId(property.serializedObject.targetObject);
                    string guid = RetrieveFileGuid(property.serializedObject.targetObject);
                    string term = $"{category}/{loc.FieldPath}_{fileId}_{guid}";
                    if (term != loc.LocProperty.ID && !Application.isPlaying) {
                        property.FindPropertyRelative("ID").stringValue = term;
                        newTerm = term;
                        return true;
                    }
                }
            }
            newTerm = null;
            return false;
        }

        public static bool InspectorValidateTerm(object property, string category, out string newTerm) {
            LocalizedField locField = GetLocalizedField(property);
            var targetObject = property switch {
                SerializedProperty p => p.serializedObject.targetObject,
                // InspectorProperty i => i.Tree.UnitySerializedObject.targetObject,
                _ => null
            };
            if (ValidatePropertyTerm(locField, targetObject, category, out newTerm)) {
                if (property is SerializedProperty serializedProperty) {
                    serializedProperty.FindPropertyRelative("ID").stringValue = newTerm;
                } /*else if (property is InspectorProperty inspectorProperty) {
                    inspectorProperty.FindChild(p => p.Name == "ID", false).BaseValueEntry.WeakSmartValue = newTerm;
                }*/
                return true;
            }
            return false;
        }
        
        static bool ValidatePropertyTerm(LocalizedField locField, Object targetObject, string category, out string newTerm) {
            newTerm = null;
            if (locField != null) {
                string fileId = RetrieveFileId(targetObject);
                if (fileId == "0") return false;
                
                string guid = RetrieveFileGuid(targetObject);
                newTerm = $"{category}/{locField.FieldPath}_{fileId}_{guid}";
                if (newTerm != locField.LocProperty.ID && !Application.isPlaying) {
                    return true;
                }
            }
            return false;
        }

        public static string DetermineCategory(SerializedProperty property) {
            NodeGraph graph = NodeGUIUtil.Graph(property);
            if (graph != null) {
                return graph.LocalizationPrefix;
            }
            return TemplateCategory;
        }
        

        public static void ValidateViewTerms(string prefix, string prefabPath, string prefabGUID) {
            GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            bool fixedAny = ValidateTerms(prefix, prefabInstance, prefabGUID);
            if (fixedAny) {
                PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
            }
            PrefabUtility.UnloadPrefabContents(prefabInstance);
        }
        
        public static void ValidateViewTerms(string prefix, GameObject view, string prefabGUID = null) {
            PrefabStage currentPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (PrefabUtil.IsInPrefabStage(view, currentPrefabStage)) {
                bool proceed = EditorUtility.DisplayDialog(
                    "Terms validation",
                    "This operation will save all changes made to this prefab. Are you sure you want proceed?",
                    "Yes",
                    "Give me time to check changes");

                if (!proceed) {
                    return;
                }

                GameObject prefabInstance = currentPrefabStage.prefabContentsRoot;
                bool fixedAny = ValidateTerms(prefix, prefabInstance, prefabGUID);

                if (fixedAny) {
                    PrefabUtility.SaveAsPrefabAsset(currentPrefabStage.prefabContentsRoot, currentPrefabStage.assetPath);
                    currentPrefabStage.ClearDirtiness();
                }
            } else {
                string prefabPath = AssetDatabase.GetAssetPath(view);
                GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);
                bool fixedAny = ValidateTerms(prefix, prefabInstance, prefabGUID);
                if (fixedAny) {
                    PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
                }
                PrefabUtility.UnloadPrefabContents(prefabInstance);
            }
        }

        static bool ValidateTerms(string prefix, GameObject prefabInstance, string prefabGUID = null) {
            prefabGUID ??= LocalizationUtils.RetrieveFileGuid(prefabInstance);

            GameObjectLocalizer[] localizes = prefabInstance.GetComponentsInChildren<GameObjectLocalizer>(true);

            bool fixedAny = false;
            foreach (var localize in localizes) {
                var objectId = LocalizationUtils.RetrieveFileId(localize);
                var term = $"{prefix}/{objectId}_{prefabGUID}";

                var tmp = localize.GetComponent<TextMeshProUGUI>();
                if (tmp == null) {
                    continue;
                }
                var trackedText = localize.GetTrackedObject(tmp);
                foreach (var prop in trackedText.TrackedProperties) {
                    if (prop is LocalizedStringProperty stringProp) {
                        var collection = LocalizationEditorSettings.GetStringTableCollection(stringProp.LocalizedString.TableReference);
                        string oldKey = collection.SharedData.GetEntryFromReference(stringProp.LocalizedString.TableEntryReference).Key;
                        if (oldKey == term) {
                            continue;
                        }
                        LocalizationUtils.AssignNewTerm(localize, term, oldKey, collection);
                        fixedAny = true;
                    }
                }
            }

            View view = prefabInstance.GetComponent<View>();
            ViewComponent viewComponent = prefabInstance.GetComponent<ViewComponent>();
            if (view != null) {
                view.LocID = prefix;
            }

            if (viewComponent != null) {
                viewComponent.LocID = prefix;
            }
            
            return fixedAny;
        }
        
        // === Helpers
        static FieldInfo[] TypeFieldsFactory(Type type) {
            return type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }

    public class LocalizedField {
        public string FieldPath { get; }
        public LocString LocProperty { get; }
        public FieldInfo FieldInfo { get; }

        public LocalizedField(string fieldPath, LocString locProperty, FieldInfo fieldInfo) {
            FieldPath = fieldPath;
            LocProperty = locProperty;
            FieldInfo = fieldInfo;
        }
    }
}
