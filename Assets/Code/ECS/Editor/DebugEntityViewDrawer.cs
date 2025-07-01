using Awaken.ECS.Authoring;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Awaken.ECS.Editor {
    [CustomPropertyDrawer(typeof(DebugEntityView))]
    public class DebugEntityViewDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            var indexProperty = property.FindPropertyRelative("index");
            var versionProperty = property.FindPropertyRelative("version");

            var entityRect = new Rect(position.x, position.y, position.width - 50, position.height);
            var buttonRect = new Rect(position.x + entityRect.width + 1, position.y, 48, position.height);

            var entity = new Entity {
                Index = indexProperty.intValue,
                Version = versionProperty.intValue
            };
            string entityName = World.DefaultGameObjectInjectionWorld.EntityManager.GetName(entity);

            EditorGUI.LabelField(entityRect, entityName);
            var oldEnabled = GUI.enabled;
            GUI.enabled = World.DefaultGameObjectInjectionWorld.EntityManager.Exists(entity);
            if (GUI.Button(buttonRect, "Select")) {
                EntitySelectionProxyUtility.SelectEntity(World.DefaultGameObjectInjectionWorld, entity);
            }
            GUI.enabled = oldEnabled;

            EditorGUI.EndProperty();
        }
    }
}
