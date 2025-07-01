using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Editor.Assets.Templates;
using Awaken.TG.Editor.Helpers;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Main.Templates;
using Awaken.Utility.UI;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Assets {
    [CustomPropertyDrawer(typeof(ARAssetReference))]
    public class ARAssetReferencePropertyDrawerInvalid : PropertyDrawer {
        public override void OnGUI(Rect propertyDimensions, SerializedProperty property, GUIContent label) {
            PropertyDrawerRects rect = propertyDimensions;
            EditorGUI.LabelField(rect.AllocateTop((int)EditorGUIUtility.singleLineHeight), "Can not find required ARAssetReferenceSettingsAttribute");

            var arReference = ARAssetReferencePropertyDrawer.ExtractARAssetReference(property);
            AddressableAssetEntry entry = AddressableHelper.GetEntry(arReference);
            if (entry?.IsSubAsset ?? false) {
                entry = entry.ParentEntry;
            }
            string labelText = entry?.address;
            if (!string.IsNullOrWhiteSpace(arReference?.SubObjectName)) {
                labelText += " | " + arReference.SubObjectName;
            }

            if (string.IsNullOrWhiteSpace(labelText)) {
                labelText = "Null";
            }

            bool oldValue = GUI.enabled;
            GUI.enabled = oldValue && entry?.MainAsset != null;
            if (GUI.Button((Rect) rect, labelText)) {
                EditorGUIUtility.PingObject(entry.MainAsset);
            }
            GUI.enabled = oldValue;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUIUtility.singleLineHeight * 2;
        }
    }
    
    [CustomPropertyDrawer(typeof(ARAssetReferenceSettingsAttribute), true)]
    public class ARAssetReferencePropertyDrawer : PropertyDrawer {
        static readonly GUIContent PingContent = new GUIContent("Ping");
        static readonly GUIContent ClearContent = new GUIContent("X");

        // === PropertyDrawer
        public override void OnGUI(Rect propertyDimensions, SerializedProperty property, GUIContent label) {
            PropertyDrawerRects rect = propertyDimensions;

            if (!(attribute is ARAssetReferenceSettingsAttribute)) {
                PropertyDrawerRects headerRow = rect.AllocateTop((int) EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField((Rect)headerRow, "Can not find required ARAssetReferenceSettingsAttribute");
                return;
            }

            EditorGUI.BeginProperty(propertyDimensions, label, property);
            Draw(propertyDimensions, property, attribute as ARAssetReferenceSettingsAttribute);
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUIUtility.singleLineHeight * 2;
        }
        
        // === Operation
        
        public static void Draw(Rect propertyDimensions, SerializedProperty property, ARAssetReferenceSettingsAttribute settingsAttribute) {
            PropertyDrawerRects rect = propertyDimensions;

            ARAssetReference arReference = ExtractARAssetReference(property);
            if (arReference == null) {
                PropertyDrawerRects headerRow = rect.AllocateTop((int) EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField((Rect)headerRow, $"Can not use {nameof(ARAssetReferenceSettingsAttribute)} on filed of type {property.GetPropertyType()}");
                return;
            }

            DrawHeaderRow(property, ref rect, arReference);

            // -- Dropdown
            DrawDropdown(property, propertyDimensions.width, ref rect, arReference, settingsAttribute);

            ManageDragAndDrop(property, propertyDimensions, arReference, settingsAttribute);
        }
        
        static void OnAssetChange(ARAssetReference arReference, string address, string subObject, ARAssetReferenceSettingsAttribute settings) {
            AddressableAssetEntry entry = AddressableHelper.GetEntry(address, subObject);

            bool isValid = AssetFilters(settings).All(f => f.Invoke(entry));
            if (!isValid) {
                return;
            }

            // Ensure entry properties

            Func<Object, AddressableAssetEntry, string, string> groupProvider = null;
            if (!string.IsNullOrWhiteSpace(settings.GroupName)) {
                groupProvider = (_0, _1, _2) => settings.GroupName;
            }

            Func<Object, AddressableAssetEntry, string> addressProvider = null;
            if (settings.NameProvider != null) {
                addressProvider = (o, _) => settings.NameProvider(o);
            }

            if (string.IsNullOrWhiteSpace(entry.guid)) {
                AddressableHelper.EnsureAsset(entry.ParentEntry.guid, groupProvider, addressProvider);
            } else {
                AddressableHelper.EnsureAsset(address, subObject, groupProvider, addressProvider);
            }

            arReference.EditorSetValues(address, subObject);
            
            AddressableHelperEditor.CheckEntries();
            GUI.changed = true;
        }

        // === Drawing
        static void DrawHeaderRow(SerializedProperty property, ref PropertyDrawerRects rect, ARAssetReference arReference) {
            PropertyDrawerRects headerRow = rect.AllocateTop((int) EditorGUIUtility.singleLineHeight);
            int labelWidth = Mathf.FloorToInt(((Rect) headerRow).width * 0.65f);
            int pingWidth = Mathf.FloorToInt(((Rect) headerRow).width * 0.25f);
            int clearWidth = Mathf.FloorToInt(((Rect) headerRow).width * 0.1f);
            EditorGUI.LabelField(headerRow.AllocateLeft(labelWidth), property.displayName);
            var oldEnabled = GUI.enabled;
            GUI.enabled = oldEnabled && (arReference?.IsSet ?? false);
            if (GUI.Button(headerRow.AllocateLeft(pingWidth), PingContent)) {
                EditorGUIUtility.PingObject(AddressableHelper.FindFirstEntry<Object>(arReference));
            }
            if (GUI.Button(headerRow.AllocateLeft(clearWidth), ClearContent)) {
                if (arReference != null) {
                    arReference.EditorSetValues(null,null);
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                    property.serializedObject.Update();
                    property.serializedObject.ApplyModifiedProperties();
                }
            }

            GUI.enabled = oldEnabled;
        }

        static void DrawDropdown(SerializedProperty property, float propertyWidth, ref PropertyDrawerRects rect, 
            ARAssetReference arReference, ARAssetReferenceSettingsAttribute settings) {
            var dropdownButtonRect = EditorGUI.IndentedRect((Rect) rect);
            
            AddressableAssetEntry entry = AddressableHelper.GetEntry(arReference);
            if (entry?.IsSubAsset ?? false) {
                entry = entry.ParentEntry;
            }
            string labelText = entry?.address;
            if (!string.IsNullOrWhiteSpace(arReference?.SubObjectName)) {
                labelText += " | " + arReference.SubObjectName;
            }

            if (string.IsNullOrWhiteSpace(labelText)) {
                labelText = "Null";
            }
            
            if (EditorGUI.DropdownButton(dropdownButtonRect, new GUIContent( labelText ), FocusType.Keyboard)) {
                var popupPosition = new Vector2(dropdownButtonRect.x, dropdownButtonRect.y);
                popupPosition.y += EditorGUIUtility.singleLineHeight;
                ARAssetReferenceSearcherPopup.Show(
                    new Rect(GUIUtility.GUIToScreenPoint(popupPosition), new Vector2(propertyWidth, 200)),
                    (address, subAsset) => {
                        property.serializedObject.ApplyModifiedProperties();
                        OnAssetChange(arReference, address, subAsset, settings);
                        EditorUtility.SetDirty(property.serializedObject.targetObject);
                        property.serializedObject.Update();
                        property.serializedObject.ApplyModifiedProperties();
                    },
                    GroupFilters(settings),
                    AssetFilters(settings)
                );
            }
        }

        // === Filtering
        static IEnumerable<Func<AddressableAssetGroup, bool>> GroupFilters(ARAssetReferenceSettingsAttribute settings) {
            if (!string.IsNullOrWhiteSpace(settings.GroupName)) {
                yield return g => g.Name == settings.GroupName;
            } else {
                yield return _ => true;
            }
        }

        static IEnumerable<Func<AddressableAssetEntry, bool>> AssetFilters(ARAssetReferenceSettingsAttribute settings) {
            yield return e => IsValidAssetType(settings, e.TargetAsset);
        }

        // === Drag&Drop
        static void ManageDragAndDrop(SerializedProperty property, Rect position, ARAssetReference arReference, ARAssetReferenceSettingsAttribute settings) {
            if (Event.current.type == EventType.DragUpdated && position.Contains(Event.current.mousePosition)) {
                DragAndDrop.visualMode = CanAcceptDrag(settings) ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;
                Event.current.Use();
            } else if (Event.current.type == EventType.DragPerform && position.Contains(Event.current.mousePosition)) {
                if (CanAcceptDrag(settings)) {
                    Object objectToAdd = DragAndDrop.objectReferences[0];
                    AssignAsset(property, arReference, objectToAdd, settings);
                    DragAndDrop.AcceptDrag();
                }
            }
        }

        static bool CanAcceptDrag(ARAssetReferenceSettingsAttribute settings) {
            bool onlyOneAsset = DragAndDrop.objectReferences.Length == 1;
            return onlyOneAsset && IsValidAssetType(settings, DragAndDrop.objectReferences[0]);
        }

        static bool IsValidAssetType(ARAssetReferenceSettingsAttribute settings, Object asset) {
            return settings.AssetTypes.Any(CanAcceptType);
            
            bool CanAcceptType(Type definedType) {
                // Templates should go thru TemplateReference
                if (typeof(ITemplate).IsAssignableFrom(definedType)) {
                    return false;
                }
                var isInstanceOf = definedType.IsInstanceOfType(asset);
                if (isInstanceOf) {
                    return true;
                }
                if (!typeof(Component).IsAssignableFrom(definedType) || asset is not GameObject gameObject) {
                    return false;
                }
                var component = gameObject.GetComponent(definedType);
                return component;
            }
        }

        // === Public operations
        public static ARAssetReference ExtractARAssetReference(SerializedProperty property) {
            ARAssetReference arReference;
            if (property.GetPropertyType() == typeof(ARAssetReference)) {
                arReference = (ARAssetReference) property.GetPropertyValue();
            } else {
                arReference = (ARAssetReference) property
                    .GetChildren()
                    .FirstOrDefault(c => c.GetPropertyType() == typeof(ARAssetReference))
                    ?.GetPropertyValue();
            }

            return arReference;
        }

        public static void AssignAsset(ARAssetReference arReference, Object objectToAdd, ARAssetReferenceSettingsAttribute settings) {
            GetAddressable(objectToAdd, settings, out var guid, out var subObject);
            OnAssetChange(arReference, guid, subObject, settings);
        }
        
        public static void AssignAsset(SerializedProperty property, ARAssetReference arReference, Object objectToAdd, ARAssetReferenceSettingsAttribute settings = null) {
            settings ??= property.ExtractAttribute<ARAssetReferenceSettingsAttribute>();
            GetAddressable(objectToAdd, settings, out var guid, out var subObject);
            property.serializedObject.ApplyModifiedProperties();
            OnAssetChange(arReference, guid, subObject, settings);
            EditorUtility.SetDirty(property.serializedObject.targetObject);
            property.serializedObject.Update();
            property.serializedObject.ApplyModifiedProperties();
        }
        
        static void GetAddressable(Object objectToAdd, ARAssetReferenceSettingsAttribute settings, out string guid, out string subObject) {
            var assetPath = AssetDatabase.GetAssetPath(objectToAdd);
            ITemplate template = TemplatesUtil.ObjectToTemplateUnsafe(objectToAdd);
            guid = null;
            subObject = null;
            if (objectToAdd == null) {
                EditorUtility.DisplayDialog("Invalid object", $"Can not add asset to addressables because asset is null", "OK");
                return;
            }else if (string.IsNullOrWhiteSpace(assetPath)) {
                EditorUtility.DisplayDialog("Invalid object", $"Can not add asset {objectToAdd.name} to addressables because can not obtain asset path", "OK");
                return;
            }else if ( AddressableHelperEditor.IsResourcesPath(assetPath)) {
                EditorUtility.DisplayDialog("Invalid object", $"Can not add asset {objectToAdd.name} to addressables because asset lives in Resources directory. Move asset to proper directory and try again.", "OK");
                return;
            }else if (TemplatesSearcher.IsTemplateAddressable(template, objectToAdd)) {
                EditorUtility.DisplayDialog("Invalid object", $"Can not assign template {objectToAdd.name} to asset reference field", "OK");
                return;
            }
            
            var group = settings.GroupName;

            Func<Object, AddressableAssetEntry, string> addressProvider = null;
            if (settings.NameProvider != null) {
                addressProvider = (o, _) => settings.NameProvider(o);
            }

            guid = AddressableHelper.AddEntry(
                new AddressableEntryDraft.Builder(objectToAdd)
                    .WithAddressProvider(addressProvider)
                    .InGroup(group)
                    .WithLabels(settings.Labels)
                    .Build());

            subObject = objectToAdd != AddressableHelper.GetEntry(guid, string.Empty).TargetAsset ?
                DragAndDrop.objectReferences[0].name :
                null;
        }
    }
}