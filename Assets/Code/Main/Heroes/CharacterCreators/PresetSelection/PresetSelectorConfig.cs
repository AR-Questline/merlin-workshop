using System;
using System.IO;
using Awaken.TG.Assets;
using Awaken.TG.Main.Localization;
using Awaken.TG.MVC.Utils;
using Awaken.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Awaken.TG.Main.Heroes.CharacterCreators.PresetSelection {
    public class PresetSelectorConfig : ScriptableObject {
        [ListDrawerSettings(ShowFoldout = false, ListElementLabelName = nameof(SceneSets.Title))]
        public SceneSets[] sceneSets = Array.Empty<SceneSets>();

        public SceneSets GetSceneSet(string sceneName) {
            for (int i = 0; i < sceneSets.Length; i++) {
                if (sceneSets[i].Scene.Name == sceneName) {
                    return sceneSets[i];
                }
            }
            return default;
        }
        
        public SceneSets Cuanacht => GetSceneSet(SpecialSceneNames.Cuanacht);
        public SceneSets Forlorn => GetSceneSet(SpecialSceneNames.Forlorn);
        public SceneSets HornsOfTheSouth => GetSceneSet(SpecialSceneNames.HornsOfTheSouth);
        public SceneSets JailTutorial => GetSceneSet(SpecialSceneNames.JailTutorial);

#if UNITY_EDITOR
        bool _onceOnEditorLoad;
        void OnValidate() {
            if (!_onceOnEditorLoad) {
                // verify that scene paths are up-to-date
                for (int i = 0; i < sceneSets.Length; i++) {
                    sceneSets[i].VerifyScenePath();
                }
                _onceOnEditorLoad = true;
            }
            
            bool anyChanged = false;
            // add default stats if none are present
            for (int i = 0; i < sceneSets.Length; i++) {
                ref SceneSets sceneSets = ref this.sceneSets[i];
                
                for (int j = 0; j < sceneSets.presets.Length; j++) {
                    ref CharacterBuildPreset preset = ref sceneSets.presets[j];
                    anyChanged |= preset.EditorUpdate();
                }
            }
            
            if (anyChanged) {
                EditorUtility.SetDirty(this);
            }
        }
#endif
    }
    
    [Serializable]
    public struct SceneSets {
        [SerializeField, Sirenix.OdinInspector.FilePath(Extensions = ".unity", RequireExistingPath = true),
         OnValueChanged(nameof(OnSceneChanged))]
        string scene;
        [SerializeField, ReadOnly]
        string sceneGUID;
        [Space]
        [UnityEngine.Scripting.Preserve] public LocString displayName;
        [UnityEngine.Scripting.Preserve] public LocString description;
        
        [Space]
        [ListDrawerSettings(ListElementLabelName = nameof(CharacterBuildPreset.name))]
        public CharacterBuildPreset[] presets;

        public SceneReference Scene => SceneReference.ByAddressable(new(sceneGUID));

        public void VerifyScenePath() {
#if UNITY_EDITOR
            scene = AssetDatabase.GUIDToAssetPath(sceneGUID);
#endif
        }
        
        // === Odin
        void OnSceneChanged() {
#if UNITY_EDITOR
            sceneGUID = AssetDatabase.AssetPathToGUID(scene);
#endif
        }
        public string Title => StringUtil.NicifyName(Path.GetFileNameWithoutExtension(scene) ?? "No Scene");
    }
}
