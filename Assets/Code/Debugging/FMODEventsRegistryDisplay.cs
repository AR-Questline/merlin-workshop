#if AR_DEBUG || DEBUG
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Awaken.Utility.Debugging;
using Awaken.Utility.UI;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using Pathfinding.Util;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Pool;

namespace Awaken.TG.Debugging {
    public class FMODEventsRegistryDisplay : UGUIWindowDisplay<FMODEventsRegistryDisplay> {
        const float StartX = 20f;
        const float SpacingWidth = 10f;
        const float SpacingWidthDouble = SpacingWidth * 2;
        const float SpacingHeight = 10f;
        const float LabelWidth = 100f;
        const float LabelHeight = 20f;
        const float BankNameWidth = 300f;
        const float EventNameWidth = 500f;
        const float EventGuidWidth = 280f;
        const float MemoryDataFieldWidth = 120f;
        const float CountWidth = 50f;
        const float IsStreamWidth = 60f;
        const float VisibleScrollRectHeight = 200f;
        const float ButtonWidth = 200;
        const float OneShotsClearDelayFieldWidth = 50f;
        const float OneShotsClearDelayNameWidth = 250f;
        const float ButtonHeight = 30f;
        const uint OneShotsClearFrameDelayDefault = 60;
        const string PropNameGuid = "Guid";
        const string PropNamePath = "Path";
        const string PropNameCount = "Count";
        const string PropNameStream = "Stream";
        static StringBuilder StringBuilder = new();
#if !UNITY_GAMECORE
        static readonly string[] StackTraceStrings = new string[3];
#endif

        Dictionary<GUID, int> _createdEventGuidToCountMap = new();
        Dictionary<GUID, int> _culledEventGuidToCountMap = new();
        Dictionary<GUID, MemoryUsageMinMax> _eventGuidToMemoryUsageMinMaxMap = new();
        Dictionary<GUID, string> _eventGuidToPath = new();
        Dictionary<GUID, bool> _eventGuidToIsStreamStatusMap = new();
        List<EventData> _createdEventsData = new();
        List<EventNameAndCountData> _culledEventsData = new();
        List<(OneShotEventData data, int frame)> _oneShotEventDatas = new();
        List<string> _loadedBanksNames = new();
        SortMode _sortMode = SortMode.ExclusiveMemoryAll;

        Vector2 _playingScrollPos, _culledScrollPos, _oneShotsScrollPos, _loadedBanksScrollPos, _allScrollPos;
        string _oneShotsClearFramesDelayString;
        uint _oneShotsClearFramesDelay;
        bool _mergeOneShots = true;

        [StaticMarvinButton(state: nameof(IsEventsRegistryWindowOn))]
        static void ToggleFmodEventsRegistry() {
            FMODEventsRegistryDisplay.Toggle(new UGUIWindowUtils.WindowPositioning(UGUIWindowUtils.WindowPosition.TopLeft, 0.95f, 0.9f));
        }

        static bool IsEventsRegistryWindowOn() => FMODEventsRegistryDisplay.IsShown;

        [StaticMarvinButton(state: nameof(IsOverlayEnabled))]
        static void ToggleFmodStatsOverlay() {
            //RuntimeManager.IsOverlayEnabledOverride = !RuntimeManager.IsOverlayEnabled;
        }

        static bool IsOverlayEnabled() {
            //return RuntimeManager.IsOverlayEnabled;
            return false;
        }

        protected override void Initialize() {
            base.Initialize();
            try {
                //RuntimeManager.LoadBank("Master.strings", loadBankFlags: LOAD_BANK_FLAGS.NORMAL);
            } catch (System.Exception e) {
                Log.Important?.Error($"Exception while loading Master.strings bank for {nameof(FMODEventsRegistryDisplay)}. Not critical for gameplay.");
                UnityEngine.Debug.LogException(e);
            }

            RefreshData();
            // FmodEventsRegistry.OnEventInstanceCreated += OnEventInstanceCreated;
            // FmodEventsRegistry.OnEventInstanceReleased += OnEventInstanceReleased;
            // FmodEventsRegistry.OnEventStartCulling += OnEventStartCulling;
            // FmodEventsRegistry.OnEventStopCulling += OnEventStopCulling;
            // FmodEventsRegistry.OnOneShotAdded += OnOneShotAdded;
            // FmodEventsRegistry.OnClearedData += RefreshData;
            //
            // RuntimeManager.OnLoadedBanksChanged += OnLoadedBanksChanged;
        }

        protected override void Shutdown() {
            // FmodEventsRegistry.OnEventInstanceCreated -= OnEventInstanceCreated;
            // FmodEventsRegistry.OnEventInstanceReleased -= OnEventInstanceReleased;
            // FmodEventsRegistry.OnEventStartCulling -= OnEventStartCulling;
            // FmodEventsRegistry.OnEventStopCulling -= OnEventStopCulling;
            // FmodEventsRegistry.OnOneShotAdded -= OnOneShotAdded;
            // FmodEventsRegistry.OnClearedData -= RefreshData;
            // RuntimeManager.OnLoadedBanksChanged -= OnLoadedBanksChanged;
        }

        protected override void DrawWindow() {
            RemoveOldOneShots();

            var allScrollsHeight = Mathf.Max(GetEventsViewHeight(_createdEventsData.Count), VisibleScrollRectHeight) +
                                   Mathf.Max(GetEventsViewHeight(_oneShotEventDatas.Count), VisibleScrollRectHeight) +
                                   Mathf.Max(Mathf.Max(GetEventsViewHeight(_culledEventsData.Count), GetEventsViewHeight(_loadedBanksNames.Count)), VisibleScrollRectHeight)
                                   + ButtonHeight + (SpacingHeight * 12);

            float startY = 0;
            float startX = StartX;
            _allScrollPos = GUI.BeginScrollView(
                new Rect(startX, startY, Position.width + SpacingWidthDouble, Position.height - 20),
                _allScrollPos,
                new Rect(0, 0, Position.width, allScrollsHeight)
            );

            DrawTopRow(ref startX, ref startY);

            startY += SpacingHeight;
            DrawCreatedEventsScrollView("Created events", _createdEventsData, ref _playingScrollPos, ref startX, ref startY);
            startX = StartX;
            startY += SpacingHeight;
            EventInstance e;
            DrawOneShotsScrollView("One Shot Events", _oneShotEventDatas, _mergeOneShots, ref _oneShotsScrollPos, ref startX, ref startY);
            startX = StartX;
            startY += SpacingHeight;
            var culledEventsRowStartY = startY;
            DrawCulledEventsScrollView("Culled events", _culledEventsData, ref _culledScrollPos, ref startX, ref startY);
            startX += SpacingWidthDouble * 4;
            startY = culledEventsRowStartY;
            DrawLoadedBanksScrollView("Loaded Banks", _loadedBanksNames, ref _loadedBanksScrollPos, ref startX, ref startY);

            GUI.EndScrollView();
        }

        void DrawTopRow(ref float startX, ref float startY) {
            float initialStartX = startX;
            var initialStartY = startY;
            startX += SpacingWidth;
            startY += SpacingHeight;
            DrawSortButtons(ref startX, ref startY);
            startX += SpacingWidth;
            startY = initialStartY + SpacingHeight;
            DrawOneShotsClearDelayField(ref startX, ref startY);
            startX += SpacingWidth;
            startY = initialStartY + SpacingHeight;
            DrawLogSnapshotToFileButton(ref startX, ref startY);
            startX += SpacingWidth;
            startY = initialStartY + SpacingHeight;
            DrawMergeOneShotsButton(ref startX, ref startY);
            startX = initialStartX;
            startY = initialStartY + ButtonHeight;
        }

        void DrawLogSnapshotToFileButton(ref float startX, ref float startY) {
            if (GUI.Button(new Rect(startX, startY, ButtonWidth, ButtonHeight), "Log snapshot to file") == false) {
                startX += ButtonWidth;
                startY += ButtonHeight;
                return;
            }
            startX += ButtonWidth;
            startY += ButtonHeight;
            
            LogSnapshotToFile();
        }

        void DrawMergeOneShotsButton(ref float startX, ref float startY) {
            var buttonWidth = ButtonWidth * 0.5f;
            if (GUI.Button(new Rect(startX, startY, buttonWidth, ButtonHeight), "OneShots") == false) {
                startX += buttonWidth;
                startY += ButtonHeight;
                return;
            }

            startX += buttonWidth;
            startY += ButtonHeight;
            _mergeOneShots = !_mergeOneShots;
        }

        void DrawSortButtons(ref float rowX, ref float startY) {
            if (GUI.Button(new Rect(rowX, startY, ButtonWidth, ButtonHeight), "Sort by count")) {
                _sortMode = SortMode.Count;
            }

            rowX += ButtonWidth;

            if (GUI.Button(new Rect(rowX, startY, ButtonWidth, ButtonHeight), "Sort by exclusive memory")) {
                _sortMode = SortMode.ExclusiveMemory;
                SortEventsData(_createdEventsData);
            }

            rowX += ButtonWidth;
            if (GUI.Button(new Rect(rowX, startY, ButtonWidth, ButtonHeight), "Sort by inclusive memory")) {
                _sortMode = SortMode.InclusiveMemory;
                SortEventsData(_createdEventsData);
            }

            rowX += ButtonWidth;
            if (GUI.Button(new Rect(rowX, startY, ButtonWidth, ButtonHeight), "Sort by sample data memory")) {
                _sortMode = SortMode.SampleDataMemory;
                SortEventsData(_createdEventsData);
            }

            rowX += ButtonWidth;
            if (GUI.Button(new Rect(rowX, startY, ButtonWidth, ButtonHeight), "Sort by exclusive memory all")) {
                _sortMode = SortMode.ExclusiveMemoryAll;
                SortEventsData(_createdEventsData);
            }

            rowX += ButtonWidth;
            startY += ButtonHeight + SpacingHeight;
        }

        void DrawOneShotsClearDelayField(ref float startX, ref float startY) {
            GUI.Label(new Rect(startX, startY, OneShotsClearDelayNameWidth, ButtonHeight), "One Shots Clear Delay Frame Count:");
            startX += OneShotsClearDelayNameWidth;
            var newOneShotsClearFramesDelayString = GUI.TextField(new Rect(startX, startY, OneShotsClearDelayFieldWidth, ButtonHeight), _oneShotsClearFramesDelayString);
            startX += OneShotsClearDelayFieldWidth;
            if (uint.TryParse(newOneShotsClearFramesDelayString, out _oneShotsClearFramesDelay) == false) {
                _oneShotsClearFramesDelay = OneShotsClearFrameDelayDefault;
            }

            _oneShotsClearFramesDelayString = _oneShotsClearFramesDelay.ToString();
            startY += ButtonHeight;
        }

        static void DrawCreatedEventsScrollView(string title, List<EventData> eventsData, ref Vector2 scrollPos, ref float xOffset, ref float yOffset) {
            float scrollViewHeight = GetEventsViewHeight(eventsData.Count);
            float scrollViewDataWidth = EventGuidWidth + EventNameWidth + CountWidth + (MemoryDataFieldWidth * 4) + (SpacingWidth * 6) + SpacingWidth;
            GUI.Label(new Rect(xOffset, yOffset, LabelWidth, LabelHeight), title);
            yOffset += LabelHeight + SpacingHeight;

            var labelsXOffset = xOffset;
            GUI.Label(new Rect(labelsXOffset, yOffset, EventGuidWidth, LabelHeight), PropNameGuid);
            labelsXOffset += EventGuidWidth + SpacingWidth;
            GUI.Label(new Rect(labelsXOffset, yOffset, EventNameWidth, LabelHeight), PropNamePath);
            labelsXOffset += EventNameWidth + SpacingWidth;
            GUI.Label(new Rect(labelsXOffset, yOffset, CountWidth, LabelHeight), PropNameCount);
            labelsXOffset += CountWidth + SpacingWidth;
            GUI.Label(new Rect(labelsXOffset, yOffset, IsStreamWidth, LabelHeight), PropNameStream);
            labelsXOffset += IsStreamWidth + SpacingWidth;
            GUI.Label(new Rect(labelsXOffset, yOffset, MemoryDataFieldWidth, LabelHeight), "Exclusive Memory");
            labelsXOffset += MemoryDataFieldWidth + SpacingWidth;
            GUI.Label(new Rect(labelsXOffset, yOffset, MemoryDataFieldWidth, LabelHeight), "Inclusive Memory");
            labelsXOffset += MemoryDataFieldWidth + SpacingWidth;
            GUI.Label(new Rect(labelsXOffset, yOffset, MemoryDataFieldWidth, LabelHeight), "SampleData Memory");
            labelsXOffset += MemoryDataFieldWidth + SpacingWidth;
            GUI.Label(new Rect(labelsXOffset, yOffset, MemoryDataFieldWidth, LabelHeight), "Exclusive All");

            yOffset += LabelHeight + SpacingHeight;
            var scrollWidth = scrollViewDataWidth + SpacingWidthDouble;
            scrollPos = GUI.BeginScrollView(
                new Rect(xOffset, yOffset, scrollWidth, VisibleScrollRectHeight),
                scrollPos,
                new Rect(0, 0, scrollViewDataWidth, scrollViewHeight)
            );

            var localYOffset = 5f;
            foreach (var data in eventsData) {
                var localXOffset = 0f;
                GUI.Label(new Rect(localXOffset, localYOffset, EventGuidWidth, LabelHeight), data.nameAndCountData.guid);
                localXOffset += EventGuidWidth + SpacingWidth;
                GUI.Label(new Rect(localXOffset, localYOffset, EventNameWidth, LabelHeight), data.nameAndCountData.path);
                localXOffset += EventNameWidth + SpacingWidth;
                GUI.Label(new Rect(localXOffset, localYOffset, CountWidth, LabelHeight), data.nameAndCountData.count.ToString());
                localXOffset += CountWidth + SpacingWidth;
                GUI.Label(new Rect(localXOffset, localYOffset, IsStreamWidth, LabelHeight), data.isStream ? "stream" : "in RAM");
                localXOffset += IsStreamWidth + SpacingWidth;
                DrawMemoryDataField(data.memoryUsageMin.exclusive, data.memoryUsageMax.exclusive, LabelHeight, MemoryDataFieldWidth, ref localXOffset, localYOffset);
                DrawMemoryDataField(data.memoryUsageMin.inclusive, data.memoryUsageMax.inclusive, LabelHeight, MemoryDataFieldWidth, ref localXOffset, localYOffset);
                DrawMemoryDataField(data.memoryUsageMin.sampledata, data.memoryUsageMax.sampledata, LabelHeight, MemoryDataFieldWidth, ref localXOffset, localYOffset);
                DrawMemoryDataField(data.memoryUsageMin.exclusive * data.nameAndCountData.count, data.memoryUsageMax.exclusive * data.nameAndCountData.count, LabelHeight,
                    MemoryDataFieldWidth, ref localXOffset, localYOffset);
                localYOffset += LabelHeight;
            }

            GUI.EndScrollView();
            yOffset += VisibleScrollRectHeight + SpacingHeight;
            xOffset += scrollWidth;
        }

        static void DrawCulledEventsScrollView(string title, List<EventNameAndCountData> eventsData, ref Vector2 scrollPos, ref float xOffset, ref float yOffset) {
            float scrollViewHeight = GetEventsViewHeight(eventsData.Count);
            float scrollViewDataWidth = EventGuidWidth + EventNameWidth + CountWidth + (SpacingWidth * 2) + SpacingWidth;
            GUI.Label(new Rect(xOffset, yOffset, LabelWidth, LabelHeight), title);
            yOffset += LabelHeight + SpacingHeight;

            var labelsXOffset = xOffset;
            GUI.Label(new Rect(labelsXOffset, yOffset, EventGuidWidth, LabelHeight), PropNameGuid);
            labelsXOffset += EventGuidWidth + SpacingWidth;
            GUI.Label(new Rect(labelsXOffset, yOffset, EventNameWidth, LabelHeight), PropNamePath);
            labelsXOffset += EventNameWidth + SpacingWidth;
            GUI.Label(new Rect(labelsXOffset, yOffset, CountWidth, LabelHeight), PropNameCount);
            yOffset += LabelHeight + SpacingHeight;
            var scrollWidth = scrollViewDataWidth + SpacingWidthDouble;
            scrollPos = GUI.BeginScrollView(
                new Rect(xOffset, yOffset, scrollWidth, VisibleScrollRectHeight),
                scrollPos,
                new Rect(0, 0, scrollViewDataWidth, scrollViewHeight)
            );

            var localYOffset = 5f;
            foreach (var data in eventsData) {
                var localXOffset = 0f;
                GUI.Label(new Rect(localXOffset, localYOffset, EventGuidWidth, LabelHeight), data.guid);
                localXOffset += EventGuidWidth + SpacingWidth;
                GUI.Label(new Rect(localXOffset, localYOffset, EventNameWidth, LabelHeight), data.path);
                localXOffset += EventNameWidth + SpacingWidth;
                GUI.Label(new Rect(localXOffset, localYOffset, CountWidth, LabelHeight), data.count.ToString());
                localYOffset += LabelHeight;
            }

            GUI.EndScrollView();
            yOffset += VisibleScrollRectHeight;
            xOffset += scrollWidth;
        }

        static void DrawOneShotsScrollView(string title, List<(OneShotEventData data, int frame)> eventsData, bool merge, ref Vector2 scrollPos, ref float xOffset, ref float yOffset) {
            float scrollViewHeight = merge == false ? GetEventsViewHeight(eventsData.Count) : GetMergedEventsViewHeight(eventsData);
            float scrollViewDataWidth = EventGuidWidth + EventNameWidth + (SpacingWidth * 2) + SpacingWidth;
            if (merge) {
                scrollViewDataWidth += CountWidth + SpacingWidth;
            }
            GUI.Label(new Rect(xOffset, yOffset, LabelWidth, LabelHeight), title);
            yOffset += LabelHeight + SpacingHeight;

            var labelsXOffset = xOffset;
            GUI.Label(new Rect(labelsXOffset, yOffset, EventGuidWidth, LabelHeight), PropNameGuid);
            labelsXOffset += EventGuidWidth + SpacingWidth;
            GUI.Label(new Rect(labelsXOffset, yOffset, EventNameWidth, LabelHeight), PropNamePath);
            if (merge) {
                labelsXOffset += EventNameWidth + SpacingWidth;
                GUI.Label(new Rect(labelsXOffset, yOffset, CountWidth, LabelHeight), PropNameCount);
            }

            yOffset += LabelHeight + SpacingHeight;
            var scrollWidth = scrollViewDataWidth + SpacingWidthDouble;
            scrollPos = GUI.BeginScrollView(
                new Rect(xOffset, yOffset, scrollWidth, VisibleScrollRectHeight),
                scrollPos,
                new Rect(0, 0, scrollViewDataWidth, scrollViewHeight)
            );

            var localYOffset = 5f;
            if (merge == false) {
                foreach (var (data, _) in eventsData) {
                    var localXOffset = 0f;
                    GUI.Label(new Rect(localXOffset, localYOffset, EventGuidWidth, LabelHeight), data.guid);
                    localXOffset += EventGuidWidth + SpacingWidth;
                    GUI.Label(new Rect(localXOffset, localYOffset, EventNameWidth, LabelHeight), data.path);
                    localYOffset += LabelHeight;
                }
            } else {
                DictionaryPool<string, int>.Get(out var eventsGuidToCountMap);
                DictionaryPool<string, string>.Get(out var eventsGuidToPathMap);

                for (int i = 0; i < eventsData.Count; i++) {
                    var eventData = eventsData[i].data;
                    if (eventsGuidToCountMap.TryGetValue(eventData.guid, out int count) == false) {
                        eventsGuidToCountMap.Add(eventData.guid, 1);
                        eventsGuidToPathMap.Add(eventData.guid, eventData.path);
                    } else {
                        eventsGuidToCountMap[eventData.guid] = count + 1;
                    }
                }
                ListPool<EventNameAndCountData>.Get(out var mergedEventsData);
                foreach (var (guid, count) in eventsGuidToCountMap) {
                    mergedEventsData.Add(new (guid, eventsGuidToPathMap[guid], count));
                }
                DictionaryPool<string, int>.Release(eventsGuidToCountMap);
                DictionaryPool<string, string>.Release(eventsGuidToPathMap);
                
                mergedEventsData.Sort(CompareDataDescendingCount);
                foreach (var data in mergedEventsData) {
                    var localXOffset = 0f;
                    GUI.Label(new Rect(localXOffset, localYOffset, EventGuidWidth, LabelHeight), data.guid);
                    localXOffset += EventGuidWidth + SpacingWidth;
                    GUI.Label(new Rect(localXOffset, localYOffset, EventNameWidth, LabelHeight), data.path);
                    localXOffset += EventNameWidth + SpacingWidth;
                    GUI.Label(new Rect(localXOffset, localYOffset, CountWidth, LabelHeight), data.count.ToString());
                    localYOffset += LabelHeight;
                }
                ListPool<EventNameAndCountData>.Release(mergedEventsData);
            }


            GUI.EndScrollView();
            yOffset += VisibleScrollRectHeight + SpacingHeight;
            xOffset += scrollWidth;

            static float GetMergedEventsViewHeight(List<(OneShotEventData data, int frame)> eventsData) {
                HashSetPool<string>.Get(out var eventsUniqueGuids);
                for (int i = 0; i < eventsData.Count; i++) {
                    var eventData = eventsData[i].data;
                    eventsUniqueGuids.Add(eventData.guid);
                }
                int count = eventsUniqueGuids.Count;
                HashSetPool<string>.Release(eventsUniqueGuids);
                return GetEventsViewHeight(count);
            }
        }

        static void DrawLoadedBanksScrollView(string title, List<string> loadedBanksNames, ref Vector2 scrollPos, ref float xOffset, ref float yOffset) {
            float scrollViewHeight = GetEventsViewHeight(loadedBanksNames.Count);
            float scrollViewDataWidth = BankNameWidth + SpacingWidth;
            GUI.Label(new Rect(xOffset, yOffset, BankNameWidth, LabelHeight), title);

            yOffset += LabelHeight + SpacingHeight;
            // Begin scroll view for culled events
            var scrollWidth = scrollViewDataWidth + SpacingWidthDouble;
            scrollPos = GUI.BeginScrollView(
                new Rect(xOffset, yOffset, scrollWidth, VisibleScrollRectHeight),
                scrollPos,
                new Rect(0, 0, scrollViewDataWidth, scrollViewHeight)
            );

            var localYOffset = 5f;
            foreach (var bankName in loadedBanksNames) {
                var localXOffset = 0f;
                GUI.Label(new Rect(localXOffset, localYOffset, BankNameWidth, LabelHeight), bankName);
                localYOffset += LabelHeight;
            }

            GUI.EndScrollView();
            yOffset += VisibleScrollRectHeight + SpacingHeight;
            xOffset += scrollWidth;
        }

        static float GetEventsViewHeight(int count) {
            return LabelHeight * (count);
        }

        static void DrawMemoryDataField(int minValue, int maxValue, float labelHeight, float memoryDataFieldWidth, ref float localXOffset, float localYOffset) {
            //var exclusiveMinMaxDiff = maxValue - minValue;
            // string lowerBoundString = exclusiveMinMaxDiff == 0 ? string.Empty : RuntimeManager.BytesToReadableKB(exclusiveMinMaxDiff) + " - ";
            // GUI.Label(new Rect(localXOffset, localYOffset, memoryDataFieldWidth, labelHeight), lowerBoundString + RuntimeManager.BytesToReadableKB(maxValue));
            localXOffset += MemoryDataFieldWidth + SpacingWidth;
        }

        void RemoveOldOneShots() {
            // if (_oneShotsClearFramesDelay == 0) {
            //     return;
            // }
            //
            // int currentFrame = Time.frameCount;
            // int count = _oneShotEventDatas.Count;
            // if (count == 0) {
            //     return;
            // }
            //
            // if (_mergeOneShots == false) {
            //     for (int i = count - 1; i >= 0; i--) {
            //         var frameDiff = currentFrame - _oneShotEventDatas[i].frame;
            //         if (frameDiff > _oneShotsClearFramesDelay) {
            //             _oneShotEventDatas.RemoveAtSwapBack(i);
            //         }
            //     }
            // } else {
            //     for (int i = count - 1; i >= 0; i--) {
            //         var eventData = _oneShotEventDatas[i].data;
            //         if (eventData.instance.isValid() == false || eventData.instance.getPlaybackState(out var playbackState) != RESULT.OK || playbackState == PLAYBACK_STATE.STOPPED) {
            //             _oneShotEventDatas.RemoveAtSwapBack(i);
            //         }
            //     }
            // }
            //
            // _oneShotEventDatas.Sort(CompareOneShotsCreatedFrameDesc);
        }

        void LogSnapshotToFile() {
            // RefreshData();
            // var filePath = FmodEventsRegistry.FilePath.Insert(FmodEventsRegistry.FilePath.Length - FmodEventsRegistry.StackTracesFileExtension.Length, ".snapshot");
            // using var fs = new FileStream(filePath, FileMode.Append, FileAccess.Write);
            // using var writer = new StreamWriter(fs);
            // writer.Write("Created events:\n\n");
            // foreach (var eventData in _createdEventsData) {
            //     if (eventData.memoryUsageMin.Equals(eventData.memoryUsageMax)) {
            //         writer.Write($"{eventData.nameAndCountData.ToString()}, Memory: {eventData.memoryUsageMin.ToString()}\n");
            //     } else {
            //         writer.Write($"{eventData.nameAndCountData.ToString()}, Memory min: {eventData.memoryUsageMin.ToString()}, Memory max: {eventData.memoryUsageMax.ToString()}\n");
            //     }
            // }
            //
            // writer.Write("\nCulled events:\n\n");
            // foreach (var eventNameAndCountData in _culledEventsData) {
            //     writer.Write($"{eventNameAndCountData.ToString()}\n");
            // }
            //
            // writer.Write("\nOne shots:\n\n");
            // foreach (var (eventData, _) in _oneShotEventDatas) {
            //     writer.Write($"{eventData.ToString()}\n");
            // }
            //
            // writer.Write("\nLoaded banks:\n\n");
            // foreach (var bankName in _loadedBanksNames) {
            //     writer.Write($"{bankName}\n");
            // }
            //
            // writer.Write("\n\n");
        }

        void RefreshData() {
            // var createdEventInstances = FmodEventsRegistry.CreatedEventInstanceToFrameWhenRegisteredMap.Keys;
            // _createdEventGuidToCountMap.Clear();
            // _eventGuidToMemoryUsageMinMaxMap.Clear();
            // _eventGuidToPath.Clear();
            // foreach (var eventInstance in createdEventInstances) {
            //     if (TryGetEventDescriptionAndGuid(eventInstance, out var eventDescription, out var guid) == false) {
            //         continue;
            //     }
            //
            //     _createdEventGuidToCountMap[guid] = _createdEventGuidToCountMap.GetValueOrDefault(guid, 0) + 1;
            //
            //     if (_eventGuidToMemoryUsageMinMaxMap.TryGetValue(guid, out var memoryUsageMinMax) == false && eventInstance.getMemoryUsage(out var instanceMemoryUsage) == RESULT.OK) {
            //         _eventGuidToMemoryUsageMinMaxMap.Add(guid, new MemoryUsageMinMax(instanceMemoryUsage, instanceMemoryUsage));
            //     } else if (eventInstance.getMemoryUsage(out instanceMemoryUsage) == RESULT.OK &&
            //                (memoryUsageMinMax.min.Equals(instanceMemoryUsage) == false || memoryUsageMinMax.max.Equals(instanceMemoryUsage) == false)) {
            //         memoryUsageMinMax.min = instanceMemoryUsage.exclusive < memoryUsageMinMax.min.exclusive ? instanceMemoryUsage : memoryUsageMinMax.min;
            //         memoryUsageMinMax.max = instanceMemoryUsage.exclusive > memoryUsageMinMax.max.exclusive ? instanceMemoryUsage : memoryUsageMinMax.max;
            //         _eventGuidToMemoryUsageMinMaxMap[guid] = memoryUsageMinMax;
            //     }
            //
            //     if (_eventGuidToPath.TryGetValue(guid, out var path) == false && eventDescription.getPath(out path) == RESULT.OK) {
            //         _eventGuidToPath.Add(guid, path);
            //     }
            //
            //     if (_eventGuidToIsStreamStatusMap.TryGetValue(guid, out var isStream) == false && eventDescription.isStream(out isStream) == RESULT.OK) {
            //         _eventGuidToIsStreamStatusMap.Add(guid, isStream);
            //     }
            // }
            //
            // _culledEventGuidToCountMap.Clear();
            // var culledEventEmitters = FmodEventsRegistry.CulledEventEmitters;
            // foreach (var eventEmitter in culledEventEmitters) {
            //     if (eventEmitter == null) {
            //         continue;
            //     }
            //
            //     var eventDescription = eventEmitter.EventDescription;
            //     if (eventDescription.isValid() == false || eventDescription.getID(out var guid) != RESULT.OK) {
            //         continue;
            //     }
            //
            //     _culledEventGuidToCountMap[guid] = _culledEventGuidToCountMap.GetValueOrDefault(guid, 0) + 1;
            //
            //     if (_eventGuidToPath.TryGetValue(guid, out var path) == false && eventDescription.getPath(out path) == RESULT.OK) {
            //         _eventGuidToPath.Add(guid, path);
            //     }
            //
            //     if (_eventGuidToIsStreamStatusMap.TryGetValue(guid, out var isStream) == false && eventDescription.isStream(out isStream) == RESULT.OK) {
            //         _eventGuidToIsStreamStatusMap.Add(guid, isStream);
            //     }
            // }
            //
            // _createdEventsData.Clear();
            // foreach (var (eventGuid, count) in _createdEventGuidToCountMap) {
            //     var eventPath = _eventGuidToPath.GetValueOrDefault(eventGuid, string.Empty);
            //     var memoryUsage = _eventGuidToMemoryUsageMinMaxMap.GetValueOrDefault(eventGuid);
            //     var isStream = _eventGuidToIsStreamStatusMap.GetValueOrDefault(eventGuid);
            //     _createdEventsData.Add(new EventData(eventGuid.ToString(), eventPath, count, memoryUsage.min, memoryUsage.max, isStream));
            // }
            //
            // SortEventsData(_createdEventsData);
            //
            // _culledEventsData.Clear();
            // foreach (var (eventGuid, count) in _culledEventGuidToCountMap) {
            //     var eventPath = _eventGuidToPath.GetValueOrDefault(eventGuid, string.Empty);
            //     _culledEventsData.Add(new EventNameAndCountData(eventGuid.ToString(), eventPath, count));
            // }
            //
            // _culledEventsData.Sort(CompareDataDescendingCount);
            //
            // _loadedBanksNames = RuntimeManager.LoadedBanksNames.ToList();
        }

        void SortEventsData(List<EventData> data) {
            switch (_sortMode) {
                case SortMode.Count:
                    data.Sort(CompareDataDescendingCount);
                    break;
                case SortMode.ExclusiveMemory:
                    data.Sort(CompareDataDescendingExclusiveMemory);
                    break;
                case SortMode.InclusiveMemory:
                    data.Sort(CompareDataDescendingInclusiveMemory);
                    break;
                case SortMode.SampleDataMemory:
                    data.Sort(CompareDataDescendingSampleDataMemory);
                    break;
                case SortMode.ExclusiveMemoryAll:
                    data.Sort(CompareDataDescendingExclusiveMemoryAll);
                    break;
            }
        }

        void OnEventInstanceCreated(EventInstance eventInstance) {
#if !UNITY_GAMECORE
            WriteStackTrace($"Created event {GetEventPathAndGuidString(eventInstance)}. Instance Id: {eventInstance.handle.ToString()}");
#endif
            RefreshData();
        }

        void OnEventInstanceReleased(EventInstance eventInstance) {
#if !UNITY_GAMECORE
            WriteStackTrace($"Released event {GetEventPathAndGuidString(eventInstance)}. Instance Id: {eventInstance.handle.ToString()}");
#endif
            RefreshData();
        }

        void OnEventStartCulling(StudioEventEmitter emitter) {
#if !UNITY_GAMECORE
            //WriteStackTrace($"Started culling event {GetEventPathAndGuidString(emitter.EventDescription)} from emitter {PathInSceneHierarchy(emitter.gameObject)}");
#endif
            RefreshData();
        }

        void OnEventStopCulling(StudioEventEmitter emitter) {
#if !UNITY_GAMECORE
            //WriteStackTrace($"Stopped culling event {GetEventPathAndGuidString(emitter.EventDescription)} from emitter {PathInSceneHierarchy(emitter.gameObject)}");
#endif
            RefreshData();
        }

        void OnOneShotAdded((EventInstance eventInstance, int currentFrame) dataAndFrame) {
            _oneShotEventDatas.Add((GetOneShotEventData(dataAndFrame.eventInstance), dataAndFrame.currentFrame));
        }

#if !UNITY_GAMECORE
        static void WriteStackTrace(string message) {
            StackTraceStrings[0] = message;
            StackTraceStrings[1] = StackTraceUtility.ExtractStackTrace();
            StackTraceStrings[2] = string.Empty;
            //File.AppendAllLines(FmodEventsRegistry.FilePath, StackTraceStrings);
        }
#endif

        static string GetEventPathAndGuidString(EventInstance eventInstance) {
            //eventInstance.getDescription(out var description);
            //return GetEventPathAndGuidString(description);
            throw new NotImplementedException();
        }

        static string PathInSceneHierarchy(GameObject obj) {
            StringBuilder.Clear();
            StringBuilder.Append(GetObjectName(obj));
            while (obj.transform.parent != null) {
                obj = obj.transform.parent.gameObject;
                string objName = GetObjectName(obj);
                StringBuilder.Insert(0, "/");
                StringBuilder.Insert(0, objName);
            }

            return StringBuilder.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static string GetObjectName(GameObject obj) {
            if (obj.transform.parent != null) {
                return $"{obj.name}${obj.transform.GetSiblingIndex()}$";
            }

            return obj.name;
        }

        static OneShotEventData GetOneShotEventData(EventInstance eventInstance) {
            // if (eventInstance.getDescription(out var description) != RESULT.OK) {
            //     return default;
            // }
            //
            // description.getPath(out var path);
            // description.getID(out var guid);
            // return new OneShotEventData(guid.ToString(), path, eventInstance);
            throw new NotImplementedException();
        }

        void OnLoadedBanksChanged() {
            RefreshData();
        }

        static int CompareDataDescendingCount(EventData x, EventData y) {
            return y.nameAndCountData.count.CompareTo(x.nameAndCountData.count);
        }

        static int CompareDataDescendingCount(EventNameAndCountData x, EventNameAndCountData y) {
            return y.count.CompareTo(x.count);
        }

        static int CompareDataDescendingExclusiveMemory(EventData x, EventData y) {
            return y.ExclusiveMemory.CompareTo(x.ExclusiveMemory);
        }

        static int CompareDataDescendingInclusiveMemory(EventData x, EventData y) {
            return y.InclusiveMemory.CompareTo(x.InclusiveMemory);
        }

        static int CompareDataDescendingSampleDataMemory(EventData x, EventData y) {
            return y.SampleDataMemory.CompareTo(x.SampleDataMemory);
        }

        static int CompareDataDescendingExclusiveMemoryAll(EventData x, EventData y) {
            return y.ExclusiveMemoryAll.CompareTo(x.ExclusiveMemoryAll);
        }

        static int CompareOneShotsCreatedFrameDesc((OneShotEventData data, int frame) x, (OneShotEventData data, int frame) y) {
            return y.frame.CompareTo(x.frame);
        }

        // static bool TryGetEventDescriptionAndGuid(EventInstance eventInstance, out EventDescription description, out GUID guid) {
        //     // if (eventInstance.getDescription(out description) != RESULT.OK) {
        //     //     guid = default;
        //     //     return false;
        //     // }
        //     //
        //     // return description.getID(out guid) == RESULT.OK;
        //     throw new NotImplementedException();
        // }

        enum SortMode : byte {
            Count = 0,
            ExclusiveMemory = 1,
            InclusiveMemory = 2,
            SampleDataMemory = 3,
            ExclusiveMemoryAll = 4,
        }

        struct OneShotEventData {
            public string guid;
            public string path;
            public EventInstance instance;

            public OneShotEventData(string guid, string path, EventInstance instance) {
                this.guid = guid;
                this.path = path;
                this.instance = instance;
            }

            public override string ToString() {
                return $"Guid: {guid}, Path: {path}";
            }
        }

        struct EventNameAndCountData {
            public string guid;
            public string path;
            public int count;

            public EventNameAndCountData(string guid, string path, int count) {
                this.guid = guid;
                this.path = path;
                this.count = count;
            }

            public override string ToString() {
                return $"Guid: {guid}, Path: {path}, Count: {count}";
            }
        }

        struct EventData {
            public EventNameAndCountData nameAndCountData;
            public MEMORY_USAGE memoryUsageMin;
            public MEMORY_USAGE memoryUsageMax;
            public bool isStream;
            public int ExclusiveMemory => memoryUsageMax.exclusive;
            public int InclusiveMemory => memoryUsageMax.inclusive;
            public int SampleDataMemory => memoryUsageMax.sampledata;
            public int ExclusiveMemoryAll => ExclusiveMemory * nameAndCountData.count;

            public EventData(string guid, string path, int count, MEMORY_USAGE memoryUsageMin, MEMORY_USAGE memoryUsageMax, bool isStream) {
                this.nameAndCountData.guid = guid;
                this.nameAndCountData.path = path;
                this.nameAndCountData.count = count;
                this.memoryUsageMin = memoryUsageMin;
                this.memoryUsageMax = memoryUsageMax;
                this.isStream = isStream;
            }
        }

        struct MemoryUsageMinMax {
            public MEMORY_USAGE min;
            public MEMORY_USAGE max;

            public MemoryUsageMinMax(MEMORY_USAGE min, MEMORY_USAGE max) {
                this.min = min;
                this.max = max;
            }

            public override string ToString() {
                return $"Min: {min.ToString()}. Max: {max.ToString()}";
            }
        }
    }
}
#endif