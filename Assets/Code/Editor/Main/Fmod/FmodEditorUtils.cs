using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Utility.Audio;
using Awaken.Utility.Debugging;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using Unity.Collections;
using UnityEditor;
using Debug = UnityEngine.Debug;
using GUID = FMOD.GUID;

namespace Awaken.TG.Editor.Main.Fmod {
    public static class FmodEditorUtils {
        
        public static void UnloadAllBanks() {
            // EditorUtils.System.unloadAll();
        }

        // public static void LoadAllBanks(out List<(Bank bank, string bankName)> banksDatas) {
        //     EventManager.ClearCache();
        //     EventManager.RefreshBanks();
        //
        //     banksDatas = new List<(Bank bank, string bankName)>(EventManager.Banks.Count);
        //     foreach (var bankRef in EventManager.Banks) {
        //         var status = FMODUnity.EditorUtils.System.loadBankFile(bankRef.Path, FMOD.Studio.LOAD_BANK_FLAGS.NORMAL, out var bank);
        //         if (status != RESULT.OK) {
        //             Debug.LogError($"Could not load bank {bankRef.Name}. Result: {status}");
        //             continue;
        //         }
        //
        //         banksDatas.Add((bank, bankRef.Name));
        //     }
        // }

        // public static void GetEventGuidToBankNameMap(List<(Bank bank, string bankName)> banksDatas, out Dictionary<GUID, List<string>> eventGuidToBanksNamesMap) {
        //     eventGuidToBanksNamesMap = new Dictionary<GUID, List<string>>();
        //
        //     foreach (var (bank, bankName) in banksDatas) {
        //         if (bank.getEventList(out var eventsDescriptions) != RESULT.OK) {
        //             continue;
        //         }
        //
        //         var eventsGuids = eventsDescriptions.Select(x => x.getID(out var guid) == RESULT.OK ? guid : default);
        //         foreach (GUID eventGuid in eventsGuids) {
        //             if (eventGuid == default) {
        //                 continue;
        //             }
        //
        //             if (eventGuidToBanksNamesMap.TryGetValue(eventGuid, out var eventBanksNames) == false) {
        //                 eventBanksNames = new List<string>(1);
        //                 eventGuidToBanksNamesMap.Add(eventGuid, eventBanksNames);
        //             }
        //             eventBanksNames.Add(bankName);
        //         }
        //     }
        // }

        // public static void GetEventGuidToPathMap(List<(Bank bank, string bankName)> banksDatas, out Dictionary<GUID, string> eventGuidToPathMap) {
        //     eventGuidToPathMap = new Dictionary<GUID, string>();
        //
        //     foreach (var (bank, _) in banksDatas) {
        //         if (bank.getEventList(out var eventsDescriptions) != RESULT.OK) {
        //             continue;
        //         }
        //
        //         foreach (var eventDescription in eventsDescriptions) {
        //             if (eventDescription.getID(out var guid) != RESULT.OK || guid == default || eventDescription.getPath(out var path) != RESULT.OK) {
        //                 continue;
        //             }
        //
        //             eventGuidToPathMap[guid] = path;
        //         }
        //     }
        // }

        // public static Bank GetBankWithName(List<(Bank bank, string bankName)> banksDatas, string name) {
        //     for (int i = 0; i < banksDatas.Count; i++) {
        //         var (bank, bankName) = banksDatas[i];
        //         if (bankName == name) {
        //             return bank;
        //         }
        //     }
        //     return default;
        // }

        // public static void GetBankEventsGuids(Bank bank, NativeHashSet<GUID> eventsGuids) {
        //     if (bank.getEventList(out var eventsDescriptions) != RESULT.OK) {
        //         return;
        //     }
        //
        //     foreach (var eventDescription in eventsDescriptions) {
        //         if (eventDescription.getID(out var guid) != RESULT.OK || guid == default) {
        //             continue;
        //         }
        //
        //         eventsGuids.Add(guid);
        //     }
        // }

        [MenuItem("TG/Audio/Validate VoiceOvers Banks Assignment")]
        public static void ValidateAllStoryGraphsVoiceOvers() {
            const string ForbiddenBankName = "VoiceOvers";
            const string ForbiddenBankName2 = "UnusedVoiceOvers";
            var voiceOvers = new List<VoiceOverData>();
            EditorUtility.DisplayProgressBar("Validating VoiceOvers Banks Assignment", "Loading VoiceOvers Data", 0);
            foreach (var audioFilePath in EditorAudioUtils.GetAllVoiceOverPaths()) {
                voiceOvers.Add(new VoiceOverData(audioFilePath));
            }

            int i = 0;
            var x = voiceOvers.GroupBy(v => v.StoryGraph).ToArray();
            int graphsLength = x.Length;
            foreach (var grouped in x) {
                if (grouped.Key == null) {
                    continue;
                }
                EditorUtility.DisplayProgressBar("Validating VoiceOvers Banks Assignment", $"Validating: {grouped.Key.name}, Progress: {i}/{graphsLength}", i/(float)graphsLength);

                bool hasAudioReplacement = false;
                HashSet<string> bankNames = new();
                foreach (var voData in grouped) {
                    try {
                        if (voData.HasAudioReplacement) {
                            hasAudioReplacement = true;
                        }
                        
                        EditorAudioUtils.GetGuidAndFileIdFromAudioFileId(voData.ID, out string guid, out long fileId);
                        // if (voData.TryFindMatchingEventRef(fileId.ToString(), guid, out var eventRef)) {
                        //     foreach (var bankName in eventRef.Banks) {
                        //         bankNames.Add(bankName.Name);
                        //     }
                        // }
                    } catch (Exception) {
                        // ignore
                    }
                }

                if (bankNames.Count > 1) {
                    Log.Critical?.Error($"StoryGraph: {grouped.Key.name}, with VO assigned to multiple banks: {string.Join(", ", bankNames)}", grouped.Key);
                } else if (bankNames.Count == 1) {
                    if (bankNames.First() == ForbiddenBankName) {
                        Log.Critical?.Error($"StoryGraph: {grouped.Key.name}, with VO assigned to default bank: {ForbiddenBankName}", grouped.Key);
                    } else if (bankNames.First() == ForbiddenBankName2) {
                        Log.Critical?.Error($"StoryGraph: {grouped.Key.name}, with VO assigned to default bank: {ForbiddenBankName2}", grouped.Key);
                    } else {
                        Log.Important?.Warning($"StoryGraph: {grouped.Key.name}, with VO assigned to single bank: {string.Join(", ", bankNames)}", grouped.Key);
                    }
                } else if (hasAudioReplacement) {
                    Log.Critical?.Error($"StoryGraph: {grouped.Key.name}, with no bank assigned!");
                }

                i++;
            }
            EditorUtility.ClearProgressBar();
        }
    }
}