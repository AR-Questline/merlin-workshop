using System;
using System.Collections.Generic;
using System.IO;
using Awaken.TG.EditorOnly;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Archives;
using Awaken.Utility.Collections;
using Awaken.Utility.Files;
using Awaken.Utility.LowLevel;
using Unity.VisualScripting;
using UnityEngine;
using Log = Awaken.Utility.Debugging.Log;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Skills {
    public class StreamedSkillGraphs : IDomainBoundService {
        public const string SubdirectoryName = "Skills";
        public const string ArchiveFileName = "skills.arch";

        public Domain Domain => Domain.Gameplay;
        public bool RemoveOnDomainChange() {
            foreach (var entry in _entries) {
                Object.Destroy(entry.Value.graph);
            }
            _entries.Clear();
            return true;
        }

        public static readonly string BakingDirectoryPath = Path.Combine("Library", SubdirectoryName);

        readonly Dictionary<Guid, Entry> _entries = new();
        string _basePath;

        public StreamedSkillGraphs() {
            _basePath = BakingDirectoryPath;
            var success = ArchiveUtils.TryMountAndAdjustPath("Skills", SubdirectoryName, ArchiveFileName, ref _basePath);
            if (!success) {
                Log.Critical?.Error($"Skills merged archive not found at {Path.Combine(Application.streamingAssetsPath, SubdirectoryName, ArchiveFileName)}");
            }
        }

        public ScriptGraphAsset Get(Guid guid) {
            if (guid == Guid.Empty) {
                return null;
            }
#if UNITY_EDITOR && !ARCHIVES_PRODUCED
            if (UnityEditor.EditorPrefs.GetInt("skills_from_streaming_assets") == 0) {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid.ToUnityGuid());
                return UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptGraphAsset>(path);
            }
#endif
            if (_entries.TryGetValue(guid, out var entry) == false) {
                entry = LoadGraph(guid);
            }
            entry.refCount++;
            _entries[guid] = entry;
            return entry.graph;
        }

        public void Release(Guid guid) {
            if (guid == Guid.Empty) {
                return;
            }
#if UNITY_EDITOR
            if (UnityEditor.EditorPrefs.GetInt("skills_from_streaming_assets") == 0) {
                return;
            }
#endif
            if (_entries.TryGetValue(guid, out var entry) == false) {
                Log.Important?.Error($"Trying to release a graph {guid} that was never loaded");
                return;
            }
            entry.refCount--;
            if (entry.refCount == 0) {
                _entries.Remove(guid);
                Object.Destroy(entry.graph);
                foreach (var dependency in entry.dependencies) {
                    Release(dependency);
                }
            } else {
                _entries[guid] = entry;
            }
        }

        unsafe Entry LoadGraph(Guid guid) {
            var filePath = Path.Combine(_basePath, $"{guid:N}.skill");
            var fileContent = FileRead.ToNewBuffer<byte>(filePath, ARAlloc.Temp);
            var reader = new BufferStreamReader(fileContent);
            
            var dependencyCount = reader.Read<int>();
            var dependencies = new Guid[dependencyCount];
            for (int i = 0; i < dependencyCount; i++) {
                dependencies[i] = reader.Read<Guid>();
            }
            var charSpan = reader.ReadRest<char>();
            var json = new string(charSpan.Ptr, 0, (int)charSpan.Length);
            
            fileContent.Dispose();
            
            var dependencyObjects = new Object[dependencyCount];
            for (int i = 0; i < dependencyCount; i++) {
                dependencyObjects[i] = Get(dependencies[i]);
            }
            
            var asset = ScriptableObject.CreateInstance<ScriptGraphAsset>();
            asset.Deserialize(json, dependencyObjects);
            asset.name = guid.ToString("N");
            
            return new Entry {
                graph = asset,
                dependencies = dependencies,
            };
        }
        
        struct Entry {
            public int refCount;
            public ScriptGraphAsset graph;
            public Guid[] dependencies;
        }
    }
}