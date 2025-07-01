using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Awaken.TG.Editor.Main.Fmod;
using Awaken.TG.Editor.Utility.Searching;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Steps;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using XNode;
using GUID = FMOD.GUID;

namespace Awaken.TG.Editor.Main.Stories {
    public static class StoryGraphFmodEditorUtils {
        static Predicate<string> AlwaysLoadedBanksNamesContainsFunc = SoundsSearching.AlwaysLoadedBanksNames.Contains;

        public static void RefreshUsedSoundBanksOnAllStoryGraphs() {
            // FmodEditorUtils.LoadAllBanks(out var banksDatas);
            // FmodEditorUtils.GetEventGuidToBankNameMap(banksDatas, out var eventGuidToBanksNamesMap);
            // FmodEditorUtils.GetEventGuidToPathMap(banksDatas, out Dictionary<GUID, string> eventGuidToPathMap);

            FmodEditorUtils.UnloadAllBanks();
            var allStoryGraphs = AssetDatabaseUtils.GetAllAssetsOfType<StoryGraph>();
            foreach (var storyGraph in allStoryGraphs) {
                // RefreshStoryGraphsUsedSoundBanks(eventGuidToBanksNamesMap, eventGuidToPathMap, storyGraph);
            }
        }
        
        static void RefreshStoryGraphsUsedSoundBanks(Dictionary<GUID, List<string>> eventGuidToBanksNamesMap, Dictionary<GUID, string> eventGuidToPathMap, StoryGraph storyGraph) {

            var voiceOversGuids = GetUsedVoiceOversFMODEventsGuids(storyGraph.nodes, ARAlloc.Temp);
            var storyGraphSoundBanks = new HashSet<string>(2);
            foreach (var voiceOverGuid in voiceOversGuids) {
                if ((eventGuidToBanksNamesMap.TryGetValue(voiceOverGuid, out var soundsBanks) == false || soundsBanks.Count == 0) && voiceOverGuid != default) {
                    var voiceOverPath = eventGuidToPathMap.GetValueOrDefault(voiceOverGuid, voiceOverGuid.ToString());
                    Log.Important?.Error($"Sound bank for voice over {voiceOverPath} in story graph {storyGraph.name} is not found", storyGraph);
                    continue;
                }
                if (soundsBanks!= null) {
                    soundsBanks.RemoveAll(AlwaysLoadedBanksNamesContainsFunc);
                    if (soundsBanks.Count > 0) {
                        // If same VO event is in multiple sound banks - we need to load only one of them to load event
                        storyGraphSoundBanks.Add(soundsBanks[0]);
                    }
                }
            }

            voiceOversGuids.Dispose();
            storyGraph.usedSoundBanksNames = storyGraphSoundBanks.ToArray();
            EditorUtility.SetDirty(storyGraph);
        }
        
        public static Dictionary<GUID, List<string>> GetVoiceOverEventsGuidToBanksMapping() {
            var allStoryGraphs = AssetDatabaseUtils.GetAllAssetsOfType<StoryGraph>();
            var voiceOverGUIDToStoryGraphsNamesMap = new Dictionary<GUID, List<string>>();
            foreach (var storyGraph in allStoryGraphs) {
                using var storyGraphUsedVoiceOversGuids = GetUsedVoiceOversFMODEventsPaths(storyGraph.nodes, ARAlloc.Temp);
                foreach (var voiceOverGuid in storyGraphUsedVoiceOversGuids) {
                    if (voiceOverGuid == default) {
                        continue;
                    }
                    if (voiceOverGUIDToStoryGraphsNamesMap.TryGetValue(voiceOverGuid, out var voiceOverStoryGraphsNames) == false) {
                        voiceOverStoryGraphsNames = new List<string>(1);
                        voiceOverGUIDToStoryGraphsNamesMap.Add(voiceOverGuid, voiceOverStoryGraphsNames);
                    }

                    voiceOverStoryGraphsNames.Add(storyGraph.name);
                }
            }
            return voiceOverGUIDToStoryGraphsNamesMap;
        }

        public static void GenerateVoiceOversToBanksMappingFile(Dictionary<string, List<string>> voiceOverPathToStoryGraphsNamesMap) {
            var sb = new StringBuilder();

            foreach (var (voiceOverPath, storyGraphsNames) in voiceOverPathToStoryGraphsNamesMap) {
                sb.Append(voiceOverPath);
                foreach (var storyGraphName in storyGraphsNames) {
                    sb.Append(',').Append(storyGraphName);
                }

                sb.Append('\n');
            }

            var path = Path.Combine(Application.dataPath, "VoiceOversToBanksMap.csv");
            File.WriteAllText(path, sb.ToString());
            Log.Important?.Error($"VoiceOversToBanksMap file was written to path: {path}");
        }

        public static void RefreshStoryGraphsUsedSoundBanks(StoryGraph storyGraph) {
            // FmodEditorUtils.LoadAllBanks(out var banksDatas);
            // FmodEditorUtils.GetEventGuidToBankNameMap(banksDatas, out var eventGuidToBankNameMap);
            // FmodEditorUtils.GetEventGuidToPathMap(banksDatas, out Dictionary<GUID, string> eventGuidToPathMap);
            FmodEditorUtils.UnloadAllBanks();
            // RefreshStoryGraphsUsedSoundBanks(eventGuidToBankNameMap, eventGuidToPathMap, storyGraph);
        }

        static NativeHashSet<GUID> GetUsedVoiceOversFMODEventsGuids(List<Node> nodes, Allocator allocator) {
            var voiceOversGuids = new NativeHashSet<GUID>(256, allocator);
            var nodesCount = nodes.Count;
            for (int i = 0; i < nodesCount; i++) {
                var node = nodes[i];
                if (node is not IEditorChapter chapterNode) {
                    continue;
                }

                foreach (IEditorStep chapterNodeStep in chapterNode.Steps) {
                    AddStepSoundsGuids(chapterNodeStep, voiceOversGuids);
                }
            }

            return voiceOversGuids;
        }

        static NativeHashSet<GUID> GetUsedVoiceOversFMODEventsPaths(List<Node> nodes, Allocator allocator) {
            var voiceOversGuids = new NativeHashSet<GUID>(256, allocator);
            var nodesCount = nodes.Count;
            for (int i = 0; i < nodesCount; i++) {
                var node = nodes[i];
                if (node is not IEditorChapter chapterNode) {
                    continue;
                }

                foreach (var chapterNodeStep in chapterNode.Steps) {
                    AddStepSoundsGuids(chapterNodeStep, voiceOversGuids);
                }
            }

            return voiceOversGuids;
        }

        static void AddStepSoundsGuids(IEditorStep chapterNodeStep, NativeHashSet<GUID> storyGraphsGuids) {
            if (chapterNodeStep is SEditorText { hasVoice: true } textStep) {
                storyGraphsGuids.Add(textStep.audioClip.Guid);
            } else if (chapterNodeStep is SEditorPlaySFX playSfxStep && HasAudio(playSfxStep)) {
                storyGraphsGuids.Add(playSfxStep.audioClip.Guid);
            } else if (chapterNodeStep is SEditorChoice choiceStep && HasAudio(choiceStep)) {
                storyGraphsGuids.Add(choiceStep.audioClip.Guid);
            } else if (chapterNodeStep is SEditorPlayTutorialVideo playTutorialVideoStep && HasAudio(playTutorialVideoStep)) {
                storyGraphsGuids.Add(playTutorialVideoStep.handle.videoAudio.Guid);
            } else if (chapterNodeStep is SEditorPlayVideo playVideoStep && HasAudio(playVideoStep)) {
                storyGraphsGuids.Add(playVideoStep.video.videoAudio.Guid);
            }
        }

        static bool HasAudio(SEditorPlaySFX step) {
            return step.audioClip.IsNull == false;
        }

        static bool HasAudio(SEditorChoice step) {
            return step.audioClip.IsNull == false;
        }

        static bool HasAudio(SEditorPlayTutorialVideo step) {
            return step.handle.videoAudio.IsNull == false;
        }

        static bool HasAudio(SEditorPlayVideo step) {
            return step.video.videoAudio.IsNull == false;
        }
    }
}