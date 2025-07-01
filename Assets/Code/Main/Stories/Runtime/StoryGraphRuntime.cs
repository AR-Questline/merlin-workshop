using System;
using System.IO;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps;
using Awaken.Utility.Archives;
using Awaken.Utility.Debugging;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Runtime {
    public struct StoryGraphRuntime {
        public const string SubdirectoryName = "Stroy";
        public const string ArchiveFileName = "story.arch";

        public static readonly string BakingDirectoryPath = Path.Combine("Library", SubdirectoryName);

        public string guid;
        public string[] usedSoundBanksNames;
        public string[] tags;
        public VariableDefine[] variables;
        public VariableReferenceDefine[] variableReferences;
        public bool sharedBetweenMultipleNPCs;
        
        [CanBeNull] public StartNode startNode;
        public StoryChapter[] chapters;
        public StoryConditions[] conditions;

        public bool IsCreated => guid != null;
        [CanBeNull]
        public StoryChapter InitialStoryChapter => startNode?.continuation;

        static string s_basePath;
        static string GetBasePath() {
            if (s_basePath != null) {
                return s_basePath;
            }
            s_basePath = BakingDirectoryPath;
            var success = ArchiveUtils.TryMountAndAdjustPath("Story", SubdirectoryName, ArchiveFileName, ref s_basePath);
            if (!success) {
                Log.Critical?.Error($"Stories merged archive not found at {Path.Combine(Application.streamingAssetsPath, SubdirectoryName, ArchiveFileName)}");
            }
            return s_basePath;
        }

        public static StoryGraphRuntime? Get(string guid) {
            try {
                string path;
#if UNITY_EDITOR && !ARCHIVES_PRODUCED
                if (UnityEditor.EditorPrefs.GetInt("story_from_streaming_assets", 0) == 0) {
                    var editorGraph = UnityEditor.AssetDatabase.LoadAssetAtPath<StoryGraph>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                    var intermediateGraph = StoryGraphParser.Parse(editorGraph);
                    if (UnityEditor.EditorPrefs.GetInt("story_intermediate_assets", 1) == 1) {
                        editorGraph.ResetGraphCache();
                        return intermediateGraph;
                    }
                    path = Path.Combine(Application.temporaryCachePath, "story.story");
                    StoryWriter.Write(intermediateGraph, path);
                    intermediateGraph.Dispose();
                } else
#endif
                {
                    path = Path.Combine(GetBasePath(), $"{guid}.story");
                }

                return StoryReader.Read(guid, path);
            } catch (Exception) {
#if UNITY_EDITOR
                Log.Critical?.Error($"Failed to read story graph with guid: {guid}");
#endif
                return null;
            }
        }
        
        public void Dispose() {}

        public readonly bool TryGetStart(StoryBookmark bookmark, out StorySettings settings, out StoryChapter chapter) {
            if (string.IsNullOrWhiteSpace(bookmark.chapterName) == false) {
                var (sChapter, sBookmark) = Bookmark(bookmark.chapterName);
                chapter = sChapter;
                if (sBookmark == null) {
                    settings = default;
                    return false;
                }
                if (sBookmark.storySettings) {
                    settings = new StorySettings {
                        involveHero = sBookmark.involveHero,
                        involveAI = sBookmark.involveAI,
                    };
                } else if (startNode != null) {
                    settings = new StorySettings {
                        involveHero = startNode.involveHero,
                        involveAI = startNode.involveAI,
                    };
                } else {
                    settings = new StorySettings {
                        involveHero = false,
                        involveAI = false,
                    };
                }
                return true;
            }

            if (startNode != null) {
                settings = new StorySettings {
                    involveHero = startNode.involveHero,
                    involveAI = startNode.involveAI,
                };
                if (startNode.choices.Length == 0) {
                    chapter = startNode.continuation;
                } else if (startNode.choices.Length == 1) {
                    chapter = startNode.choices[0].targetChapter;
                } else {
                    chapter = startNode.continuation;
                    Log.Critical?.Error("Multiple choices in start node are not supported. Pleas notify programmer to add support.");
                }

                return true;
            }

            settings = default;
            chapter = null;
            return false;
        }
        
        public readonly StoryChapter BookmarkedChapter(string name) => Bookmark(name).chapter;
        
        readonly (StoryChapter chapter, SBookmark bookmark) Bookmark(string name) {
            foreach (var chapter in chapters) {
                foreach (var step in chapter.steps) {
                    if (step is SBookmark bookmark && bookmark.flag == name) {
                        return (chapter, bookmark);
                    }
                }
            }
            return (null, null);
        }

        public struct StorySettings {
            public bool involveHero;
            public bool involveAI;
        }
    }
}