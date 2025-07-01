using System;
using System.IO;
using Awaken.TG.Editor.Utility.Paths;
using Awaken.TG.EditorOnly.WorkflowTools;
using Awaken.TG.Graphics.Cutscenes;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character.Features.Config;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.CharacterCreators.Data;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments.Audio;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Heroes.Setup;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Locations.Shops;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Quests.Objectives.Effectors;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Animations.FightingStyles;
using Awaken.TG.Main.Utility.Animations.Gestures;
using Awaken.TG.Main.Utility.Animations.HitStops;
using Awaken.TG.Main.Utility.Video.Subtitles;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using UnityEditor;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Editor.Assets {
    public static class TemplateCreation {
        // === Menu items

        // === Miscellaneous
        [MenuItem("Assets/TG Data/Miscellaneous/Hero")]
        static void CreateHeroTemplate() => CreatePrefab(HeroTemplate.CreateNewHero);
        [MenuItem("Assets/TG Data/Miscellaneous/Item")]
        static void CreateItemTemplate() => CreatePrefab(PrefabCreator<ItemTemplate>);
        [MenuItem("Assets/Create Quest", priority = 100)]
        static void CreateQuestTemplate() => CreatePrefab(PrefabCreator<QuestTemplate>);
        [MenuItem("Assets/TG Data/Miscellaneous/Achievement")]
        static void CreateAchievementTemplate() => CreatePrefab(PrefabCreator<AchievementTemplate, AchievementObjectiveSpec, AchievementEffectorAttachment>);
        [MenuItem("Assets/TG Data/Miscellaneous/Npc Template")]
        static void CreateNpcTemplate() => CreatePrefab(PrefabCreator<NpcTemplate>);
        [MenuItem("Assets/TG Data/Miscellaneous/Faction")]
        static void CreateFactionTemplate() => CreatePrefab(PrefabCreator<FactionTemplate>);
        [MenuItem("Assets/TG Data/Miscellaneous/Shop Definition")]
        static void CreateShopDefinition() => CreatePrefab(PrefabCreator<ShopTemplate>);
        [MenuItem("Assets/TG Data/Miscellaneous/Status")]
        static void CreateStatusTemplate() => CreatePrefab(PrefabCreator<StatusTemplate>);
        
        // === Loot
        [MenuItem("Assets/TG Data/Loot/Loot Table")]
        static void CreateLootTableAsset() => CreateScriptableObject(ScriptableObjectCreator<LootTableAsset>);

        // === Graphs
        [MenuItem("Assets/Create Story Graph", priority = 100)]
        static void CreateStoryGraph() => CreateScriptableObject(StoryGraph.CreateGraph);
        [MenuItem("Assets/TG Data/Graphs/Skill Graph")]
        static void CreateSkillGraph() => CreatePrefab(PrefabCreator<SkillGraph>);
        
        // === Talents
        [MenuItem("Assets/TG Data/Talents/Table")]
        static void CreateTalentTable() => CreateScriptableObject(ScriptableObjectCreator<TalentTableTemplate>);
        [MenuItem("Assets/TG Data/Talents/Talent")]
        static void CreateTalent() => CreateScriptableObject(ScriptableObjectCreator<TalentTemplate>);
        
        // === Hero
        [MenuItem("Assets/TG Data/Hero/Character Creator")]
        static void CreateCharacterCreatorTemplate() => CreateScriptableObject(ScriptableObjectCreator<CharacterCreatorTemplate>);

        // === Video
        [MenuItem("Assets/TG Data/Video/CutsceneFpp")]
        public static void CreateCutsceneFppTemplate() => CreateScriptableObjectAndPrefab(CutsceneTemplate.CreateCutsceneTemplate, VCutsceneFPP.CreateNewPrefab);
        [MenuItem("Assets/TG Data/Video/CutsceneFreeCam")]
        public static void CreateCutsceneFreeCamTemplate() => CreateScriptableObjectAndPrefab(CutsceneTemplate.CreateCutsceneTemplate, VCutsceneFreeCam.CreateNewPrefab);
        [MenuItem("Assets/TG Data/Video/Subtitles")]
        static void CreateSubtitles() => CreateScriptableObject(ScriptableObjectCreator<SubtitlesData>);
        
        
        // === Audio
        [MenuItem("Assets/TG Data/Audio/Audio Events")]
        static void CreateAudioEvent() => CreateScriptableObject(ScriptableObjectCreator<FModEventRef>);
        [MenuItem("Assets/TG Data/Audio/Hero Gender Specific Audio Events")]
        static void CreateGenderSpecificAudioEvent() => CreateScriptableObject(ScriptableObjectCreator<HeroGenderSpecificFModEventRef>);
        [MenuItem("Assets/TG Data/Audio/Gender Audio Clips Holder")]
        static void CreateGenderAudioClipHolder() => CreateScriptableObject(ScriptableObjectCreator<GenderAudioClipTemplate>);
        [MenuItem("Assets/TG Data/Audio/Item Audio Container")]
        static void CreateItemAudioContainer() => CreateScriptableObject(ScriptableObjectCreator<ItemAudioContainerAsset>);
        [MenuItem("Assets/TG Data/Audio/Character Audio Container")]
        static void CreateCharacterAudioContainer() => CreateScriptableObject(ScriptableObjectCreator<AliveAudioContainerAsset>);

        
        // === Animations
        [MenuItem("Assets/TG Data/Animations/AR Animation Event")]
        static void CreateARAnimationEvent() => CreateScriptableObject(ScriptableObjectCreator<ARAnimationEvent>);
        [MenuItem("Assets/TG Data/Animations/AR Finisher Animation Event")]
        static void CreateARFinisherAnimationEvent() => CreateScriptableObject(ScriptableObjectCreator<ARFinisherAnimationEvent>);
        [MenuItem("Assets/TG Data/Animations/AR VFX Animation Event")]
        static void CreateARVfxAnimationEvent() => CreateScriptableObject(ScriptableObjectCreator<ARVfxAnimationEvent>);
        [MenuItem("Assets/TG Data/Animations/Gesture Overrides")]
        static void CreateGestureOverrides() => CreateScriptableObject(ScriptableObjectCreator<GestureOverridesTemplate>);
        [MenuItem("Assets/TG Data/Animations/Char BlendShape Config")]
        static void CreateCharacterBlendShapeConfig() => CreateScriptableObject(ScriptableObjectCreator<BlendShapeConfigSO>);
        [MenuItem("Assets/TG Data/Animations/Char BlendShape Group Config")]
        static void CreateCharacterBlendShapeGroupConfig() => CreateScriptableObject(ScriptableObjectCreator<BlendShapeGroupSO>);
        [MenuItem("Assets/TG Data/Animations/HitStops Data")]
        static void CreateHitStopDataAsset() => CreateScriptableObject(ScriptableObjectCreator<HitStopsAsset>);
        // === Fighting Styles
        [MenuItem("Assets/TG Data/Fighting Style/Humanoid")]
        static void CreateHumanoidFightingStyle() => CreateScriptableObject(ScriptableObjectCreator<HumanoidFightingStyle>);
        [MenuItem("Assets/TG Data/Fighting Style/Custom")]
        static void CreateCustomFightingStyle() => CreateScriptableObject(ScriptableObjectCreator<CustomFightingStyle>);
        [MenuItem("Assets/TG Data/Fighting Style/Boss")]
        static void CreateBossFightingStyle() => CreateScriptableObject(ScriptableObjectCreator<BossFightingStyle>);
        
        [MenuItem("Assets/TG Data/Create Asset Picker Config")]
        static void CreateAssetPickerConfig() => CreateScriptableObject(ScriptableObjectCreator<AssetPickerConfig>);

        // === Helpers
        public static GameObject CreatePrefab(Func<string, MonoBehaviour> templateCreator, string defaultDirectory = null, bool select = true) {
            if (!TryGetPath("prefab", defaultDirectory, out string path)) {
                return null;
            }
            
            path = PathUtils.FilesystemToAssetPath(path);
            string name = Path.GetFileNameWithoutExtension(path);
            MonoBehaviour template = templateCreator(name);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(template.gameObject, path);
            if (select) {
                Selection.activeObject = prefab;
            }
            // destroy the template
            UnityEngine.Object.DestroyImmediate(template.gameObject);
            // done!
            AssetDatabase.SaveAssets();
            return prefab;
        }

        public static ScriptableObject CreateScriptableObject(Func<string, ScriptableObject> soCreator, string defaultDirectory = null, bool select = true) {
            if (!TryGetPath("asset", defaultDirectory, out string path)) {
                return null;
            }

            path = PathUtils.FilesystemToAssetPath(path);
            string name = Path.GetFileNameWithoutExtension(path);

            ScriptableObject asset = soCreator(name);
            AssetDatabase.CreateAsset(asset, path);

            if (select) {
                Selection.activeObject = asset;
            }

            AssetDatabase.SaveAssets();
            return asset;
        }

        static bool TryGetPath(string extension, string defaultDirectory, out string path) {
            path = EditorUtility.SaveFilePanel("Choose name", defaultDirectory ?? AssetPaths.GetSelectedPathOrFallback(), "", extension);
            if (string.IsNullOrEmpty(path)) {
                return false;
            }
            if (path.Contains("/Resources/")) {
                Log.Important?.Error("Creating templates in the Resources directory is forbidden (template need to be Addressable)");
                return false;
            }
            return true;
        }
        
        static void CreateScriptableObjectAndPrefab(Func<string, GameObject, ScriptableObject> soCreator, Func<string, MonoBehaviour> templateCreator) {
            CreatePrefab(templateCreator);
            var prefab = (GameObject)Selection.activeObject;

            CreateScriptableObject(path => soCreator(path, prefab));
        }

        public static T ScriptableObjectCreator<T>(string name) where T : ScriptableObject {
            T so = ScriptableObject.CreateInstance<T>();
            so.name = name;
            return so;
        }

        static T PrefabCreator<T>(string name) where T : MonoBehaviour {
            return GameObjects.WithSingleBehavior<T>(name: name);
        }
        
        static T1 PrefabCreator<T1, T2>(string name) where T1 : MonoBehaviour where T2 : MonoBehaviour {
            var main = GameObjects.WithSingleBehavior<T1>(name: name);
            main.gameObject.AddComponent<T2>();
            return main;
        }
        static T1 PrefabCreator<T1, T2, T3>(string name) where T1 : MonoBehaviour where T2 : MonoBehaviour where T3 : MonoBehaviour {
            var main = GameObjects.WithSingleBehavior<T1>(name: name);
            main.gameObject.AddComponent<T2>();
            main.gameObject.AddComponent<T3>();
            return main;
        }
        static T1 PrefabCreator<T1, T2, T3, T4>(string name) where T1 : MonoBehaviour where T2 : MonoBehaviour where T3 : MonoBehaviour where T4 : MonoBehaviour {
            var main = GameObjects.WithSingleBehavior<T1>(name: name);
            main.gameObject.AddComponent<T2>();
            main.gameObject.AddComponent<T3>();
            main.gameObject.AddComponent<T4>();
            return main;
        }
    }
}
