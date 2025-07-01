using Awaken.TG.Assets;
using Awaken.TG.Editor.Assets;
using Awaken.TG.Editor.Localizations;
using Awaken.TG.Main.AI.Idle.Data.Attachment;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using XNodeEditor;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Editor.Main.Templates.Presets {
    public class NpcCreator : OdinEditorWindow {
        LocationSpec _spec;
        static string NpcTemplatePath => PrefabReferencesSettings.Instance.DefaultNpcTemplate.Path;

        public static void Show(LocationSpec spec) {
            NpcCreator creator = GetWindow<NpcCreator>("NPC Creator", true);
            creator._spec = spec;
            creator.npcName = spec.displayName;
            creator.gender = spec.GetComponent<NpcAttachment>()?.VisualPrefab?.Address == GetGenderPrefabReference(Gender.Female)?.Address ? Gender.Female : Gender.Male;
            creator.story = spec.GetComponent<DialogueAttachment>()?.bookmark?.story?.Get<StoryGraph>();
            creator.baseNpcTemplate = AssetDatabase.LoadAssetAtPath<NpcTemplate>(NpcTemplatePath);
            creator.Show();
        }
        
        public string npcName;
        [EnumToggleButtons] public Gender gender = Gender.Male;
        [HorizontalGroup("Story")] public StoryGraph story;
        public NpcTemplate baseNpcTemplate;
        [ReadOnly, Tooltip("Coming soon")]
        public bool modifyNpcTemplate = false;

        [HorizontalGroup("Story"), Button, ShowIf("@" + nameof(story) + "==null")]
        void CreateStory() =>
            story = (StoryGraph) TemplateCreation.CreateScriptableObject(StoryGraph.CreateGraph, defaultDirectory: "Assets/Data/Templates/Stories",
                select: false);
        [HorizontalGroup("Story"), Button, ShowIf("@" + nameof(story) + "!=null")]
        void EditStory() => NodeEditorWindow.Open(story);

        [PropertySpace(50), Button(ButtonSizes.Medium), HorizontalGroup("Buttons")]
        void Cancel() {
            Close();
        }

        [PropertySpace(50), Button(ButtonSizes.Medium), HorizontalGroup("Buttons")]
        void Approve() {
            GameObject go = _spec.gameObject;
            CommonPresets.RemoveAllExcept(go, typeof(NpcAttachment), typeof(DialogueAttachment), typeof(LocationSpec), typeof(IdleDataAttachment));
            LocationSpec specInPrefab = go.GetComponent<LocationSpec>();
            // Location Spec
            GameObjects.SetStaticRecursively(go, false);

            // Display Name
            var stringTable = LocalizationTools.PrefabCollection.GetTable(LocalizationSettings.ProjectLocale.Identifier) as StringTable;
            LocalizationUtils.ChangeTextTranslation(_spec.displayName.ID, npcName, stringTable);

            // Prefab
            specInPrefab.prefabReference = new ARAssetReference(PrefabReferencesSettings.Instance.DefaultAI.Guid);

            // NPC Template
            NpcAttachment npcAttach = go.GetOrAddComponent<NpcAttachment>();
            TemplateReferenceDrawer.ValidateDraggedObject(baseNpcTemplate.gameObject);
            TemplatesUtil.EDITOR_AssignGuid(baseNpcTemplate, baseNpcTemplate.gameObject);
            npcAttach.Setup(baseNpcTemplate, GetGenderPrefabReference(gender));

            // Dialogue
            if (story != null) {
                DialogueAttachment dialogueAttach = go.GetOrAddComponent<DialogueAttachment>();
                TemplateReferenceDrawer.ValidateDraggedObject(story);
                TemplatesUtil.EDITOR_AssignGuid(story, story);
                dialogueAttach.bookmark = StoryBookmark.EDITOR_ToInitialChapter(story);
            }
            
            // Idle Behaviours
            go.GetOrAddComponent<IdleDataAttachment>();

            // Apply overrides to prefab
            GameObject prefab = PrefabUtility.GetNearestPrefabInstanceRoot(go);
            if (prefab != null) {
                PrefabUtility.ApplyPrefabInstance(prefab, InteractionMode.AutomatedAction);
            }

            Close();
        }

        static ARAssetReference GetGenderPrefabReference(Gender gender) {
            var genderPrefabGuid = gender == Gender.Male
                ? PrefabReferencesSettings.Instance.DefaultMale.Guid
                : PrefabReferencesSettings.Instance.DefaultFemale.Guid;
            
            if (string.IsNullOrEmpty(genderPrefabGuid)) {
                Log.Important?.Error($"Couldn't find {gender} prefab in {nameof(PrefabReferencesSettings)}");
            }
            return new ARAssetReference(genderPrefabGuid);
        }
    }
}