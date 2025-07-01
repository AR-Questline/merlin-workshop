using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.ECS.Flocks.Authorings;
using Awaken.TG.Editor.Main.Fmod;
using Awaken.TG.Editor.Main.Stories;
using Awaken.TG.Main.AI.Fights.Projectiles;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.AudioSystem.Biomes;
using Awaken.TG.Main.Crafting;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.CharacterCreators;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Bag;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.FootSteps;
using Awaken.TG.Main.Heroes.HUD;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.Attachments.Audio;
using Awaken.TG.Main.Heroes.Storage;
using Awaken.TG.Main.Heroes.VolumeCheckers;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Actions.Lockpicking;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Locations.Shops.Tabs;
using Awaken.TG.Main.Locations.Spawners.Critters;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Scenes.SceneConstructors.AdditiveScenes;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications;
using Awaken.TG.Main.UI.Menu;
using Awaken.TG.Main.UI.Menu.DeathUI;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.UI.TitleScreen;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Editor;
using Awaken.Utility.GameObjects;
using FMOD.Studio;
using FMODUnity;
using Sirenix.Utilities.Editor;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using GUID = FMOD.GUID;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Utility.Searching {
    public static class SoundsSearching {
        const string CommonReferencesPrefabPath = "Assets/Data/Settings/CommonReferences.prefab";
        const int SharedSoundsCountThreshold = 2;
        const string ApplicationScenePath = "Assets/Scenes/ApplicationScene.unity";
        const string VfxBankName = "VfxSounds";
        const string UIBankName = "UISounds";
        const string HeroBankName = "HeroSounds";
        const string HardToTrackBankName = "HardToTrackSounds";
        const string CommonSoundsBankName = "CommonSounds";
        const string CommonSfxBankName = "CommonSfx";
        const string CommonMusicBankName = "CommonMusic";
        const string CommonAmbienceBankName = "CommonAmbience";
        const string OtherAliveThingsName = "OtherAliveThings";
        const string AnimalsBankName = "AnimalsSounds";
        const string WeaponBankName = "WeaponsSounds";
        const string NpcsBankName = "NpcSounds";
        const string ItemsBankName = "ItemsSounds";
        const string MaleBankName = "MaleSounds";
        const string FemaleBankName = "FemaleSounds";
        const string VoiceOversBankName = "VoiceOvers";
        const string UnusedVoiceOversBankName = "UnusedVoiceOvers";

        static string SoundsEventsBanksFilePath => Application.dataPath + "/SoundsMappingFiles/SoundsEventsBanks.csv";
        static string VOEventsBanksFilePath => Application.dataPath + "/SoundsMappingFiles/VOEventsBanks.csv";

        public static readonly string[] AlwaysLoadedBanksNames = {
            VfxBankName, UIBankName, HeroBankName, HardToTrackBankName, CommonSoundsBankName, CommonSfxBankName, CommonMusicBankName, CommonAmbienceBankName, OtherAliveThingsName, AnimalsBankName, 
            WeaponBankName, NpcsBankName, ItemsBankName, VoiceOversBankName, UnusedVoiceOversBankName };

        static readonly string[] UIPrefabsPaths = {
            "Assets\\Resources\\Prefabs\\MapViews",
            "Assets\\Resources\\Prefabs\\UIComponents",
            "Assets\\UI\\UGUI\\UIComponents"
        };

        static readonly GUID[] InvalidEventsGuids = {
            GUID.Parse("19d42bed-25c8-4d56-bd3c-7903dedafbce"),
            GUID.Parse("456e50cc-a2b0-499e-9a15-c8709869f709"),
            GUID.Parse("163ebe65-f9f0-4ede-aa73-f2207e3c7a04")
        };

        static readonly Dictionary<GUID, (GUID guid, string path)> invalidEventsGuidToValidDataMap = new() {
            { GUID.Parse("19d42bed-25c8-4d56-bd3c-7903dedafbce"), (GUID.Parse("07959dcc-4c0a-4cf1-8426-1edf471159bc"), "event:/SFX/Weapon/Shield/SFX_Weapon_Shield_Block_Wooden") },
            { GUID.Parse("456e50cc-a2b0-499e-9a15-c8709869f709"), (GUID.Parse("d1a6615f-05ae-4be5-96ad-53f614bb3a99"), "event:/SFX/Weapon/Dagger/SFX_Weapon_Dagger_Unequip") },
            { GUID.Parse("163ebe65-f9f0-4ede-aa73-f2207e3c7a04"), (GUID.Parse("18914cf6-32f4-4284-8a68-97252654614d"), "event:/SFX/Weapon/Shield/SFX_Weapon_Shield_Block_Metal") },
        };

        static TemplateService _templateService;
        static CommonReferences _commonReferences;

        [MenuItem("TG/Search/Sounds/Create all sounds mapping file")]
        public static void SearchForSoundsInAllScenes() {
            using var fs = new FileStream(SoundsEventsBanksFilePath, FileMode.Create, FileAccess.Write);
            using var writer = new StreamWriter(fs);
            SearchForSoundsAndWriteBanksMappingToFile(BuildTools.GetAllScenes(), writer);
        }

        [MenuItem("TG/Search/Sounds/Create VO mapping file")]
        public static void SearchForVoiceOvers() {
            FmodEditorUtils.UnloadAllBanks();
            // FmodEditorUtils.LoadAllBanks(out var banksDatas);
            // FmodEditorUtils.GetEventGuidToPathMap(banksDatas, out Dictionary<GUID, string> eventGuidToPathMap);
            using var fs = new FileStream(VOEventsBanksFilePath, FileMode.Create, FileAccess.Write);
            using var writer = new StreamWriter(fs);
            // var voBank = FmodEditorUtils.GetBankWithName(banksDatas, VoiceOversBankName);
            // SearchForVoiceOversAndWriteBanksMappingToFile(writer, voBank, eventGuidToPathMap);
            FmodEditorUtils.UnloadAllBanks();
        }

        // static void SearchForVoiceOversAndWriteBanksMappingToFile(StreamWriter writer, Bank voiceOversBank, Dictionary<GUID, string> eventGuidToPathMap) {
        //     try {
        //         var unusedVoiceOversGuids = new NativeHashSet<GUID>(64, Allocator.Domain);
        //         FmodEditorUtils.GetBankEventsGuids(voiceOversBank, unusedVoiceOversGuids);
        //         var voiceOverGuidToStoryGraphsNamesMap = StoryGraphFmodEditorUtils.GetVoiceOverEventsGuidToBanksMapping();
        //         foreach (var (voiceOverGuid, storyGraphsNames) in voiceOverGuidToStoryGraphsNamesMap) {
        //             unusedVoiceOversGuids.Remove(voiceOverGuid);
        //             if (eventGuidToPathMap.TryGetValue(voiceOverGuid, out var voiceOverPath)) {
        //                 writer.Write(voiceOverPath);
        //                 foreach (var storyGraphName in storyGraphsNames) {
        //                     writer.Write(',');
        //                     writer.Write(storyGraphName);
        //                 }
        //
        //                 writer.Write('\n');
        //             }
        //         }
        //         foreach (var unusedVOEventGuid in unusedVoiceOversGuids) {
        //             if (eventGuidToPathMap.TryGetValue(unusedVOEventGuid, out var voiceOverPath)) {
        //                 writer.Write(voiceOverPath);
        //                 writer.Write(',');
        //                 writer.Write(UnusedVoiceOversBankName);
        //                 writer.Write('\n');
        //             }
        //         }
        //         unusedVoiceOversGuids.Dispose();
        //     } catch (Exception e) {
        //         Debug.LogException(e);
        //     }
        // }

        static void SearchForSoundsAndWriteBanksMappingToFile(string[] scenesToProcessPaths, StreamWriter writer) {
            FmodEditorUtils.UnloadAllBanks();
            // FmodEditorUtils.LoadAllBanks(out var banksDatas);
            // FmodEditorUtils.GetEventGuidToPathMap(banksDatas, out Dictionary<GUID, string> eventGuidToPathMap);

            NativeHashSet<GUID> hardToTrackSoundsGuids = default;
            NativeHashSet<GUID> uiSoundsGuids = default;
            NativeHashSet<GUID> maleSoundsGuids = default;
            NativeHashSet<GUID> femaleSoundsGuids = default;
            NativeHashSet<GUID> heroSoundsGuids = default;
            NativeHashSet<GUID> sharedSoundsGuids = default;
            NativeHashSet<GUID> preloadedSoundsGuids = default;
            NativeHashMap<GUID, int> soundGuidToCountMap = default;
            Dictionary<string, NativeHashSet<GUID>> vfxPathToSoundsGuidsMap = default;
            Dictionary<string, NativeHashSet<GUID>> weaponPathToSoundsGuidsMap = default;
            Dictionary<string, NativeHashSet<GUID>> itemPathToSoundsGuidsMap = default;
            Dictionary<string, NativeHashSet<GUID>> npcPathToSoundsGuidsMap = default;
            Dictionary<string, NativeHashSet<GUID>> animalPathToSoundsGuidsMap = default;
            Dictionary<string, NativeHashSet<GUID>> otherAliveToSoundsGuidsMap = default;
            Dictionary<string, NativeHashSet<GUID>> scenePathToMusicSoundsGuidsMap = default;
            Dictionary<string, NativeHashSet<GUID>> scenePathToAmbienceSoundsGuidsMap = default;
            Dictionary<string, NativeHashSet<GUID>> scenePathToMapItemsSoundsGuidsMap = default;

            try {
                // var voBank = FmodEditorUtils.GetBankWithName(banksDatas, VoiceOversBankName);
                // SearchForVoiceOversAndWriteBanksMappingToFile(writer, voBank, eventGuidToPathMap);

                Allocator allocator = Allocator.Domain;


                InitializeStaticRefs();
                var prefabPathsToExclude = new HashSet<string>(1000);

                vfxPathToSoundsGuidsMap = new(64);
                SearchForMagicSounds(vfxPathToSoundsGuidsMap, allocator, prefabPathsToExclude);
                prefabPathsToExclude.AddRange(vfxPathToSoundsGuidsMap.Keys);

                weaponPathToSoundsGuidsMap = new(64);
                SearchForWeaponsSounds(weaponPathToSoundsGuidsMap, allocator, prefabPathsToExclude);
                prefabPathsToExclude.AddRange(weaponPathToSoundsGuidsMap.Keys);

                itemPathToSoundsGuidsMap = new(64);
                SearchForItemsSounds(itemPathToSoundsGuidsMap, allocator, prefabPathsToExclude);
                prefabPathsToExclude.AddRange(itemPathToSoundsGuidsMap.Keys);

                npcPathToSoundsGuidsMap = new(64);
                SearchForEnemiesSounds(npcPathToSoundsGuidsMap, allocator, prefabPathsToExclude);
                prefabPathsToExclude.AddRange(npcPathToSoundsGuidsMap.Keys);
                SearchForNpcsSounds(npcPathToSoundsGuidsMap, allocator, prefabPathsToExclude);
                prefabPathsToExclude.AddRange(npcPathToSoundsGuidsMap.Keys);

                animalPathToSoundsGuidsMap = new(64);
                SearchForAnimalsSounds(animalPathToSoundsGuidsMap, allocator, prefabPathsToExclude);
                prefabPathsToExclude.AddRange(animalPathToSoundsGuidsMap.Keys);

                otherAliveToSoundsGuidsMap = new(64);
                SearchForOtherAliveSounds(otherAliveToSoundsGuidsMap, allocator, prefabPathsToExclude);
                prefabPathsToExclude.AddRange(otherAliveToSoundsGuidsMap.Keys);

                uiSoundsGuids = new(64, allocator);
                SearchForUISounds(uiSoundsGuids, null);

                maleSoundsGuids = new(64, allocator);
                femaleSoundsGuids = new(64, allocator);
                heroSoundsGuids = new(64, allocator);

                SearchForHeroSounds(maleSoundsGuids, femaleSoundsGuids, heroSoundsGuids);

                scenePathToMusicSoundsGuidsMap = new(64);
                scenePathToAmbienceSoundsGuidsMap = new(64);
                scenePathToMapItemsSoundsGuidsMap = new(64);

                SearchForSoundsInScenes(scenesToProcessPaths, scenePathToMusicSoundsGuidsMap, scenePathToAmbienceSoundsGuidsMap, scenePathToMapItemsSoundsGuidsMap, allocator);

                soundGuidToCountMap = new(9999, allocator);

                soundGuidToCountMap.Clear();
                AddSoundGuidsCount(vfxPathToSoundsGuidsMap, soundGuidToCountMap);
                AddSoundGuidsCount(heroSoundsGuids, soundGuidToCountMap);
                AddSoundGuidsCount(uiSoundsGuids, soundGuidToCountMap);
                AddSoundGuidsCount(npcPathToSoundsGuidsMap, soundGuidToCountMap);
                AddSoundGuidsCount(animalPathToSoundsGuidsMap, soundGuidToCountMap);
                AddSoundGuidsCount(weaponPathToSoundsGuidsMap, soundGuidToCountMap);
                AddSoundGuidsCount(itemPathToSoundsGuidsMap, soundGuidToCountMap);
                AddSoundGuidsCount(maleSoundsGuids, soundGuidToCountMap);
                AddSoundGuidsCount(femaleSoundsGuids, soundGuidToCountMap);
                AddSoundGuidsCount(scenePathToMusicSoundsGuidsMap, soundGuidToCountMap);
                AddSoundGuidsCount(scenePathToAmbienceSoundsGuidsMap, soundGuidToCountMap);
                AddSoundGuidsCount(scenePathToMapItemsSoundsGuidsMap, soundGuidToCountMap);

                hardToTrackSoundsGuids = new NativeHashSet<GUID>(50, allocator);
                SearchForHardToTrackSounds(hardToTrackSoundsGuids, soundGuidToCountMap);

                preloadedSoundsGuids = new NativeHashSet<GUID>(50, allocator);

                foreach (var (_, guids) in vfxPathToSoundsGuidsMap) {
                    foreach (var guid in guids) {
                        uiSoundsGuids.Remove(guid);
                        heroSoundsGuids.Remove(guid);

                        preloadedSoundsGuids.Add(guid);
                    }
                }
                foreach (var guid in uiSoundsGuids) {
                    heroSoundsGuids.Remove(guid);
                }
                preloadedSoundsGuids.AddRange(uiSoundsGuids);

                preloadedSoundsGuids.AddRange(heroSoundsGuids);

                sharedSoundsGuids = new NativeHashSet<GUID>(50, allocator);
                var otherVfxGuids = new NativeHashSet<GUID>(50, allocator);
                foreach (var soundGuidWithCount in soundGuidToCountMap) {
                    // if (eventGuidToPathMap.TryGetValue(soundGuidWithCount.Key, out var soundPath) && soundPath.StartsWith("event:/SFX/VFX/")) {
                    //     preloadedSoundsGuids.Add(soundGuidWithCount.Key);
                    //     otherVfxGuids.Add(soundGuidWithCount.Key);
                    // } else if (soundGuidWithCount.Value >= SharedSoundsCountThreshold) {
                    //     sharedSoundsGuids.Add(soundGuidWithCount.Key);
                    // }
                }
                vfxPathToSoundsGuidsMap.Add(VfxBankName, otherVfxGuids);

                Dictionary<string, HashSet<string>> soundPathToBanksMap = new();


                // -- Preloaded banks
                // foreach (var (_, soundsGuids) in vfxPathToSoundsGuidsMap) {
                //     AddSoundBank(VfxBankName, soundsGuids, eventGuidToPathMap, default, default, soundPathToBanksMap);
                // }
                //
                // AddSoundBank(UIBankName, uiSoundsGuids, eventGuidToPathMap, default, default, soundPathToBanksMap);
                //
                // AddSoundBank(HeroBankName, heroSoundsGuids, eventGuidToPathMap, default, default, soundPathToBanksMap);
                //
                // // -- End of preloaded banks
                // var sharedAlwaysLoadedOnMapsSoundsGUIDs = new NativeHashSet<GUID>(999, ARAlloc.Temp);
                //
                // AddCommonSoundBank(sharedSoundsGuids, eventGuidToPathMap, preloadedSoundsGuids, soundPathToBanksMap);
                // sharedAlwaysLoadedOnMapsSoundsGUIDs.AddRange(sharedSoundsGuids);
                //
                // AddSoundBank(HardToTrackBankName, hardToTrackSoundsGuids, eventGuidToPathMap, sharedSoundsGuids, preloadedSoundsGuids, soundPathToBanksMap);
                // sharedAlwaysLoadedOnMapsSoundsGUIDs.AddRange(hardToTrackSoundsGuids);
                //
                // foreach (var (_, soundsGuids) in animalPathToSoundsGuidsMap) {
                //     sharedAlwaysLoadedOnMapsSoundsGUIDs.AddRange(soundsGuids);
                //     AddSoundBank(AnimalsBankName, soundsGuids, eventGuidToPathMap, sharedSoundsGuids, preloadedSoundsGuids, soundPathToBanksMap);
                // }
                //
                // foreach (var (_, soundsGuids) in npcPathToSoundsGuidsMap) {
                //     sharedAlwaysLoadedOnMapsSoundsGUIDs.AddRange(soundsGuids);
                //     AddSoundBank(NpcsBankName, soundsGuids, eventGuidToPathMap, sharedSoundsGuids, preloadedSoundsGuids, soundPathToBanksMap);
                // }
                //
                // foreach (var (_, soundsGuids) in otherAliveToSoundsGuidsMap) {
                //     var usedGuids = new NativeHashSet<GUID>(sharedSoundsGuids.Count + sharedAlwaysLoadedOnMapsSoundsGUIDs.Count, ARAlloc.Temp);
                //     usedGuids.AddRange(sharedSoundsGuids);
                //     usedGuids.AddRange(sharedAlwaysLoadedOnMapsSoundsGUIDs);
                //     AddSoundBank(OtherAliveThingsName, soundsGuids, eventGuidToPathMap, usedGuids, preloadedSoundsGuids, soundPathToBanksMap);
                //     usedGuids.Dispose();
                // }
                //
                // sharedAlwaysLoadedOnMapsSoundsGUIDs.Dispose();
                //
                // foreach (var (_, soundsGuids) in weaponPathToSoundsGuidsMap) {
                //     AddSoundBank(WeaponBankName, soundsGuids, eventGuidToPathMap, sharedSoundsGuids, preloadedSoundsGuids, soundPathToBanksMap);
                // }
                //
                // foreach (var (_, soundsGuids) in itemPathToSoundsGuidsMap) {
                //     AddSoundBank(ItemsBankName, soundsGuids, eventGuidToPathMap, sharedSoundsGuids, preloadedSoundsGuids, soundPathToBanksMap);
                // }
                //
                // AddSoundBank(MaleBankName, maleSoundsGuids, eventGuidToPathMap, sharedSoundsGuids, preloadedSoundsGuids, soundPathToBanksMap);
                //
                // AddSoundBank(FemaleBankName, femaleSoundsGuids, eventGuidToPathMap, sharedSoundsGuids, preloadedSoundsGuids, soundPathToBanksMap);
                //
                // foreach (var (_, soundsGuids) in scenePathToMusicSoundsGuidsMap) {
                //     AddSoundBank(CommonMusicBankName, soundsGuids, eventGuidToPathMap,
                //         sharedSoundsGuids, preloadedSoundsGuids, soundPathToBanksMap);
                // }
                //
                // foreach (var (_, soundsGuids) in scenePathToAmbienceSoundsGuidsMap) {
                //     AddSoundBank(CommonAmbienceBankName, soundsGuids, eventGuidToPathMap,
                //         sharedSoundsGuids, preloadedSoundsGuids, soundPathToBanksMap);
                // }
                //
                // foreach (var (_, soundsGuids) in scenePathToMapItemsSoundsGuidsMap) {
                //     AddSoundBank(ItemsBankName, soundsGuids, eventGuidToPathMap,
                //         sharedSoundsGuids, preloadedSoundsGuids, soundPathToBanksMap);
                // }
                //
                // foreach (var (soundPath, banks) in soundPathToBanksMap) {
                //     writer.Write(soundPath);
                //     foreach (var bank in banks) {
                //         writer.Write(',');
                //         writer.Write(bank);
                //     }
                //
                //     writer.Write('\n');
                // }
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                if (uiSoundsGuids.IsCreated) uiSoundsGuids.Dispose();
                if (maleSoundsGuids.IsCreated) maleSoundsGuids.Dispose();
                if (femaleSoundsGuids.IsCreated) femaleSoundsGuids.Dispose();
                if (heroSoundsGuids.IsCreated) heroSoundsGuids.Dispose();
                if (hardToTrackSoundsGuids.IsCreated) hardToTrackSoundsGuids.Dispose();
                if (sharedSoundsGuids.IsCreated) sharedSoundsGuids.Dispose();
                if (preloadedSoundsGuids.IsCreated) preloadedSoundsGuids.Dispose();
                if (soundGuidToCountMap.IsCreated) soundGuidToCountMap.Dispose();
                DisposeDictionaryNativeHashSet(vfxPathToSoundsGuidsMap);
                DisposeDictionaryNativeHashSet(weaponPathToSoundsGuidsMap);
                DisposeDictionaryNativeHashSet(itemPathToSoundsGuidsMap);
                DisposeDictionaryNativeHashSet(npcPathToSoundsGuidsMap);
                DisposeDictionaryNativeHashSet(animalPathToSoundsGuidsMap);
                DisposeDictionaryNativeHashSet(otherAliveToSoundsGuidsMap);
                DisposeDictionaryNativeHashSet(scenePathToMusicSoundsGuidsMap);
                DisposeDictionaryNativeHashSet(scenePathToAmbienceSoundsGuidsMap);
                DisposeDictionaryNativeHashSet(scenePathToMapItemsSoundsGuidsMap);
            }

            FmodEditorUtils.UnloadAllBanks();
        }

        static void DisposeDictionaryNativeHashSet(Dictionary<string, NativeHashSet<GUID>> dictionary) {
            if (dictionary == null)
                return;

            foreach (var kvp in dictionary) {
                kvp.Value.Dispose();
            }

            dictionary.Clear();
        }

        static void InitializeStaticRefs() {
            _commonReferences = AssetDatabase.LoadAssetAtPath<GameObject>(CommonReferencesPrefabPath).GetComponent<CommonReferences>();
            _templateService = _commonReferences.gameObject.GetComponentInChildren<TemplateService>();
        }

        [MenuItem("TG/Search/Replace Invalid Audio")]
        public static void SearchForInvalidShieldBlockAudio() {
            HashSet<EventReference> eventReferences = new();
            var prefabsGuids = GetPrefabsGuids("Assets");
            HashSet<EventReference> allEventReferences = new();
            AssetDatabase.StartAssetEditing();
            foreach (var prefabGuid in prefabsGuids) {
                eventReferences.Clear();
                var prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                var prefabContentsRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                var aliveAudioAttachments = prefabContentsRoot.GetComponents<AliveAudioAttachment>();
                var itemAudioAttachments = prefabContentsRoot.GetComponents<ItemAudioAttachment>();
                foreach (var aliveAudioAttachment in aliveAudioAttachments) {
                    if (aliveAudioAttachment.aliveAudioContainerWrapper?.assetType != EmbedExplicitWrapper<AliveAudioContainerAsset, AliveAudioContainer>.AssetType.Embed) {
                        continue;
                    }

                    AddAliveAudioContainerSoundsEvents(aliveAudioAttachment.aliveAudioContainerWrapper?.Data, eventReferences);
                    AddAliveAudioContainerSoundsEvents(aliveAudioAttachment.wyrdConvertedAudioContainerWrapper?.Data, eventReferences);
                }

                foreach (var itemAudioAttachment in itemAudioAttachments) {
                    if (itemAudioAttachment.itemAudioContainerWrapper?.assetType != EmbedExplicitWrapper<ItemAudioContainerAsset, ItemAudioContainer>.AssetType.Embed) {
                        continue;
                    }

                    AddItemAudioAttachmentSoundsEvents(itemAudioAttachment.itemAudioContainerWrapper?.Data, eventReferences);
                }

                allEventReferences.AddRange(eventReferences);
                if (ContainsInvalidEvent(InvalidEventsGuids, eventReferences) == false) {
                    continue;
                }

                using var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath);
                prefabContentsRoot = scope.prefabContentsRoot;
                aliveAudioAttachments = prefabContentsRoot.GetComponents<AliveAudioAttachment>();
                itemAudioAttachments = prefabContentsRoot.GetComponents<ItemAudioAttachment>();
                foreach (var aliveAudioAttachment in aliveAudioAttachments) {
                    ReplaceAliveAudioContainerInvalidSounds(aliveAudioAttachment, invalidEventsGuidToValidDataMap);
                }

                foreach (var itemAudioAttachment in itemAudioAttachments) {
                    ReplaceItemAudioContainerInvalidSounds(itemAudioAttachment, invalidEventsGuidToValidDataMap);
                }
            }

            var aliveAudioContainers = AssetDatabaseUtils.GetAllAssetsOfType<AliveAudioContainerAsset>();
            var itemsAudioContainers = AssetDatabaseUtils.GetAllAssetsOfType<ItemAudioContainerAsset>();
            foreach (var aliveAudioContainerAsset in aliveAudioContainers) {
                eventReferences.Clear();
                AddAliveAudioContainerSoundsEvents(aliveAudioContainerAsset.audioContainer, eventReferences);
                allEventReferences.AddRange(eventReferences);
                if (ContainsInvalidEvent(InvalidEventsGuids, eventReferences) == false) {
                    continue;
                }

                ReplaceAliveAudioContainerInvalidSounds(aliveAudioContainerAsset, invalidEventsGuidToValidDataMap);
                EditorUtility.SetDirty(aliveAudioContainerAsset);
            }

            foreach (var itemAudioContainerAsset in itemsAudioContainers) {
                eventReferences.Clear();
                AddItemAudioAttachmentSoundsEvents(itemAudioContainerAsset.audioContainer, eventReferences);
                allEventReferences.AddRange(eventReferences);
                if (ContainsInvalidEvent(InvalidEventsGuids, eventReferences) == false) {
                    continue;
                }

                ReplaceItemAudioContainerInvalidSounds(itemAudioContainerAsset, invalidEventsGuidToValidDataMap);
                EditorUtility.SetDirty(itemAudioContainerAsset);
            }

            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            // FmodEditorUtils.LoadAllBanks(out var banksDatas);
            // FmodEditorUtils.GetEventGuidToPathMap(banksDatas, out var eventGuidToPathMap);
            Debug.Log("Invalid event refs");
            foreach (var eventReference in allEventReferences) {
                // if (eventGuidToPathMap.ContainsKey(eventReference.Guid) == false) {
                //     Debug.Log($"{eventReference.Guid.ToString()} | {eventReference.Path}");
                // }
            }

            FmodEditorUtils.UnloadAllBanks();

            static bool ContainsInvalidEvent(GUID[] invalidEventsGuids, HashSet<EventReference> eventReferences) {
                foreach (var eventReference in eventReferences) {
                    foreach (GUID guid in invalidEventsGuids) {
                        if (guid == eventReference.Guid) {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        static void AddSoundGuidsCount(Dictionary<string, NativeHashSet<GUID>> pathToSoundsGuidsMap, NativeHashMap<GUID, int> soundGuidToCountMap) {
            foreach (var (_, soundsGuids) in pathToSoundsGuidsMap) {
                AddSoundGuidsCount(soundsGuids, soundGuidToCountMap);
            }
        }

        static void AddSoundGuidsCount(NativeHashSet<GUID> soundsGuids, NativeHashMap<GUID, int> soundGuidToCountMap) {
            foreach (var soundGuid in soundsGuids) {
                if (soundGuidToCountMap.TryGetValue(soundGuid, out int count) == false) {
                    count = 0;
                }

                soundGuidToCountMap[soundGuid] = count + 1;
            }
        }

        static void AddSoundBank(string bankName, NativeHashSet<GUID> soundsGuids, Dictionary<GUID, string> soundGuidToPath,
            NativeHashSet<GUID> sharedSoundsGuids, NativeHashSet<GUID> preloadedSoundsGuids,
            Dictionary<string, HashSet<string>> soundPathToBanksMap) {
            if (soundsGuids.Count == 0 || (soundsGuids.Count == 1 && soundsGuids.GetFirstAndOnlyEntry() == default)) {
                return;
            }

            foreach (GUID soundGuid in soundsGuids) {
                if (soundGuid == default || IsInSharedSounds(soundGuid) || IsInPreloadedSounds(soundGuid)) {
                    continue;
                }

                var soundPath = soundGuidToPath.GetValueOrDefault(soundGuid, soundGuid.ToString());
                if (soundPathToBanksMap.TryGetValue(soundPath, out var banks) == false) {
                    banks = new HashSet<string>(1);
                    soundPathToBanksMap.Add(soundPath, banks);
                }

                banks.Add(bankName);
            }

            bool IsInSharedSounds(GUID guid) => sharedSoundsGuids.IsCreated && sharedSoundsGuids.Contains(guid);
            bool IsInPreloadedSounds(GUID guid) => preloadedSoundsGuids.IsCreated && preloadedSoundsGuids.Contains(guid);
        }

        static void AddCommonSoundBank(NativeHashSet<GUID> soundsGuids, Dictionary<GUID, string> soundGuidToPath,
            NativeHashSet<GUID> preloadedSoundsGuids,
            Dictionary<string, HashSet<string>> soundPathToBanksMap) {
            if (soundsGuids.Count == 0 || (soundsGuids.Count == 1 && soundsGuids.GetFirstAndOnlyEntry() == default)) {
                return;
            }

            foreach (GUID soundGuid in soundsGuids) {
                if (soundGuid == default || IsInPreloadedSounds(soundGuid)) {
                    continue;
                }

                var soundPath = soundGuidToPath.GetValueOrDefault(soundGuid, soundGuid.ToString());
                if (soundPathToBanksMap.TryGetValue(soundPath, out var banks) == false) {
                    banks = new HashSet<string>(1);
                    soundPathToBanksMap.Add(soundPath, banks);
                }
                if (soundPath.StartsWith("event:/SFX/")) {
                    banks.Add(CommonSfxBankName);
                } else if (soundPath.StartsWith("event:/Music/")) {
                    banks.Add(CommonMusicBankName);
                } else {
                    banks.Add(CommonSoundsBankName);
                }
            }

            bool IsInPreloadedSounds(GUID guid) => preloadedSoundsGuids.IsCreated && preloadedSoundsGuids.Contains(guid);
        }

        static void SearchForAnimalsSounds(Dictionary<string, NativeHashSet<GUID>> pathToSoundsGuidsMap, Allocator allocator, HashSet<string> excludedPaths) {
            SearchForSoundsInPrefabs("Assets", IsAnimalLocationSpec, excludedPaths, pathToSoundsGuidsMap, allocator, searchAliveComponents: true);
        }

        static bool IsAnimalLocationSpec(GameObject prefab) {
            try {
                return prefab.TryGetComponent(out LocationSpec _) && prefab.TryGetComponent(out NpcAttachment npcAttachment) &&
                       npcAttachment.NpcTemplate != null && _templateService.IsAnimal(npcAttachment.NpcTemplate);
            } catch (Exception) {
                return false;
            }
        }

        static void SearchForEnemiesSounds(Dictionary<string, NativeHashSet<GUID>> pathToSoundsGuidsMap, Allocator allocator, HashSet<string> excludedPaths) {
            SearchForSoundsInPrefabs("Assets", IsMonsterLocationSpec, excludedPaths, pathToSoundsGuidsMap, allocator, searchAliveComponents: true);
        }

        static bool IsMonsterLocationSpec(GameObject prefab) {
            try {
                bool isNpc = prefab.TryGetComponent(out LocationSpec _) & prefab.TryGetComponent(out NpcAttachment npcAttachment);
                if (isNpc == false) {
                    return false;
                }

                var npcTemplate = npcAttachment.NpcTemplate;
                return npcTemplate != null && _templateService.IsMonster(npcTemplate);
            } catch (Exception) {
                return false;
            }
        }

        static void SearchForNpcsSounds(Dictionary<string, NativeHashSet<GUID>> pathToSoundsGuidsMap, Allocator allocator, HashSet<string> excludedPaths) {
            SearchForSoundsInPrefabs("Assets", IsNpcLocationSpec, excludedPaths, pathToSoundsGuidsMap, allocator, searchAliveComponents: true);
        }

        static bool IsNpcLocationSpec(GameObject prefab) {
            try {
                return prefab.TryGetComponent(out LocationSpec _) && prefab.TryGetComponent(out NpcAttachment npcAttachment) &&
                       npcAttachment.NpcTemplate != null;
            } catch (Exception) {
                return false;
            }
        }

        static void SearchForOtherAliveSounds(Dictionary<string, NativeHashSet<GUID>> pathToSoundsGuidsMap, Allocator allocator, HashSet<string> excludedPaths) {
            SearchForSoundsInPrefabs("Assets", null, excludedPaths, pathToSoundsGuidsMap, allocator, searchAliveComponents: true);
        }

        static void SearchForHardToTrackSounds(NativeHashSet<GUID> soundsGuids, NativeHashMap<GUID, int> allFoundSoundGuidToCountMap) {
            var fmodEventsRefs = AssetDatabaseUtils.GetAllAssetsOfType<FModEventRef>();
            foreach (var fmodEventRef in fmodEventsRefs) {
                soundsGuids.Add(fmodEventRef.eventPath.Guid);
            }

            var aliveAudioAssets = AssetDatabaseUtils.GetAllAssetsOfType<AliveAudioContainerAsset>();
            foreach (var aliveAudioAsset in aliveAudioAssets) {
                AddAliveAudioContainerSounds(aliveAudioAsset.audioContainer, soundsGuids);
            }

            var itemAudioAssets = AssetDatabaseUtils.GetAllAssetsOfType<ItemAudioContainerAsset>();
            foreach (var itemAudioAsset in itemAudioAssets) {
                AddItemAudioContainerSounds(itemAudioAsset.audioContainer, soundsGuids);
            }

            var soundsGuidsCopy = soundsGuids.ToNativeArray(ARAlloc.Temp);
            foreach (var soundGuid in soundsGuidsCopy) {
                if (allFoundSoundGuidToCountMap.ContainsKey(soundGuid)) {
                    soundsGuids.Remove(soundGuid);
                }
            }
        }

        static void SearchForWeaponsSounds(Dictionary<string, NativeHashSet<GUID>> pathToSoundsGuidsMap, Allocator allocator, HashSet<string> excludedPaths) {
            SearchForSoundsInPrefabs("Assets", IsWeaponItem, excludedPaths, pathToSoundsGuidsMap, allocator, searchAliveComponents: true);
        }

        static bool IsWeaponItem(GameObject prefab) {
            try {
                return prefab.TryGetComponent(out ItemTemplate itemTemplate) && _templateService.IsNonMagicWeaponOrArrow(itemTemplate);
            } catch (Exception) {
                return false;
            }
        }

        static void SearchForMagicSounds(Dictionary<string, NativeHashSet<GUID>> pathToSoundsGuidsMap, Allocator allocator, HashSet<string> excludedPaths) {
            SearchForSoundsInPrefabs("Assets", IsMagicItem, excludedPaths, pathToSoundsGuidsMap, allocator, searchAliveComponents: true);

            static bool IsMagicItem(GameObject prefab) {
                try {
                    return prefab.name.StartsWith("VFX_") ||
                           (prefab.TryGetComponent(out ItemTemplate itemTemplate) && itemTemplate.IsMagic);
                } catch (Exception) {
                    return false;
                }
            }
        }

        static void SearchForItemsSounds(Dictionary<string, NativeHashSet<GUID>> pathToSoundsGuidsMap, Allocator allocator, HashSet<string> excludedPaths) {
            SearchForSoundsInPrefabs("Assets", null, excludedPaths, pathToSoundsGuidsMap, allocator);

            AddFishingSounds(pathToSoundsGuidsMap, allocator);
        }

        static void SearchForUISounds(NativeHashSet<GUID> uiSoundsGuids, HashSet<string> excludedPaths) {
            var commonReferences = CommonReferences.Get;
            uiSoundsGuids.Add(commonReferences.AudioConfig.TabSelectedSound.Guid);
            uiSoundsGuids.Add(commonReferences.AudioConfig.ButtonSelectedSound.Guid);
            uiSoundsGuids.Add(commonReferences.AudioConfig.ButtonClickedSound.Guid);
            uiSoundsGuids.Add(commonReferences.AudioConfig.ButtonApplySound.Guid);

            uiSoundsGuids.Add(commonReferences.AudioConfig.ObjectiveAudio.ObjectiveChangedSound.Guid);
            uiSoundsGuids.Add(commonReferences.AudioConfig.ObjectiveAudio.ObjectiveCompletedSound.Guid);
            uiSoundsGuids.Add(commonReferences.AudioConfig.ObjectiveAudio.ObjectiveFailedSound.Guid);

            uiSoundsGuids.Add(commonReferences.AudioConfig.QuestAudio.QuestTakenSound.Guid);
            uiSoundsGuids.Add(commonReferences.AudioConfig.QuestAudio.QuestCompletedSound.Guid);
            uiSoundsGuids.Add(commonReferences.AudioConfig.QuestAudio.QuestFailedSound.Guid);

            uiSoundsGuids.Add(_commonReferences.AudioConfig.LightNegativeFeedbackSound.Guid);
            uiSoundsGuids.Add(_commonReferences.AudioConfig.StrongNegativeFeedbackSound.Guid);
            uiSoundsGuids.Add(_commonReferences.AudioConfig.StartGameSound.Guid);

            foreach (var prefabsPath in UIPrefabsPaths) {
                SearchForSoundsInPrefabs(prefabsPath, null, excludedPaths, allSoundsGuids: uiSoundsGuids, searchUIComponents: true, searchAliveComponents: true);
            }

            AddPresenterDataProvidersSounds(uiSoundsGuids);
        }

        static void SearchForHeroSounds(NativeHashSet<GUID> maleSoundsGuids, NativeHashSet<GUID> femaleSoundsGuids, NativeHashSet<GUID> heroSoundsGuids) {
            const string VCharacterCreatorPrefabPath = "Assets/Resources/Prefabs/MapViews/CharacterCreator/VCharacterCreator.prefab";
            const string VCharacterControllerPrefabPath = "Assets/Resources/Prefabs/MapViews/Hero/VHeroController.prefab";
            const string VHeroStaminaUsedUpEffectPrefabPath = "Assets/Resources/Prefabs/MapViews/Hero/VHeroStaminaUsedUpEffect.prefab";

            var vCharacterCreatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VCharacterCreatorPrefabPath);
            if (vCharacterCreatorPrefab != null && vCharacterCreatorPrefab.TryGetComponent(out VCharacterCreator vCharacterCreator)) {
                AddAliveAudioContainerSounds(vCharacterCreator.maleAudioContainer.Data, maleSoundsGuids);
                AddAliveAudioContainerSounds(vCharacterCreator.femaleAudioContainer.Data, femaleSoundsGuids);
            } else {
                Log.Important?.Error($"No {nameof(VCharacterCreator)} on path {VCharacterCreatorPrefabPath}");
            }

            var vCharacterControllerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VCharacterControllerPrefabPath);
            if (vCharacterControllerPrefab != null && vCharacterControllerPrefab.TryGetComponentInChildren<VHeroFootsteps>(true, out var vHeroFootsteps)) {
                heroSoundsGuids.Add(vHeroFootsteps.footStepEventPath.Guid);
            } else {
                Log.Important?.Error($"No {nameof(VHeroFootsteps)} on path {VCharacterControllerPrefabPath}");
            }

            var vHeroStaminaUsedUpEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VHeroStaminaUsedUpEffectPrefabPath);
            if (vHeroStaminaUsedUpEffectPrefab != null && vHeroStaminaUsedUpEffectPrefab.TryGetComponent<VHeroStaminaUsedUpEffect>(out var vHeroStaminaUsedUpEffect)) {
                maleSoundsGuids.Add(vHeroStaminaUsedUpEffect.maleEventReference.Guid);
                maleSoundsGuids.Add(vHeroStaminaUsedUpEffect.maleSnapshotEventReference.Guid);
                femaleSoundsGuids.Add(vHeroStaminaUsedUpEffect.femaleEventReference.Guid);
                femaleSoundsGuids.Add(vHeroStaminaUsedUpEffect.femaleSnapshotEventReference.Guid);
            } else {
                Log.Important?.Error($"No {nameof(VHeroStaminaUsedUpEffect)} on path {VHeroStaminaUsedUpEffectPrefabPath}");
            }

            var commonReferences = CommonReferences.Get;
            var gameConstants = GameConstants.Get;

            var eventPairs = commonReferences.AudioConfig.StatusAudioMap.statusEventPairs;
            foreach (var eventPair in eventPairs) {
                maleSoundsGuids.Add(eventPair.GetEventReference(Gender.Male).Guid);
                femaleSoundsGuids.Add(eventPair.GetEventReference(Gender.Female).Guid);
            }

            heroSoundsGuids.Add(commonReferences.AudioConfig.HeroHitBonusAudio.Guid);
            heroSoundsGuids.Add(gameConstants.wyrdStalkerSpawnAudioCue.Guid);
            heroSoundsGuids.Add(gameConstants.wyrdStalkerOnSightAudioCue.Guid);
            heroSoundsGuids.Add(gameConstants.wyrdStalkerHideAudioCue.Guid);

            AddAliveAudioContainerSounds(commonReferences.AudioConfig.DefaultAliveAudioContainer, heroSoundsGuids);

            heroSoundsGuids.Add(_commonReferences.AudioConfig.HeroLandedSound.Guid);
            heroSoundsGuids.Add(_commonReferences.AudioConfig.AttackOutsideFOVWarningSound.Guid);
            heroSoundsGuids.Add(_commonReferences.AudioConfig.DefaultEnemyFootStep.Guid);

            var heroGenderSpecificFModEventRefs = AssetDatabaseUtils.GetAllAssetsOfType<HeroGenderSpecificFModEventRef>();
            foreach (var fmodEventRef in heroGenderSpecificFModEventRefs) {
                maleSoundsGuids.Add(fmodEventRef.eventPath.Guid);
                femaleSoundsGuids.Add(fmodEventRef.femaleEventPath.Guid);
            }
        }

        static void SearchForSoundsInScenes(string[] scenesToProcessPaths, Dictionary<string, NativeHashSet<GUID>> scenePathToMusicSoundsGuidsMap,
            Dictionary<string, NativeHashSet<GUID>> scenePathToAmbienceSoundsGuidsMap, Dictionary<string, NativeHashSet<GUID>> scenePathToMapItemsSoundsGuidsMap, Allocator allocator) {
            // Enable previews to gather dynamic items sounds
            var prevAllPreviewsEnabled = LocationSpec.AllPreviewsEnabled;
            LocationSpec.AllPreviewsEnabled = true;
            var prevLocationPreviewsEnabled = LocationSpec.LocationPreviewsEnabled;
            LocationSpec.LocationPreviewsEnabled = true;

            foreach (string scenePath in scenesToProcessPaths) {
                if (scenePath != ApplicationScenePath && scenePath.Contains("Dev_Scenes") == false)
                    SearchForSoundsInScene(scenePathToMusicSoundsGuidsMap, scenePathToAmbienceSoundsGuidsMap, scenePathToMapItemsSoundsGuidsMap, allocator, scenePath);
            }

            SearchForSoundsInScene(scenePathToMusicSoundsGuidsMap, scenePathToAmbienceSoundsGuidsMap, scenePathToMapItemsSoundsGuidsMap, allocator, ApplicationScenePath);
            LocationSpec.AllPreviewsEnabled = prevAllPreviewsEnabled;
            LocationSpec.LocationPreviewsEnabled = prevLocationPreviewsEnabled;
        }

        static void SearchForSoundsInScene(Dictionary<string, NativeHashSet<GUID>> scenePathToMusicSoundsGuidsMap, Dictionary<string, NativeHashSet<GUID>> scenePathToAmbienceSoundsGuidsMap, Dictionary<string, NativeHashSet<GUID>> scenePathToMapItemsSoundsGuidsMap, Allocator allocator, string scenePath) {
            if (BuildTools.IsSubsceneByPath(scenePath)) {
                return;
            }

            using SceneResources sr = new(scenePath, true);

            var allLoadedScenes = sr.loadedSubscenes;
            allLoadedScenes.Add(sr.loadedScene);
            // Ensure all dynamic locations loaded
            var locationSpecs = GameObjects.FindComponentsByTypeInScenes<LocationSpec>(allLoadedScenes, true);
            foreach (var locationSpec in locationSpecs) {
                locationSpec.ValidatePrefab(true);
            }

            ListPool<LocationSpec>.Release(locationSpecs);

            var musicSoundsGuids = new NativeHashSet<GUID>(64, allocator);
            var ambienceSoundsGuids = new NativeHashSet<GUID>(64, allocator);
            var mapItemsSoundsGuids = new NativeHashSet<GUID>(64, allocator);
            GetSoundsInOpenScenes(allLoadedScenes, musicSoundsGuids, ambienceSoundsGuids, mapItemsSoundsGuids);
            if (scenePath == ApplicationScenePath) {
                TryGetSoundsInApplicationScene(musicSoundsGuids, ambienceSoundsGuids);
            }

            if (musicSoundsGuids.Count == 0 || (musicSoundsGuids.Count == 1 && musicSoundsGuids.GetFirstAndOnlyEntry() == default)) {
                musicSoundsGuids.Dispose();
            } else {
                scenePathToMusicSoundsGuidsMap.Add(scenePath, musicSoundsGuids);
            }

            if (ambienceSoundsGuids.Count == 0 || (ambienceSoundsGuids.Count == 1 && ambienceSoundsGuids.GetFirstAndOnlyEntry() == default)) {
                ambienceSoundsGuids.Dispose();
            } else {
                scenePathToAmbienceSoundsGuidsMap.Add(scenePath, ambienceSoundsGuids);
            }

            if (mapItemsSoundsGuids.Count == 0 || (mapItemsSoundsGuids.Count == 1 && mapItemsSoundsGuids.GetFirstAndOnlyEntry() == default)) {
                mapItemsSoundsGuids.Dispose();
            } else {
                scenePathToMapItemsSoundsGuidsMap.Add(scenePath, mapItemsSoundsGuids);
            }
        }

        static void SearchForSoundsInPrefabs(string folderPath, Func<GameObject, bool> isPrefabValidFunc, HashSet<string> excludedPaths,
            Dictionary<string, NativeHashSet<GUID>> prefabPathToSoundsGuids = null, Allocator prefabPathToSoundsGuidsValueAllocator = Allocator.Invalid,
            NativeHashSet<GUID> allSoundsGuids = default,
            bool searchAliveComponents = false, bool searchUIComponents = false) {
            if ((prefabPathToSoundsGuids == null || prefabPathToSoundsGuidsValueAllocator <= Allocator.None) && allSoundsGuids.IsCreated == false) {
                Log.Important?.Error($"Incorrect use of {nameof(SearchForSoundsInPrefabs)}. You should provide valid {prefabPathToSoundsGuids} with valid allocator or valid {allSoundsGuids}");
                return;
            }

            // Disable previews because it spawns prefab inside prefab which we don't want 
            var prevAllPreviewsEnabled = LocationSpec.AllPreviewsEnabled;
            LocationSpec.AllPreviewsEnabled = false;
            var prevLocationPreviewsEnabled = LocationSpec.LocationPreviewsEnabled;
            LocationSpec.LocationPreviewsEnabled = false;

            var studioEventEmitters = new List<StudioEventEmitter>(2);
            var aliveAudioAttachments = new List<AliveAudioAttachment>(2);
            var crittersSpawners = new List<CritterSpawnerAttachment>(2);
            var digOutAttachments = new List<DigOutAttachment>(2);
            var ogreReelPusherFootSteps = new List<OgreReelPusherFootSteps>(2);
            var itemsAudioAttachments = new List<ItemAudioAttachment>(2);
            var itemSlotUIs = new List<ItemSlotUI>(2);
            var vCharacterSheetUIs = new List<VCharacterSheetUI>(2);
            var vHeroStorageTabUIs = new List<VHeroStorageTabUI>(2);
            var vLockpickings = new List<VLockpicking>(2);
            var vShopVendorBaseUIs = new List<VShopVendorBaseUI>(2);
            var arButtons = new List<ARButton>(2);
            var advancedNotificationsViews = new List<IAdvancedNotificationsView>(2);
            var vDeathTeleportUIs = new List<VDeathTeleportUI>(2);
            var vDeathUIs = new List<VDeathUI>(2);
            var vMenuUIs = new List<VMenuUI>(2);
            var vReadablePopupUIs = new List<VReadablePopupUI>(2);
            var vSketchPopupUIs = new List<VSketchPopupUI>(2);
            var vWaitForInputBoards = new List<VWaitForInputBoard>(2);
            var vBagUIs = new List<VBagUI>(2);
            var vCStatBarWithFails = new List<VCStatBarWithFail>(2);
            var vCGravityZoneCheckers = new List<VCGravityZoneChecker>(2);
            var vQuestTrackers = new List<VQuestTracker>(2);
            var simpleInteractions = new List<SimpleInteraction>(2);
            var doorsAttachments = new List<DoorsAttachment>(2);
            var interactAttachments = new List<InteractAttachment>(2);
            var lockAttachments = new List<LockAttachment>(2);
            var logicEmitterAttachmentBases = new List<LogicEmitterAttachmentBase>(2);
            var lumberingAttachments = new List<LumberingAttachment>(2);

            var prefabsGuids = GetPrefabsGuids(folderPath);
            foreach (string prefabGuid in prefabsGuids) {
                var path = AssetDatabase.GUIDToAssetPath(prefabGuid);
                if (excludedPaths != null && excludedPaths.Contains(path)) {
                    continue;
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (isPrefabValidFunc != null && isPrefabValidFunc(prefab) == false) {
                    continue;
                }

                if (prefab.TryGetComponent<LocationSpec>(out var locationSpec)) {
                    locationSpec.ValidatePrefab(true);
                }

                NativeHashSet<GUID> soundsGuids;
                if (prefabPathToSoundsGuids != null) {
                    if (prefabPathToSoundsGuids.TryGetValue(path, out soundsGuids) == false) {
                        soundsGuids = new NativeHashSet<GUID>(2, prefabPathToSoundsGuidsValueAllocator);
                        prefabPathToSoundsGuids.Add(path, soundsGuids);
                    }
                } else {
                    soundsGuids = allSoundsGuids;
                }

                studioEventEmitters.Clear();
                AddPrefabEventEmittersSoundsGuids(prefab, studioEventEmitters, soundsGuids);

                AddItemsSoundsGuids(soundsGuids, prefab, itemsAudioAttachments, vCGravityZoneCheckers, simpleInteractions,
                    doorsAttachments, interactAttachments, lockAttachments, logicEmitterAttachmentBases, lumberingAttachments);

                if (searchAliveComponents) {
                    AddAliveSoundsGuids(soundsGuids, prefab, aliveAudioAttachments, crittersSpawners, ogreReelPusherFootSteps, digOutAttachments);
                }

                if (searchUIComponents) {
                    AddUISounds(soundsGuids, prefab, itemSlotUIs, vCharacterSheetUIs, vHeroStorageTabUIs, vLockpickings, vShopVendorBaseUIs, arButtons, advancedNotificationsViews,
                        vDeathTeleportUIs, vDeathUIs, vMenuUIs, vReadablePopupUIs, vSketchPopupUIs, vWaitForInputBoards, vQuestTrackers, vBagUIs, vCStatBarWithFails);
                }

                if (prefabPathToSoundsGuids != null &&
                    (soundsGuids.Count == 0 || (soundsGuids.Count == 1 && soundsGuids.GetFirstAndOnlyEntry() == default))) {
                    prefabPathToSoundsGuids.Remove(path);
                }
            }

            LocationSpec.AllPreviewsEnabled = prevAllPreviewsEnabled;
            LocationSpec.LocationPreviewsEnabled = prevLocationPreviewsEnabled;
        }

        static void AddPrefabEventEmittersSoundsGuids(GameObject prefab, List<StudioEventEmitter> studioEventEmitters, NativeHashSet<GUID> soundsGuids) {
            prefab.GetComponentsInChildren(true, studioEventEmitters);
            foreach (var studioEventEmitter in studioEventEmitters) {
                if (studioEventEmitter.EventReference.Guid.IsNull == false) {
                    if (IsPrefabInPrefab(prefab, studioEventEmitter.gameObject)) {
                        continue;
                    }

                    soundsGuids.Add(studioEventEmitter.EventReference.Guid);
                }
            }
        }

        static void TryGetSoundsInApplicationScene(NativeHashSet<GUID> musicSoundsGuids, NativeHashSet<GUID> ambienceSoundsGuids) {
            var audioCore = Object.FindFirstObjectByType<AudioCore>();
            if (audioCore != null) {
                musicSoundsGuids.Add(audioCore.worldExplorationMusicDefault.Guid);
                ambienceSoundsGuids.Add(audioCore.rainEventEmitter.EventReference.Guid);
            }

            var audioBiome = Object.FindFirstObjectByType<AudioBiome>();
            AddAudioBiomeSoundsGuids(audioBiome, musicSoundsGuids, ambienceSoundsGuids);
        }

        static void GetSoundsInOpenScenes(IReadOnlyList<Scene> allLoadedScenes, NativeHashSet<GUID> musicSoundsGuids, NativeHashSet<GUID> ambienceSoundsGuids,
            NativeHashSet<GUID> itemsSoundsGuids) {
            var mapScene = Object.FindFirstObjectByType<MapScene>();
            AddMapSceneComponentSoundsGuids(mapScene, musicSoundsGuids, ambienceSoundsGuids);

            var wyrdnessAudioProviders = Object.FindObjectsByType<WyrdnessAudioProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var wyrdnessAudioProvider in wyrdnessAudioProviders) {
                AddWyrdnessProviderSoundsGuids(wyrdnessAudioProvider, musicSoundsGuids, ambienceSoundsGuids);
            }
            
            var additiveScenes = Object.FindObjectsByType<AdditiveScene>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var additiveScene in additiveScenes) {
                AddAdditiveSceneSoundsGuids(additiveScene, musicSoundsGuids, ambienceSoundsGuids);
            }

            //Using this because FindObjectsByType is not finding locations preview instances
            var mapEventEmitters = GameObjects.FindComponentsByTypeInScenes<StudioEventEmitter>(allLoadedScenes, true);
            foreach (var mapEventEmitter in mapEventEmitters) {
                AddMapEventEmittersSoundsGuids(mapEventEmitter, ambienceSoundsGuids, itemsSoundsGuids);
            }

            ListPool<StudioEventEmitter>.Release(mapEventEmitters);

            var flockGroups = Object.FindObjectsByType<FlockGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var flockGroup in flockGroups) {
                AddFlockGroupSoundsGuids(ambienceSoundsGuids, flockGroup);
            }

            var startingDungeonArrows = GameObjects.FindComponentsByTypeInScenes<StartingDungeonArrow>(allLoadedScenes, true, 1);
            foreach (var startingDungeonArrow in startingDungeonArrows) {
                AddStartingDungeonArrowSounds(startingDungeonArrow, itemsSoundsGuids);
            }

            ListPool<StartingDungeonArrow>.Release(startingDungeonArrows);


            var simpleInteractions = Object.FindObjectsByType<SimpleInteraction>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var simpleInteraction in simpleInteractions) {
                itemsSoundsGuids.Add(simpleInteraction.interactionSFX.Guid);
            }

            var audioBiomes = Object.FindObjectsByType<AudioBiome>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var audioBiome in audioBiomes) {
                AddAudioBiomeSoundsGuids(audioBiome, musicSoundsGuids, ambienceSoundsGuids);
            }

            var doorAttachments = Object.FindObjectsByType<DoorsAttachment>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var doorAttachment in doorAttachments) {
                itemsSoundsGuids.Add(doorAttachment.openSound.Guid);
                itemsSoundsGuids.Add(doorAttachment.closeSound.Guid);
            }

            var interactAttachments = Object.FindObjectsByType<InteractAttachment>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var interactAttachment in interactAttachments) {
                itemsSoundsGuids.Add(interactAttachment.interactionSound.Guid);
            }

            var lockAttachments = Object.FindObjectsByType<LockAttachment>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var lockAttachment in lockAttachments) {
                itemsSoundsGuids.Add(lockAttachment.unlockSound.Guid);
            }

            var logicEmitterAttachmentBases = Object.FindObjectsByType<LogicEmitterAttachmentBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var logicEmitterAttachmentBase in logicEmitterAttachmentBases) {
                itemsSoundsGuids.Add(logicEmitterAttachmentBase.interactionSound.Guid);
                itemsSoundsGuids.Add(logicEmitterAttachmentBase.inactiveInteractionSound.Guid);
            }

            var lumberingAttachments = Object.FindObjectsByType<LumberingAttachment>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var lumberingAttachment in lumberingAttachments) {
                itemsSoundsGuids.Add(lumberingAttachment.hitSound.Guid);
            }
        }

        static void AddMapSceneComponentSoundsGuids(MapScene mapScene, NativeHashSet<GUID> musicSoundsGuids, NativeHashSet<GUID> ambienceSoundsGuids) {
            if (mapScene == null) {
                return;
            }

            if (mapScene.audioSceneSet is {} set) {
                foreach (var source in set.musicAudioSources) {
                    musicSoundsGuids.Add(source.EventReference().Guid);
                }
                
                foreach (var source in set.musicAlertAudioSources) {
                    musicSoundsGuids.Add(source.EventReference().Guid);
                }
                
                foreach (var source in set.musicCombatAudioSources) {
                    musicSoundsGuids.Add(source.EventReference().Guid);
                }
                
                ambienceSoundsGuids.Add(set.ambientAudioSource.EventReference().Guid);
                ambienceSoundsGuids.Add(set.snapshotAudioSource.EventReference().Guid);
                return;
            }

            foreach (var mapMusicAudioSource in mapScene.musicAudioSources) {
                if (mapMusicAudioSource != null)
                    musicSoundsGuids.Add(mapMusicAudioSource.EventReference().Guid);
            }

            foreach (var mapMusicAlertAudioSource in mapScene.musicAlertAudioSources) {
                if (mapMusicAlertAudioSource != null)
                    musicSoundsGuids.Add(mapMusicAlertAudioSource.EventReference().Guid);
            }

            foreach (var mapMusicCombatAudioSource in mapScene.musicCombatAudioSources) {
                if (mapMusicCombatAudioSource != null)
                    musicSoundsGuids.Add(mapMusicCombatAudioSource.EventReference().Guid);
            }

            if (mapScene.ambientAudioSource != null)
                ambienceSoundsGuids.Add(mapScene.ambientAudioSource.EventReference().Guid);

            if (mapScene.snapshotAudioSource != null)
                ambienceSoundsGuids.Add(mapScene.snapshotAudioSource.EventReference().Guid);
        }

        static void AddWyrdnessProviderSoundsGuids(WyrdnessAudioProvider wyrdnessAudioProvider, NativeHashSet<GUID> musicSoundsGuids, NativeHashSet<GUID> ambienceSoundsGuids) {
            if (wyrdnessAudioProvider == null) {
                return;
            }

            foreach (var source in wyrdnessAudioProvider.musicToRegister) {
                musicSoundsGuids.Add(source.EventReference().Guid);
            }
                
            foreach (var source in wyrdnessAudioProvider.alertMusicToRegister) {
                musicSoundsGuids.Add(source.EventReference().Guid);
            }
                
            foreach (var source in wyrdnessAudioProvider.combatMusicToRegister) {
                musicSoundsGuids.Add(source.EventReference().Guid);
            }
            
            foreach (var source in wyrdnessAudioProvider.ambientsToRegister) {
                ambienceSoundsGuids.Add(source.EventReference().Guid);
            }
            
            foreach (var source in wyrdnessAudioProvider.snapshotsToRegister) {
                ambienceSoundsGuids.Add(source.EventReference().Guid);
            }
        }

        static void AddAdditiveSceneSoundsGuids(AdditiveScene additiveScene, NativeHashSet<GUID> musicSoundsGuids, NativeHashSet<GUID> ambienceSoundsGuids) {
            musicSoundsGuids.Add(additiveScene.musicAudioSource.EventReference().Guid);
            foreach (var audioSource in additiveScene.musicAudioSources) {
                if (audioSource != null)
                    musicSoundsGuids.Add(audioSource.EventReference().Guid);
            }

            foreach (var audioSource in additiveScene.musicAlertAudioSources) {
                if (audioSource != null)
                    musicSoundsGuids.Add(audioSource.EventReference().Guid);
            }

            foreach (var audioSource in additiveScene.musicCombatAudioSources) {
                if (audioSource != null)
                    musicSoundsGuids.Add(audioSource.EventReference().Guid);
            }

            if (additiveScene.ambientAudioSource != null)
                ambienceSoundsGuids.Add(additiveScene.ambientAudioSource.EventReference().Guid);
            if (additiveScene.snapshotAudioSource != null)
                ambienceSoundsGuids.Add(additiveScene.snapshotAudioSource.EventReference().Guid);
        }

        static void AddMapEventEmittersSoundsGuids(StudioEventEmitter mapEventEmitter, NativeHashSet<GUID> ambienceSoundsGuids,
            NativeHashSet<GUID> itemsSoundsGuids) {
            var locationSpec = mapEventEmitter.GetComponentInParent<LocationSpec>(true);
            if (locationSpec != null) {
                itemsSoundsGuids.Add(mapEventEmitter.EventReference.Guid);
            } else {
                ambienceSoundsGuids.Add(mapEventEmitter.EventReference.Guid);
            }
        }

        static void AddFlockGroupSoundsGuids(NativeHashSet<GUID> soundsGuids, FlockGroup flockGroup) {
            soundsGuids.Add(flockGroup.groupFlyingEvent.Guid);
            soundsGuids.Add(flockGroup.groupRestingEvent.Guid);
            soundsGuids.Add(flockGroup.groupTakeOffEvent.Guid);
            soundsGuids.Add(flockGroup.restingSoundEvent.Guid);
            soundsGuids.Add(flockGroup.flyingSoundEvent.Guid);
            soundsGuids.Add(flockGroup.landEvent.Guid);
            soundsGuids.Add(flockGroup.takeOffEvent.Guid);
        }

        static void AddAliveSoundsGuids(NativeHashSet<GUID> aliveSoundsGuids, GameObject prefab, List<AliveAudioAttachment> aliveAudioAttachments,
            List<CritterSpawnerAttachment> critterSpawners, List<OgreReelPusherFootSteps> ogreReelPusherFootSteps, List<DigOutAttachment> digOutAttachments) {
            aliveAudioAttachments.Clear();

            prefab.GetComponentsInChildren(true, aliveAudioAttachments);
            foreach (var aliveAudioAttachment in aliveAudioAttachments) {
                if (IsPrefabInPrefab(prefab, aliveAudioAttachment.gameObject)) {
                    continue;
                }

                AddAliveAudioContainerSounds(aliveAudioAttachment.aliveAudioContainerWrapper?.Data, aliveSoundsGuids);
                AddAliveAudioContainerSounds(aliveAudioAttachment.wyrdConvertedAudioContainerWrapper?.Data, aliveSoundsGuids);
            }

            critterSpawners.Clear();
            prefab.GetComponentsInChildren(true, critterSpawners);
            foreach (var critterSpawner in critterSpawners) {
                if (IsPrefabInPrefab(prefab, critterSpawner.gameObject)) {
                    continue;
                }

                AddCrittersSounds(critterSpawner, aliveSoundsGuids);
            }

            ogreReelPusherFootSteps.Clear();
            prefab.GetComponentsInChildren(true, ogreReelPusherFootSteps);
            foreach (var ogreReelPusherFootStepsComponent in ogreReelPusherFootSteps) {
                if (IsPrefabInPrefab(prefab, ogreReelPusherFootStepsComponent.gameObject)) {
                    continue;
                }

                aliveSoundsGuids.Add(ogreReelPusherFootStepsComponent.footStepEvent.Guid);
            }

            digOutAttachments.Clear();
            prefab.GetComponentsInChildren(true, digOutAttachments);
            foreach (var digOutAttachment in digOutAttachments) {
                if (IsPrefabInPrefab(prefab, digOutAttachment.gameObject)) {
                    continue;
                }

                aliveSoundsGuids.Add(digOutAttachment.digUpSound.Guid);
            }
        }

        static void AddItemsSoundsGuids(NativeHashSet<GUID> itemsSoundsGuids, GameObject prefab, List<ItemAudioAttachment> itemsAudioAttachments,
            List<VCGravityZoneChecker> vCGravityZoneCheckers,
            List<SimpleInteraction> simpleInteractions, List<DoorsAttachment> doorsAttachments, List<InteractAttachment> interactAttachments,
            List<LockAttachment> lockAttachments, List<LogicEmitterAttachmentBase> logicEmitterAttachmentBases, List<LumberingAttachment> lumberingAttachments) {
            itemsAudioAttachments.Clear();
            prefab.GetComponentsInChildren(true, itemsAudioAttachments);
            foreach (var itemAudioAttachment in itemsAudioAttachments) {
                if (IsPrefabInPrefab(prefab, itemAudioAttachment.gameObject)) {
                    continue;
                }

                AddItemAudioContainerSounds(itemAudioAttachment.itemAudioContainerWrapper?.Data, itemsSoundsGuids);
            }

            vCGravityZoneCheckers.Clear();
            prefab.GetComponentsInChildren(true, vCGravityZoneCheckers);
            foreach (var vCGravityZoneChecker in vCGravityZoneCheckers) {
                if (IsPrefabInPrefab(prefab, vCGravityZoneChecker.gameObject)) {
                    continue;
                }

                itemsSoundsGuids.Add(vCGravityZoneChecker.onEnterGravityField.Guid);
                itemsSoundsGuids.Add(vCGravityZoneChecker.onExitGravityField.Guid);
            }

            simpleInteractions.Clear();
            prefab.GetComponentsInChildren(true, simpleInteractions);
            foreach (var simpleInteraction in simpleInteractions) {
                if (IsPrefabInPrefab(prefab, simpleInteraction.gameObject)) {
                    continue;
                }

                itemsSoundsGuids.Add(simpleInteraction.interactionSFX.Guid);
            }

            doorsAttachments.Clear();
            prefab.GetComponentsInChildren(true, doorsAttachments);
            foreach (var doorsAttachmentsComponent in doorsAttachments) {
                if (IsPrefabInPrefab(prefab, doorsAttachmentsComponent.gameObject)) {
                    continue;
                }

                itemsSoundsGuids.Add(doorsAttachmentsComponent.openSound.Guid);
                itemsSoundsGuids.Add(doorsAttachmentsComponent.closeSound.Guid);
            }

            interactAttachments.Clear();
            prefab.GetComponentsInChildren(true, interactAttachments);
            foreach (var interactAttachment in interactAttachments) {
                if (IsPrefabInPrefab(prefab, interactAttachment.gameObject)) {
                    continue;
                }

                itemsSoundsGuids.Add(interactAttachment.interactionSound.Guid);
            }

            lockAttachments.Clear();
            prefab.GetComponentsInChildren(true, lockAttachments);
            foreach (var lockAttachment in lockAttachments) {
                if (IsPrefabInPrefab(prefab, lockAttachment.gameObject)) {
                    continue;
                }

                itemsSoundsGuids.Add(lockAttachment.unlockSound.Guid);
            }

            logicEmitterAttachmentBases.Clear();
            prefab.GetComponentsInChildren(true, logicEmitterAttachmentBases);
            foreach (var logicEmitterAttachmentBase in logicEmitterAttachmentBases) {
                if (IsPrefabInPrefab(prefab, logicEmitterAttachmentBase.gameObject)) {
                    continue;
                }

                itemsSoundsGuids.Add(logicEmitterAttachmentBase.interactionSound.Guid);
                itemsSoundsGuids.Add(logicEmitterAttachmentBase.inactiveInteractionSound.Guid);
            }

            lumberingAttachments.Clear();
            prefab.GetComponentsInChildren(true, lumberingAttachments);
            foreach (var lumberingAttachment in lumberingAttachments) {
                if (IsPrefabInPrefab(prefab, lumberingAttachment.gameObject)) {
                    continue;
                }

                itemsSoundsGuids.Add(lumberingAttachment.hitSound.Guid);
            }
        }

        static void AddUISounds(NativeHashSet<GUID> uiSoundsGuids, GameObject prefab, List<ItemSlotUI> itemSlotUIs, List<VCharacterSheetUI> vCharacterSheetUIs,
            List<VHeroStorageTabUI> vHeroStorageTabUIs, List<VLockpicking> vLockpickings, List<VShopVendorBaseUI> vShopVendorBaseUIs, List<ARButton> arButtons,
            List<IAdvancedNotificationsView> advancedNotificationsViews, List<VDeathTeleportUI> vDeathTeleportUIs, List<VDeathUI> vDeathUIs, List<VMenuUI> vMenuUIs,
            List<VReadablePopupUI> vReadablePopupUIs, List<VSketchPopupUI> vSketchPopupUIs, List<VWaitForInputBoard> vWaitForInputBoards, List<VQuestTracker> vQuestTrackers,
            List<VBagUI> vBagUIs, List<VCStatBarWithFail> vCStatBarWithFails) {
            itemSlotUIs.Clear();
            prefab.GetComponentsInChildren(true, itemSlotUIs);
            foreach (var itemSlotUI in itemSlotUIs) {
                if (IsPrefabInPrefab(prefab, itemSlotUI.gameObject)) {
                    continue;
                }

                uiSoundsGuids.Add(itemSlotUI.hoverSound.Guid);
            }

            vCharacterSheetUIs.Clear();
            prefab.GetComponentsInChildren(true, vCharacterSheetUIs);
            foreach (var vCharacterSheetUI in vCharacterSheetUIs) {
                if (IsPrefabInPrefab(prefab, vCharacterSheetUI.gameObject)) {
                    continue;
                }

                uiSoundsGuids.Add(vCharacterSheetUI.OpenSound.Guid);
                uiSoundsGuids.Add(vCharacterSheetUI.ExitSound.Guid);
            }

            vHeroStorageTabUIs.Clear();
            prefab.GetComponentsInChildren(true, vHeroStorageTabUIs);
            foreach (var vHeroStorageTabUI in vHeroStorageTabUIs) {
                if (IsPrefabInPrefab(prefab, vHeroStorageTabUI.gameObject)) {
                    continue;
                }

                uiSoundsGuids.Add(vHeroStorageTabUI.putSfx.Guid);
            }

            vLockpickings.Clear();
            prefab.GetComponentsInChildren(true, vLockpickings);
            foreach (var lockpicking in vLockpickings) {
                if (IsPrefabInPrefab(prefab, lockpicking.gameObject)) {
                    continue;
                }

                AddVLockpickingSounds(lockpicking, uiSoundsGuids);
            }

            vShopVendorBaseUIs.Clear();
            prefab.GetComponentsInChildren(true, vShopVendorBaseUIs);
            foreach (var vShopVendorBaseUI in vShopVendorBaseUIs) {
                if (IsPrefabInPrefab(prefab, vShopVendorBaseUI.gameObject)) {
                    continue;
                }

                uiSoundsGuids.Add(vShopVendorBaseUI.sellSfx.Guid);
                uiSoundsGuids.Add(vShopVendorBaseUI.cantAffordSfx.Guid);
            }

            arButtons.Clear();
            prefab.GetComponentsInChildren(true, arButtons);
            foreach (var arButton in arButtons) {
                if (IsPrefabInPrefab(prefab, arButton.gameObject)) {
                    continue;
                }

                uiSoundsGuids.Add(arButton.clickSound.Guid);
                uiSoundsGuids.Add(arButton.selectedSound.Guid);
                uiSoundsGuids.Add(arButton.clickInactiveSound.Guid);
            }

            advancedNotificationsViews.Clear();
            prefab.GetComponentsInChildren(true, advancedNotificationsViews);
            foreach (var advancedNotificationsView in advancedNotificationsViews) {
                if (IsPrefabInPrefab(prefab, (advancedNotificationsView as View)?.gameObject)) {
                    continue;
                }

                uiSoundsGuids.Add(advancedNotificationsView.NotificationSound.Guid);
            }

            vDeathTeleportUIs.Clear();
            prefab.GetComponentsInChildren(true, vDeathTeleportUIs);
            foreach (var vDeathTeleportUI in vDeathTeleportUIs) {
                if (IsPrefabInPrefab(prefab, vDeathTeleportUI.gameObject)) {
                    continue;
                }

                uiSoundsGuids.Add(vDeathTeleportUI.jailSound.Guid);
            }

            vDeathUIs.Clear();
            prefab.GetComponentsInChildren(true, vDeathUIs);
            foreach (var vDeathUI in vDeathUIs) {
                if (IsPrefabInPrefab(prefab, vDeathUI.gameObject)) {
                    continue;
                }

                uiSoundsGuids.Add(vDeathUI.deathSound.Guid);
            }

            vMenuUIs.Clear();
            prefab.GetComponentsInChildren(true, vMenuUIs);
            foreach (var vMenuUI in vMenuUIs) {
                if (IsPrefabInPrefab(prefab, vMenuUI.gameObject)) {
                    continue;
                }

                uiSoundsGuids.Add(vMenuUI.openMenuSound.Guid);
                uiSoundsGuids.Add(vMenuUI.closeMenuSound.Guid);
            }

            vReadablePopupUIs.Clear();
            prefab.GetComponentsInChildren(true, vReadablePopupUIs);
            foreach (var vReadablePopupUI in vReadablePopupUIs) {
                if (IsPrefabInPrefab(prefab, vReadablePopupUI.gameObject)) {
                    continue;
                }

                uiSoundsGuids.Add(vReadablePopupUI.closeSound.Guid);
            }

            vSketchPopupUIs.Clear();
            prefab.GetComponentsInChildren(true, vSketchPopupUIs);
            foreach (var vSketchPopupUI in vSketchPopupUIs) {
                if (IsPrefabInPrefab(prefab, vSketchPopupUI.gameObject)) {
                    continue;
                }

                uiSoundsGuids.Add(vSketchPopupUI.closeSound.Guid);
            }

            vWaitForInputBoards.Clear();
            prefab.GetComponentsInChildren(true, vWaitForInputBoards);
            foreach (var vWaitForInputBoard in vWaitForInputBoards) {
                if (IsPrefabInPrefab(prefab, vWaitForInputBoard.gameObject)) {
                    continue;
                }

                uiSoundsGuids.Add(vWaitForInputBoard.videoHandle?.videoAudio.Guid ?? default(GUID));
            }

            vBagUIs.Clear();
            prefab.GetComponentsInChildren(true, vBagUIs);
            foreach (var vBagUI in vBagUIs) {
                if (IsPrefabInPrefab(prefab, vBagUI.gameObject)) {
                    continue;
                }

                uiSoundsGuids.Add(vBagUI.DropHoldSound.Guid);
            }

            vCStatBarWithFails.Clear();
            prefab.GetComponentsInChildren(true, vCStatBarWithFails);
            foreach (var VCStatBarWithFail in vCStatBarWithFails) {
                if (IsPrefabInPrefab(prefab, VCStatBarWithFail.gameObject)) {
                    continue;
                }

                uiSoundsGuids.Add(VCStatBarWithFail.failSound.Guid);
            }
        }

        static bool IsPrefabInPrefab(GameObject prefabRoot, GameObject checkedGO) {
            if (checkedGO == null) {
                return false;
            }

            if (prefabRoot == checkedGO) {
                return false;
            }

            GameObject checkGOSource = PrefabUtility.GetCorrespondingObjectFromSource(checkedGO);
            return checkGOSource != null && checkGOSource != prefabRoot;
        }

        static void AddItemAudioContainerSounds(ItemAudioContainer itemAudioContainer, NativeHashSet<GUID> itemsSoundsGuids) {
            if (itemAudioContainer == null) {
                return;
            }

            itemsSoundsGuids.Add(itemAudioContainer.meleeSwing.Guid);
            itemsSoundsGuids.Add(itemAudioContainer.meleeSwingHeavy.Guid);
            itemsSoundsGuids.Add(itemAudioContainer.meleeEquip.Guid);
            itemsSoundsGuids.Add(itemAudioContainer.meleeUnEquip.Guid);
            itemsSoundsGuids.Add(itemAudioContainer.meleeHit.Guid);
            itemsSoundsGuids.Add(itemAudioContainer.dragBow.Guid);
            itemsSoundsGuids.Add(itemAudioContainer.equipBow.Guid);
            itemsSoundsGuids.Add(itemAudioContainer.unEquipBow.Guid);
            itemsSoundsGuids.Add(itemAudioContainer.releaseBow.Guid);
            itemsSoundsGuids.Add(itemAudioContainer.arrowSwish.Guid);
            itemsSoundsGuids.Add(itemAudioContainer.castBegun.Guid);
            itemsSoundsGuids.Add(itemAudioContainer.castCharging.Guid);
            itemsSoundsGuids.Add(itemAudioContainer.castCancel.Guid);
            itemsSoundsGuids.Add(itemAudioContainer.castRelease.Guid);
            itemsSoundsGuids.Add(itemAudioContainer.equipMagic.Guid);
            itemsSoundsGuids.Add(itemAudioContainer.unEquipMagic.Guid);
            itemsSoundsGuids.Add(itemAudioContainer.magicHit.Guid);
            itemsSoundsGuids.Add(itemAudioContainer.sheathe.Guid);
            itemsSoundsGuids.Add(itemAudioContainer.unsheathe.Guid);
            itemsSoundsGuids.Add(itemAudioContainer.onBlockDamage.Guid);
            itemsSoundsGuids.Add(itemAudioContainer.footStep.Guid);
            itemsSoundsGuids.Add(itemAudioContainer.bodyMovement.Guid);
            itemsSoundsGuids.Add(itemAudioContainer.bodyMovementFast.Guid);
            itemsSoundsGuids.Add(itemAudioContainer.equipArmor.Guid);
            itemsSoundsGuids.Add(itemAudioContainer.unEquipArmor.Guid);
            itemsSoundsGuids.Add(itemAudioContainer.useItem.Guid);
            itemsSoundsGuids.Add(itemAudioContainer.pickupItem.Guid);
            itemsSoundsGuids.Add(itemAudioContainer.dropItem.Guid);
            itemsSoundsGuids.Add(itemAudioContainer.specialAttackSwing.Guid);
        }

        static void AddItemAudioAttachmentSoundsEvents(ItemAudioContainer itemAudioContainer, HashSet<EventReference> itemsSoundsEvents) {
            if (itemAudioContainer == null) {
                return;
            }

            itemsSoundsEvents.Add(itemAudioContainer.meleeSwing);
            itemsSoundsEvents.Add(itemAudioContainer.meleeSwingHeavy);
            itemsSoundsEvents.Add(itemAudioContainer.meleeEquip);
            itemsSoundsEvents.Add(itemAudioContainer.meleeUnEquip);
            itemsSoundsEvents.Add(itemAudioContainer.meleeHit);
            itemsSoundsEvents.Add(itemAudioContainer.dragBow);
            itemsSoundsEvents.Add(itemAudioContainer.equipBow);
            itemsSoundsEvents.Add(itemAudioContainer.unEquipBow);
            itemsSoundsEvents.Add(itemAudioContainer.releaseBow);
            itemsSoundsEvents.Add(itemAudioContainer.arrowSwish);
            itemsSoundsEvents.Add(itemAudioContainer.castBegun);
            itemsSoundsEvents.Add(itemAudioContainer.castCharging);
            itemsSoundsEvents.Add(itemAudioContainer.castCancel);
            itemsSoundsEvents.Add(itemAudioContainer.castRelease);
            itemsSoundsEvents.Add(itemAudioContainer.equipMagic);
            itemsSoundsEvents.Add(itemAudioContainer.unEquipMagic);
            itemsSoundsEvents.Add(itemAudioContainer.magicHit);
            itemsSoundsEvents.Add(itemAudioContainer.sheathe);
            itemsSoundsEvents.Add(itemAudioContainer.unsheathe);
            itemsSoundsEvents.Add(itemAudioContainer.onBlockDamage);
            itemsSoundsEvents.Add(itemAudioContainer.footStep);
            itemsSoundsEvents.Add(itemAudioContainer.bodyMovement);
            itemsSoundsEvents.Add(itemAudioContainer.bodyMovementFast);
            itemsSoundsEvents.Add(itemAudioContainer.equipArmor);
            itemsSoundsEvents.Add(itemAudioContainer.unEquipArmor);
            itemsSoundsEvents.Add(itemAudioContainer.useItem);
            itemsSoundsEvents.Add(itemAudioContainer.pickupItem);
            itemsSoundsEvents.Add(itemAudioContainer.dropItem);
            itemsSoundsEvents.Add(itemAudioContainer.specialAttackSwing);
        }

        static void ReplaceItemAudioContainerInvalidSounds(ItemAudioAttachment itemAudioAttachment, Dictionary<GUID, (GUID validEventGuid, string validEventPath)> invalidGuidToValidData) {
            if (itemAudioAttachment.itemAudioContainerWrapper?.Data == null) {
                return;
            }

            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.meleeSwing = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.meleeSwing);
            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.meleeSwingHeavy = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.meleeSwingHeavy);
            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.meleeEquip = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.meleeEquip);
            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.meleeUnEquip = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.meleeUnEquip);
            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.meleeHit = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.meleeHit);
            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.dragBow = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.dragBow);
            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.equipBow = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.equipBow);
            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.unEquipBow = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.unEquipBow);
            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.releaseBow = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.releaseBow);
            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.arrowSwish = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.arrowSwish);
            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.castBegun = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.castBegun);
            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.castCharging = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.castCharging);
            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.castCancel = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.castCancel);
            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.castRelease = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.castRelease);
            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.equipMagic = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.equipMagic);
            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.unEquipMagic = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.unEquipMagic);
            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.magicHit = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.magicHit);
            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.sheathe = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.sheathe);
            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.unsheathe = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.unsheathe);
            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.onBlockDamage = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.onBlockDamage);
            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.footStep = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.footStep);
            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.bodyMovement = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.bodyMovement);
            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.bodyMovementFast = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.bodyMovementFast);
            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.equipArmor = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.equipArmor);
            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.unEquipArmor = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.unEquipArmor);
            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.useItem = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.useItem);
            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.pickupItem = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.pickupItem);
            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.dropItem = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.dropItem);
            itemAudioAttachment.itemAudioContainerWrapper.EmbeddedDataRef.specialAttackSwing = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioAttachment.itemAudioContainerWrapper.Data.specialAttackSwing);
        }

        static void ReplaceItemAudioContainerInvalidSounds(ItemAudioContainerAsset itemAudioContainerAsset, Dictionary<GUID, (GUID validEventGuid, string validEventPath)> invalidGuidToValidData) {
            if (itemAudioContainerAsset?.audioContainer == null) {
                return;
            }

            itemAudioContainerAsset.audioContainer.meleeSwing = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.meleeSwing);
            itemAudioContainerAsset.audioContainer.meleeSwingHeavy = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.meleeSwingHeavy);
            itemAudioContainerAsset.audioContainer.meleeEquip = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.meleeEquip);
            itemAudioContainerAsset.audioContainer.meleeUnEquip = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.meleeUnEquip);
            itemAudioContainerAsset.audioContainer.meleeHit = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.meleeHit);
            itemAudioContainerAsset.audioContainer.dragBow = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.dragBow);
            itemAudioContainerAsset.audioContainer.equipBow = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.equipBow);
            itemAudioContainerAsset.audioContainer.unEquipBow = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.unEquipBow);
            itemAudioContainerAsset.audioContainer.releaseBow = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.releaseBow);
            itemAudioContainerAsset.audioContainer.arrowSwish = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.arrowSwish);
            itemAudioContainerAsset.audioContainer.castBegun = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.castBegun);
            itemAudioContainerAsset.audioContainer.castCharging = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.castCharging);
            itemAudioContainerAsset.audioContainer.castCancel = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.castCancel);
            itemAudioContainerAsset.audioContainer.castRelease = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.castRelease);
            itemAudioContainerAsset.audioContainer.equipMagic = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.equipMagic);
            itemAudioContainerAsset.audioContainer.unEquipMagic = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.unEquipMagic);
            itemAudioContainerAsset.audioContainer.magicHit = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.magicHit);
            itemAudioContainerAsset.audioContainer.sheathe = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.sheathe);
            itemAudioContainerAsset.audioContainer.unsheathe = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.unsheathe);
            itemAudioContainerAsset.audioContainer.onBlockDamage = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.onBlockDamage);
            itemAudioContainerAsset.audioContainer.footStep = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.footStep);
            itemAudioContainerAsset.audioContainer.bodyMovement = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.bodyMovement);
            itemAudioContainerAsset.audioContainer.bodyMovementFast = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.bodyMovementFast);
            itemAudioContainerAsset.audioContainer.equipArmor = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.equipArmor);
            itemAudioContainerAsset.audioContainer.unEquipArmor = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.unEquipArmor);
            itemAudioContainerAsset.audioContainer.useItem = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.useItem);
            itemAudioContainerAsset.audioContainer.pickupItem = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.pickupItem);
            itemAudioContainerAsset.audioContainer.dropItem = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.dropItem);
            itemAudioContainerAsset.audioContainer.specialAttackSwing = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, itemAudioContainerAsset.audioContainer.specialAttackSwing);
        }

        static void AddAliveAudioContainerSounds(AliveAudioContainer audioContainer, NativeHashSet<GUID> aliveSoundsGuids) {
            if (audioContainer == null) {
                return;
            }

            aliveSoundsGuids.Add(audioContainer.idle.Guid);
            aliveSoundsGuids.Add(audioContainer.hurt.Guid);
            aliveSoundsGuids.Add(audioContainer.die.Guid);
            aliveSoundsGuids.Add(audioContainer.attack.Guid);
            aliveSoundsGuids.Add(audioContainer.specialAttack.Guid);
            aliveSoundsGuids.Add(audioContainer.specialBegin.Guid);
            aliveSoundsGuids.Add(audioContainer.specialRelease.Guid);
            aliveSoundsGuids.Add(audioContainer.fall.Guid);
            aliveSoundsGuids.Add(audioContainer.dash.Guid);
            aliveSoundsGuids.Add(audioContainer.footStep.Guid);
            aliveSoundsGuids.Add(audioContainer.roar.Guid);
        }

        static void AddAliveAudioContainerSoundsEvents(AliveAudioContainer audioContainer, HashSet<EventReference> aliveSoundsEvents) {
            if (audioContainer == null) {
                return;
            }

            aliveSoundsEvents.Add(audioContainer.idle);
            aliveSoundsEvents.Add(audioContainer.hurt);
            aliveSoundsEvents.Add(audioContainer.die);
            aliveSoundsEvents.Add(audioContainer.attack);
            aliveSoundsEvents.Add(audioContainer.specialAttack);
            aliveSoundsEvents.Add(audioContainer.specialBegin);
            aliveSoundsEvents.Add(audioContainer.specialRelease);
            aliveSoundsEvents.Add(audioContainer.fall);
            aliveSoundsEvents.Add(audioContainer.dash);
            aliveSoundsEvents.Add(audioContainer.footStep);
            aliveSoundsEvents.Add(audioContainer.roar);
        }

        static void ReplaceAliveAudioContainerInvalidSounds(AliveAudioAttachment aliveAudioAttachment, Dictionary<GUID, (GUID validEventGuid, string validEventPath)> invalidGuidToValidData) {
            if (aliveAudioAttachment?.aliveAudioContainerWrapper?.Data == null) {
                return;
            }

            aliveAudioAttachment.aliveAudioContainerWrapper.EmbeddedDataRef.idle = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioAttachment.aliveAudioContainerWrapper.Data.idle);
            aliveAudioAttachment.aliveAudioContainerWrapper.EmbeddedDataRef.hurt = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioAttachment.aliveAudioContainerWrapper.Data.hurt);
            aliveAudioAttachment.aliveAudioContainerWrapper.EmbeddedDataRef.die = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioAttachment.aliveAudioContainerWrapper.Data.die);
            aliveAudioAttachment.aliveAudioContainerWrapper.EmbeddedDataRef.attack = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioAttachment.aliveAudioContainerWrapper.Data.attack);
            aliveAudioAttachment.aliveAudioContainerWrapper.EmbeddedDataRef.specialAttack = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioAttachment.aliveAudioContainerWrapper.Data.specialAttack);
            aliveAudioAttachment.aliveAudioContainerWrapper.EmbeddedDataRef.specialBegin = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioAttachment.aliveAudioContainerWrapper.Data.specialBegin);
            aliveAudioAttachment.aliveAudioContainerWrapper.EmbeddedDataRef.specialRelease = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioAttachment.aliveAudioContainerWrapper.Data.specialRelease);
            aliveAudioAttachment.aliveAudioContainerWrapper.EmbeddedDataRef.fall = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioAttachment.aliveAudioContainerWrapper.Data.fall);
            aliveAudioAttachment.aliveAudioContainerWrapper.EmbeddedDataRef.dash = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioAttachment.aliveAudioContainerWrapper.Data.dash);
            aliveAudioAttachment.aliveAudioContainerWrapper.EmbeddedDataRef.footStep = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioAttachment.aliveAudioContainerWrapper.Data.footStep);
            aliveAudioAttachment.aliveAudioContainerWrapper.EmbeddedDataRef.roar = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioAttachment.aliveAudioContainerWrapper.Data.roar);
            
            aliveAudioAttachment.wyrdConvertedAudioContainerWrapper.EmbeddedDataRef.idle = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioAttachment.wyrdConvertedAudioContainerWrapper.Data.idle);
            aliveAudioAttachment.wyrdConvertedAudioContainerWrapper.EmbeddedDataRef.hurt = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioAttachment.wyrdConvertedAudioContainerWrapper.Data.hurt);
            aliveAudioAttachment.wyrdConvertedAudioContainerWrapper.EmbeddedDataRef.die = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioAttachment.wyrdConvertedAudioContainerWrapper.Data.die);
            aliveAudioAttachment.wyrdConvertedAudioContainerWrapper.EmbeddedDataRef.attack = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioAttachment.wyrdConvertedAudioContainerWrapper.Data.attack);
            aliveAudioAttachment.wyrdConvertedAudioContainerWrapper.EmbeddedDataRef.specialAttack = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioAttachment.wyrdConvertedAudioContainerWrapper.Data.specialAttack);
            aliveAudioAttachment.wyrdConvertedAudioContainerWrapper.EmbeddedDataRef.specialBegin = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioAttachment.wyrdConvertedAudioContainerWrapper.Data.specialBegin);
            aliveAudioAttachment.wyrdConvertedAudioContainerWrapper.EmbeddedDataRef.specialRelease = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioAttachment.wyrdConvertedAudioContainerWrapper.Data.specialRelease);
            aliveAudioAttachment.wyrdConvertedAudioContainerWrapper.EmbeddedDataRef.fall = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioAttachment.wyrdConvertedAudioContainerWrapper.Data.fall);
            aliveAudioAttachment.wyrdConvertedAudioContainerWrapper.EmbeddedDataRef.dash = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioAttachment.wyrdConvertedAudioContainerWrapper.Data.dash);
            aliveAudioAttachment.wyrdConvertedAudioContainerWrapper.EmbeddedDataRef.footStep = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioAttachment.wyrdConvertedAudioContainerWrapper.Data.footStep);
            aliveAudioAttachment.wyrdConvertedAudioContainerWrapper.EmbeddedDataRef.roar = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioAttachment.wyrdConvertedAudioContainerWrapper.Data.roar);
        }
        
        static void ReplaceAliveAudioContainerInvalidSounds(AliveAudioContainerAsset aliveAudioContainerAsset, Dictionary<GUID, (GUID validEventGuid, string validEventPath)> invalidGuidToValidData) {
            if (aliveAudioContainerAsset?.audioContainer == null) {
                return;
            }

            aliveAudioContainerAsset.audioContainer.idle = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioContainerAsset.audioContainer.idle);
            aliveAudioContainerAsset.audioContainer.hurt = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioContainerAsset.audioContainer.hurt);
            aliveAudioContainerAsset.audioContainer.die = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioContainerAsset.audioContainer.die);
            aliveAudioContainerAsset.audioContainer.attack = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioContainerAsset.audioContainer.attack);
            aliveAudioContainerAsset.audioContainer.specialAttack = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioContainerAsset.audioContainer.specialAttack);
            aliveAudioContainerAsset.audioContainer.specialBegin = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioContainerAsset.audioContainer.specialBegin);
            aliveAudioContainerAsset.audioContainer.specialRelease = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioContainerAsset.audioContainer.specialRelease);
            aliveAudioContainerAsset.audioContainer.fall = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioContainerAsset.audioContainer.fall);
            aliveAudioContainerAsset.audioContainer.dash = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioContainerAsset.audioContainer.dash);
            aliveAudioContainerAsset.audioContainer.footStep = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioContainerAsset.audioContainer.footStep);
            aliveAudioContainerAsset.audioContainer.roar = ReplaceEventReferenceIfInvalid(invalidGuidToValidData, aliveAudioContainerAsset.audioContainer.roar);
        }

        static EventReference ReplaceEventReferenceIfInvalid(Dictionary<GUID, (GUID validEventGuid, string validEventPath)> invalidGuidToValidData, EventReference eventRef) {
            if (invalidGuidToValidData.TryGetValue(eventRef.Guid, out var validData)) {
                return new EventReference() {
                    Guid = validData.validEventGuid,
                    Path = validData.validEventPath
                };
            }

            return eventRef;
        }

        static void AddCrittersSounds(CritterSpawnerAttachment critterAIWalk, NativeHashSet<GUID> aliveSoundsGuids) {
            aliveSoundsGuids.Add(critterAIWalk.IdleSound.Guid);
            aliveSoundsGuids.Add(critterAIWalk.DeathSound.Guid);
            aliveSoundsGuids.Add(critterAIWalk.MovementSound.Guid);
        }

        static void AddVLockpickingSounds(VLockpicking vLockpicking, NativeHashSet<GUID> uiSoundsGuids) {
            var audio = vLockpicking.audioEvents;
            uiSoundsGuids.Add(audio.enterToolsIntoLock.Guid);
            uiSoundsGuids.Add(audio.pickRotate.Guid);
            uiSoundsGuids.Add(audio.pickDamageTaken.Guid);
            uiSoundsGuids.Add(audio.pickBreak.Guid);
            uiSoundsGuids.Add(audio.lockRotateOpen.Guid);
            uiSoundsGuids.Add(audio.lockToNextLayer.Guid);
            uiSoundsGuids.Add(audio.lockOpen.Guid);
        }

        static void AddAudioBiomeSoundsGuids(AudioBiome audioBiome, NativeHashSet<GUID> musicSoundsGuids, NativeHashSet<GUID> ambienceSoundsGuids) {
            if (audioBiome == null) {
                return;
            }

            foreach (var audioSource in audioBiome._ambientsToRegister) {
                if (audioSource == null) {
                    continue;
                }

                ambienceSoundsGuids.Add(audioSource.EventReference().Guid);
            }

            foreach (var audioSource in audioBiome._snapshotsToRegister) {
                if (audioSource == null) {
                    continue;
                }

                ambienceSoundsGuids.Add(audioSource.EventReference().Guid);
            }

            foreach (var audioSource in audioBiome._musicToRegister) {
                if (audioSource == null) {
                    continue;
                }

                musicSoundsGuids.Add(audioSource.EventReference().Guid);
            }

            foreach (var audioSource in audioBiome._alertMusicToRegister) {
                if (audioSource == null) {
                    continue;
                }

                musicSoundsGuids.Add(audioSource.EventReference().Guid);
            }

            foreach (var audioSource in audioBiome._combatMusicToRegister) {
                if (audioSource == null) {
                    continue;
                }

                musicSoundsGuids.Add(audioSource.EventReference().Guid);
            }
        }

        static void AddStartingDungeonArrowSounds(StartingDungeonArrow startingDungeonArrow, NativeHashSet<GUID> dynamicItemsSoundsGuids) {
            dynamicItemsSoundsGuids.Add(startingDungeonArrow.onEnableAudio.Guid);
            dynamicItemsSoundsGuids.Add(startingDungeonArrow.onHitAudio.Guid);
            foreach (var additionalOnHitAudio in startingDungeonArrow.additionalOnHitAudio) {
                dynamicItemsSoundsGuids.Add(additionalOnHitAudio.Guid);
            }
        }

        static void AddFishingSounds(Dictionary<string, NativeHashSet<GUID>> pathToSoundsGuidsMap, Allocator allocator) {
            var fishingAudioSoundsGuids = new NativeHashSet<GUID>(5, allocator);
            var fishingAudio = CommonReferences.Get.AudioConfig.FishingAudio;
            foreach (var audioSource in fishingAudio.music) {
                if (audioSource == null) {
                    continue;
                }

                fishingAudioSoundsGuids.Add(audioSource.EventReference().Guid);
            }

            foreach (var audioSource in fishingAudio.ambient) {
                if (audioSource == null) {
                    continue;
                }

                fishingAudioSoundsGuids.Add(audioSource.EventReference().Guid);
            }

            foreach (var audioSource in fishingAudio.snapshots) {
                if (audioSource == null) {
                    continue;
                }

                fishingAudioSoundsGuids.Add(audioSource.EventReference().Guid);
            }

            fishingAudioSoundsGuids.Add(fishingAudio.rodCastingStart.Guid);
            fishingAudioSoundsGuids.Add(fishingAudio.rodCastingThrow.Guid);
            fishingAudioSoundsGuids.Add(fishingAudio.rodCatch.Guid);
            fishingAudioSoundsGuids.Add(fishingAudio.fishingRodStruggling.Guid);
            fishingAudioSoundsGuids.Add(fishingAudio.bobberHitWater.Guid);
            fishingAudioSoundsGuids.Add(fishingAudio.bobberSubmergeWithCatch.Guid);
            fishingAudioSoundsGuids.Add(fishingAudio.bobberSubmergeFakeCatch.Guid);
            fishingAudioSoundsGuids.Add(fishingAudio.catchGarbage.Guid);
            fishingAudioSoundsGuids.Add(fishingAudio.catchCommonFish.Guid);
            fishingAudioSoundsGuids.Add(fishingAudio.catchUncommonFish.Guid);
            fishingAudioSoundsGuids.Add(fishingAudio.catchRareFish.Guid);
            fishingAudioSoundsGuids.Add(fishingAudio.catchLegendaryFish.Guid);
            fishingAudioSoundsGuids.Add(fishingAudio.lineBreak.Guid);
            fishingAudioSoundsGuids.Add(fishingAudio.fishFighting.Guid);

            pathToSoundsGuidsMap.Add("CommonReferences/AudioConfig/FishingAudio.struct", fishingAudioSoundsGuids);
        }

        static void AddPresenterDataProvidersSounds(NativeHashSet<GUID> uiSoundsGuids) {
            uiSoundsGuids.Add(_commonReferences.AudioConfig.ExpAudio.eventReference.Guid);
            uiSoundsGuids.Add(_commonReferences.AudioConfig.ItemAudio.eventReference.Guid);
            uiSoundsGuids.Add(_commonReferences.AudioConfig.LocationAudio.eventReference.Guid);
            uiSoundsGuids.Add(_commonReferences.AudioConfig.SpecialItemAudio.eventReference.Guid);
            uiSoundsGuids.Add(_commonReferences.AudioConfig.ProficiencyAudio.eventReference.Guid);
            uiSoundsGuids.Add(_commonReferences.AudioConfig.LevelUpAudio.eventReference.Guid);
            uiSoundsGuids.Add(_commonReferences.AudioConfig.QuestAudio.QuestCompletedSound.Guid);
            uiSoundsGuids.Add(_commonReferences.AudioConfig.QuestAudio.QuestFailedSound.Guid);
            uiSoundsGuids.Add(_commonReferences.AudioConfig.QuestAudio.QuestTakenSound.Guid);
            uiSoundsGuids.Add(_commonReferences.AudioConfig.ObjectiveAudio.ObjectiveChangedSound.Guid);
            uiSoundsGuids.Add(_commonReferences.AudioConfig.ObjectiveAudio.ObjectiveCompletedSound.Guid);
            uiSoundsGuids.Add(_commonReferences.AudioConfig.ObjectiveAudio.ObjectiveFailedSound.Guid);
            uiSoundsGuids.Add(_commonReferences.AudioConfig.RecipeAudio.eventReference.Guid);
            uiSoundsGuids.Add(_commonReferences.AudioConfig.JournalAudio.eventReference.Guid);
        }

        static string[] GetPrefabsGuids(string folderPath) {
            if (AssetDatabase.IsValidFolder(folderPath) == false) {
                Log.Important?.Error($"{folderPath} is not a valid folder");
                return Array.Empty<string>();
            }

            return AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        }

        static bool TryGetSelectedFolderPath(out string selectedPath) {
            selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            return string.IsNullOrEmpty(selectedPath) == false && AssetDatabase.IsValidFolder(selectedPath);
        }
    }
}