using Awaken.TG.Editor.Helpers.Tags;
using Awaken.TG.Editor.Main.AI.Barks;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Utility.Attributes.Tags;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Stories.Actors {
    [CustomEditor(typeof(ActorSpec)), CanEditMultipleObjects]
    public class ActorSpecEditor : OdinEditor {
        const string TagsPropertyName = nameof(ActorSpec.tags);
        const string UseCustomBarkGraphPropertyName = nameof(ActorSpec.useCustomBarkGraph);
        const string BarkConfigPropertyName = nameof(ActorSpec.barkConfig);

        const string TagsInfo = "Barks are dialogues that can be played by the actor. " +
                                "They are defined in the BarksConfig asset.Tags are used to filter barks. " +
                                "If the actor has a tag that matches the tag of a bark, the bark will be available for the actor.";

        const string IsCustomWarning = "Checking the box allow marks the actor as using a custom bark configuration. " +
                                      "This means that the actor will not be able to use the barks defined in the BarksConfig asset. " +
                                      "This is useful if you want to create a custom bark configuration that is not intended for automatic updates.";
        
        const string SyncButtonInfo = "Syncing may take time, ensure you're done editing tags before proceeding.";

        public override void OnInspectorGUI() {
            SerializedProperty property = serializedObject.GetIterator();

            property.NextVisible(true);
            while (property.NextVisible(false)) {
                if (property.name is TagsPropertyName or UseCustomBarkGraphPropertyName or BarkConfigPropertyName) {
                    continue;
                }
                EditorGUILayout.PropertyField(property, true);
            }

            // SirenixEditorGUI.BeginBox();
            // SirenixEditorGUI.BeginBoxHeader();
            // SirenixEditorGUI.Title("Barks", null, TextAlignment.Left, true);
            // SirenixEditorGUI.EndBoxHeader();
            
            var isCustomProperty = serializedObject.FindProperty(UseCustomBarkGraphPropertyName);
            EditorGUILayout.PropertyField(isCustomProperty, true);
            if (isCustomProperty.boolValue) {
                // SirenixEditorGUI.WarningMessageBox(IsCustomWarning);
                var barkConfigProperty = serializedObject.FindProperty(BarkConfigPropertyName);
                EditorGUILayout.PropertyField(barkConfigProperty, true);
            } else {
                // SirenixEditorGUI.InfoMessageBox(TagsInfo);
                var tagsProp = serializedObject.FindProperty(TagsPropertyName);
                TagsEditing.Show(tagsProp, TagsCategory.Barks);

                // SirenixEditorGUI.WarningMessageBox(SyncButtonInfo);
                // if (SirenixEditorGUI.Button("Sync with barks config", ButtonSizes.Medium)) {
                //     BarksConfig.instance.TrySyncGraphs();
                // }
            }

            // SirenixEditorGUI.EndBox();

            serializedObject.ApplyModifiedProperties();
        }
    }
}