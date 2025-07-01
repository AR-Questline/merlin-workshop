using System;
using Awaken.TG.Main.AudioSystem.Biomes;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Sirenix.OdinInspector;
using UnityEngine;
using BaseAudioSource = Awaken.TG.Main.AudioSystem.Biomes.BaseAudioSource;

namespace Awaken.TG.Main.AudioSystem {
    public class AudioSceneSet : ScriptableObject {
        [Header("Settings")] 
        public bool interpolateCombatLevel;
        [Header("Music")]
        [SerializeReference, HideReferenceObjectPicker, ListDrawerSettings(ListElementLabelName = "EDITOR_Label", CustomAddFunction = nameof(AddBaseAudioSource))] public BaseAudioSource[] musicAudioSources = Array.Empty<BaseAudioSource>();
        [SerializeReference, HideReferenceObjectPicker, ListDrawerSettings(ListElementLabelName = "EDITOR_Label", CustomAddFunction = nameof(AddBaseAudioSource))] public BaseAudioSource[] musicAlertAudioSources = new BaseAudioSource[0];
        [SerializeReference, HideReferenceObjectPicker, ListDrawerSettings(ListElementLabelName = "EDITOR_Label", CustomAddFunction = nameof(AddCombatAudioSource))] public CombatMusicAudioSource[] musicCombatAudioSources = Array.Empty<CombatMusicAudioSource>();
        
        [SerializeField, Header("Ambient")] public BaseAudioSource ambientAudioSource;
        [SerializeField, Header("Snapshot")] public BaseAudioSource snapshotAudioSource;

        BaseAudioSource AddBaseAudioSource() {
            return new BaseAudioSource();
        }
        
        CombatMusicAudioSource AddCombatAudioSource() {
            return new CombatMusicAudioSource();
        }
#if UNITY_EDITOR
        public static AudioSceneSet CreateFrom(MapScene scene) {
            var so = ScriptableObject.CreateInstance<AudioSceneSet>();
            so.name = scene.gameObject.scene.name;
            so.musicAudioSources = new BaseAudioSource[scene.musicAudioSources.Length];
            for (var i = 0; i < scene.musicAudioSources.Length; i++) {
                so.musicAudioSources[i] = scene.musicAudioSources[i];
            }
            so.musicAlertAudioSources = new BaseAudioSource[scene.musicAlertAudioSources.Length];
            for (var i = 0; i < scene.musicAlertAudioSources.Length; i++) {
                so.musicAlertAudioSources[i] = scene.musicAlertAudioSources[i];
            }
            so.musicCombatAudioSources = new CombatMusicAudioSource[scene.musicCombatAudioSources.Length];
            for (var i = 0; i < scene.musicCombatAudioSources.Length; i++) {
                so.musicCombatAudioSources[i] = scene.musicCombatAudioSources[i];
            }
            so.ambientAudioSource = scene.ambientAudioSource;
            so.snapshotAudioSource = scene.snapshotAudioSource;
            
            var path = $"Assets/Audio/SceneSets/{so.name}.asset";
            UnityEditor.AssetDatabase.CreateAsset(so, path);
            
            // clear scene references
            scene.musicAudioSources = Array.Empty<BaseAudioSource>();
            scene.musicAlertAudioSources = Array.Empty<BaseAudioSource>();
            scene.musicCombatAudioSources = Array.Empty<CombatMusicAudioSource>();
            scene.ambientAudioSource = null;
            scene.snapshotAudioSource = null;
            return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioSceneSet>(path);
        }
#endif
    }
}