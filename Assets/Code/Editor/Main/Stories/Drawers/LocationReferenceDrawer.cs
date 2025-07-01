using System.Reflection;
using Awaken.TG.Editor.Helpers;
using Awaken.TG.Editor.Helpers.Tags;
using Awaken.TG.Editor.SceneCaches.Locations;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Editor.Utility.StoryGraphs;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility.Attributes.List;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using XNode;

namespace Awaken.TG.Editor.Main.Stories.Drawers {
    // [CustomPropertyDrawer(typeof(LocationReference))]
    // public class LocationReferenceDrawer : PropertyDrawer {
    //     public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
    //         var targetTypesProp = property.FindPropertyRelative("targetTypes");
    //         EditorGUILayout.BeginHorizontal();
    //         GUIUtils.PushFieldWidth(130);
    //         EditorGUILayout.PropertyField(targetTypesProp, new GUIContent("Target Type:"), true);
    //         GUIUtils.PopFieldWidth();
    //         var targetType = (TargetType) targetTypesProp.GetPropertyValue();
    //         if (GUILayout.Button("Find", GUILayout.Width(42))) {
    //             LocationSearchWindow.OpenWindowOn(property.GetPropertyValue() as LocationReference);
    //         }
    //
    //         EditorGUILayout.EndHorizontal();
    //         
    //         if (targetType is TargetType.Tags or TargetType.AnyTag) {
    //             var tags = property.FindPropertyRelative("tags");
    //             int nodeWidth = property.serializedObject.targetObject switch {
    //                 Node node => NodeGUIUtil.GetNodeWidth(node),
    //                 NodeElement element => NodeGUIUtil.GetNodeWidth(element.genericParent),
    //                 _ => 200
    //             };
    //             DrawTags(tags, tags.FieldInfo(), nodeWidth);
    //             return;
    //         }
    //         foreach (var prop in property.GetChildren()) {
    //             if (prop.name == "targetTypes") continue;
    //             EditorGUILayout.PropertyField(prop, true);
    //         }
    //         // if (targetType == TargetType.Templates) {
    //         //     EditorGUILayout.PropertyField(property.FindPropertyRelative("locationRefs"), new GUIContent("Templates:"), true);
    //         // } else if (targetType == TargetType.Actor) {
    //         //     var actorProperty = property.FindPropertyRelative("actors");
    //         //     ListEditing.Show(actorProperty, ListEditOption.FewButtons);
    //         // } else if (targetType == TargetType.UnityReferences) {
    //         //     
    //         // } else {
    //         //     EditorGUILayout.LabelField("Not implemented");
    //         // }
    //     }
    //
    //     public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => 0;
    //
    //     static void DrawTags(SerializedProperty serializedProperty, FieldInfo field, int nodeWidth) {
    //         // header
    //         HeaderAttribute headerAttr = AttributesCache.GetCustomAttribute<HeaderAttribute>(field);
    //         if (headerAttr != null) {
    //             EditorGUILayout.LabelField(headerAttr.header, EditorStyles.boldLabel);
    //         }
    //         TagsEditing.Show(serializedProperty, serializedProperty.ExtractAttribute<TagsAttribute>().tagsCategory, nodeWidth);
    //     }
    // }
}