using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Awaken.TG.Editor.Assets;
using Awaken.TG.Main.General.Caches;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using ThreadPriority = System.Threading.ThreadPriority;

namespace Awaken.TG.Editor.Debugging.GUIDSearching {
    public class GUIDCache : BaseCache {
        const string Path = "Assets/Vendor/GUIDSearch/GUIDSearchingCache.asset";

        MultiMap<string, string> _cache = new(20000);
        HashSet<string> _unusedCache = new(5000);
        
        [SerializeField][FoldoutGroup("Serialized Data")] List<string> _serializedCache = new();
        [SerializeField][FoldoutGroup("Serialized Data")] List<int> _keyIndexes = new();
        [SerializeField][FoldoutGroup("Serialized Data")] List<string> _unusedGuids = new();

        public HashSet<string> UnusedCache => _unusedCache;
        
        // == Singleton

        static GUIDCache s_instance;
        static readonly Regex GUIDRegex = new(@"[^a-z0-9]([a-z0-9]{32})[^a-z0-9]", RegexOptions.Compiled);
        static readonly Regex RichEnumRegex = new(@"(Awaken\.[A-Za-z0-9_\.]+),\s+([A-Za-z0-9_\.]+),\s+(Version=\d+\.\d+\.\d+\.\d+),\s+(Culture=[a-z]+),\s+(PublicKeyToken=null):(\w+)", RegexOptions.Compiled);
        static readonly Regex IdOverrideRegex = new("IdOverride: (.+)", RegexOptions.Compiled);
        public static GUIDCache Instance => s_instance;

        public static void Load() {
            if (s_instance == null) { 
                s_instance = AssetDatabase.LoadAssetAtPath<GUIDCache>(Path); 
                s_instance?.LoadMultiMap();
                s_instance?.LoadUnusedCache();
            }
        }
        
        public static void Unload() {
            Resources.UnloadAsset(s_instance);
            s_instance = null;
        }

        public override void Clear() {
            _cache.Clear();
            _unusedCache.Clear();
        }

        // == Dependencies

        public bool IsUnused(string guid) {
            return _unusedCache.Contains(guid);
        }

        public bool IsUnused(Object obj) {
            string guid = GetGuid(obj);
            return guid == null || _unusedCache.Contains(guid);
        }

        public IEnumerable<string> GetDependent(string guid, bool ignoreIrrelevant = false) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            var values = _cache.GetValues(guid, true);
            if (ignoreIrrelevant) {
                return values.Where(IgnoreIrrelevant);
            } else {
                return values;
            }
            
            bool IgnoreIrrelevant(string p) {
                string pathAltSlash = p.Replace('\\', '/');
                
                return !p.Contains("Addressable") // not addressable
                       && pathAltSlash != Path // not guid cache itself
                       && pathAltSlash != path // not object it self
                       && !p.Contains("Assets\\Localizations\\") // not localizations
                       && !p.EndsWith(".meta") // not its meta file
                       && !p.Contains("SceneConfigs"); // not scenes config that holds all scenes (used and unused)
            }
        }
        
        public IEnumerable<string> GetDependent(Object obj, bool ignoreIrrelevant = false) {
            string guid = GetGuid(obj);
            return guid != null ? GetDependent(guid, ignoreIrrelevant) : Enumerable.Empty<string>();
        }

        public IEnumerable<string> GetIdOverrideUsages(string idOverride) {
            return _cache.GetValues(idOverride, true);
        }
        
        // == Refresh

        [Button(ButtonSizes.Large)]
        public void Refresh(int threadNum = 8) {
            Clear();
            
            var paths = GUIDSearchUtils.GetValidPaths(threadNum);
            var count = paths.Length;

            int index = -1;

            var threads = new Thread[threadNum];
            var threadMaps = new MultiMap<string, string>[threadNum];
            for (int i = 0; i < threadNum; i++) {
                threadMaps[i] = new MultiMap<string, string>();
                threads[i] = new Thread(Process(threadMaps[i]));
                threads[i].Priority = ThreadPriority.Highest;
                threads[i].Start();
            }
            var mainMap = new MultiMap<string, string>();
            Process(mainMap)();
            while (threads.Any(thread => thread.IsAlive)) {
                Thread.Sleep(100);
            }
            
            _cache.Merge(mainMap);
            foreach (var map in threadMaps) {
                _cache.Merge(map);
            }
            
            SaveMultimap();
            FindUnusedAssets();
            
            MarkBaked();

            ThreadStart Process(MultiMap<string, string> map) => () => {
                while (true) {
                    var myIndex = Interlocked.Increment(ref index);
                    if (myIndex >= count) {
                        break;
                    }
                    var path = paths[myIndex];
                    FindInFile(path, map);
                }
            };
        }

        void FindUnusedAssets() {
            _unusedGuids.Clear();
            _unusedCache.Clear();
            
            var addressableCleaner = new AddressablesCleaner.Cleaner();
            addressableCleaner.FindUnusedAddressables();
            _unusedGuids.AddRange(addressableCleaner.unusedAssetsSimple.Select(GetGuid));
            _unusedGuids.AddRange(addressableCleaner.unusedAssetsRecursive.Select(GetGuid));
            _unusedCache.AddRange(_unusedGuids);
        }

        static string GetGuid(Object obj) {
            if (obj == null) {
                return null;
            }
            
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out string guid, out _)) {
                return guid;
            } else {
                return null;
            }
        }

        static void FindInFile(string path, MultiMap<string, string> map) {
            var text = File.ReadAllText(path);
            foreach (Match match in GUIDRegex.Matches(text)) {
                var guid = match.Value.Substring(1, 32);
                map.Add(guid, path);
            }
            foreach (Match match in RichEnumRegex.Matches(text)) {
                var groups = match.Groups;
                var richEnum = $"{groups[1].Value}, {groups[2].Value}, {groups[3].Value}, {groups[4].Value}, {groups[5].Value}:{groups[6].Value}";
                map.Add(richEnum, path);
            }

            foreach (Match match in IdOverrideRegex.Matches(text)) {
                var idOverride = match.Groups[1].Value;
                map.Add(idOverride, path);
            }
        }

        // == Serialization
        
        void SaveMultimap() {
            _serializedCache.Clear();
            _keyIndexes.Clear();

            int index = 0;
            foreach (var pair in _cache) {
                _serializedCache.Add(pair.Key);
                _keyIndexes.Add(index);
                index++;
                foreach (var dependent in pair.Value) {
                    _serializedCache.Add(dependent);
                    index++;
                }
            }
        }

        void LoadMultiMap() {
            _cache.Clear();
            
            int nextKeyIndex = 0;
            string currentKey = "";
            for (int i = 0; i < _serializedCache.Count; i++) {
                if (nextKeyIndex < _keyIndexes.Count && i == _keyIndexes[nextKeyIndex]) {
                    currentKey = _serializedCache[i];
                    nextKeyIndex++;
                } else {
                    _cache.Add(currentKey, _serializedCache[i]);
                }
            }
        }

        void LoadUnusedCache() {
            _unusedCache.Clear();
            _unusedCache.AddRange(_unusedGuids);
        }
    }
}