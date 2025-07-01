using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;

namespace UnityEditor.Localization.UI
{
    class SceneControlsWindow : EditorWindow
    {
        ProjectLocalePopupField m_ProjectLocale;
        VisualElement m_ActiveSettingsRoot;

        static class Styles
        {
            public static readonly GUIContent assetTable = EditorGUIUtility.TrTextContent("Asset Table", "The Asset Table to add new assets in to.");
            public static readonly GUIContent stringTable = EditorGUIUtility.TrTextContent("String Table", "The String Table to add new Localized Strings in to.");
            public static readonly GUIContent trackChanges = EditorGUIUtility.TrTextContent("Track Changes", "When enabled any changes made to a Components properties will be recorded for the selected Locale.");
        }

        [MenuItem("Window/Asset Management/Localization Scene Controls")]
        static void ShowWindow()
        {
            var window = GetWindow<SceneControlsWindow>();
            window.titleContent = EditorGUIUtility.TrTextContent("Localization Scene Controls");
            window.Show();
        }

        void OnEnable()
        {
            m_ActiveSettingsRoot = new VisualElement();
            rootVisualElement.Add(m_ActiveSettingsRoot);

            rootVisualElement.Add(new IMGUIContainer(() =>
            {
                if (LocalizationEditorSettings.ActiveLocalizationSettings == null)
                    EditorGUILayout.HelpBox("Project contains no Localization Settings. Please create one via `Edit/Project Settings/Localization`", MessageType.Info);
                m_ActiveSettingsRoot.style.display = LocalizationEditorSettings.ActiveLocalizationSettings == null ? DisplayStyle.None : DisplayStyle.Flex;
            }));

            var template = Resources.GetTemplateAsset(nameof(SceneControlsWindow));
            template.CloneTree(m_ActiveSettingsRoot);

            #if ENABLE_PROPERTY_VARIANTS

            LocalizationProjectSettings.instance.hideFlags = HideFlags.None;
            var serializedObject = new SerializedObject(LocalizationProjectSettings.instance);
            m_ActiveSettingsRoot.Q<IMGUIContainer>().onGUIHandler = () =>
            {
                // The asset may go null when changing scene etc.
                if (serializedObject.targetObject == null)
                {
                    LocalizationProjectSettings.instance.hideFlags = HideFlags.None;
                    serializedObject = new SerializedObject(LocalizationProjectSettings.instance);
                }
                else
                    serializedObject.Update();

                var trackChanges = serializedObject.FindProperty("m_TrackingChanges");
                EditorGUILayout.PropertyField(trackChanges, Styles.trackChanges);

                var stringTable = serializedObject.FindProperty("m_StringTable");
                EditorGUILayout.PropertyField(stringTable, Styles.stringTable, true);

                var assetTable = serializedObject.FindProperty("m_AssetTable");
                EditorGUILayout.PropertyField(assetTable, Styles.assetTable, true);

                if (serializedObject.ApplyModifiedPropertiesWithoutUndo())
                    LocalizationProjectSettings.instance.Save();
            };

            m_ProjectLocale = m_ActiveSettingsRoot.Q<ProjectLocalePopupField>();
            m_ProjectLocale.RegisterValueChangedCallback(evt =>
            {
                InternalEditorUtility.RepaintAllViews();

                // In order for UGUI components to update themselves we need to tell the component it is dirty with SetAllDirty and then force the player loop to run.
                // This method allows us to queue a player loop update regardless of whether the Scene has been marked dirty.
                EditorApplication.QueuePlayerLoopUpdate(); // Force the scene to update
            });
            #endif

            Undo.postprocessModifications += PostprocessModifications;
            Undo.undoRedoPerformed += LocalizationEditorSettings.RefreshEditorPreview;
        }

        void OnDisable()
        {
            Undo.postprocessModifications -= PostprocessModifications;
        }

        UndoPropertyModification[] PostprocessModifications(UndoPropertyModification[] modifications)
        {
            // If we detect a change to a LocalizedTable then we force a refresh to the editor preview.
            foreach (var mod in modifications)
            {
                if (mod.currentValue.target is LocalizationTable)
                {
                    EditorApplication.delayCall += LocalizationEditorSettings.RefreshEditorPreview;
                    break;
                }
            }

            return modifications;
        }
    }
}
