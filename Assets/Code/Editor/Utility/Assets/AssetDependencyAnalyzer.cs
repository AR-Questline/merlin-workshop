using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Awaken.TG.Editor.Debugging.GUIDSearching;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.Assets {
    /// <summary>
    /// Shared utility for analyzing Unity asset dependencies via GUID relationships.
    /// Provides common functionality for finding unused assets and analyzing usage patterns.
    /// </summary>
    public static class AssetDependencyAnalyzer {
        // === Constants
        static readonly Regex GUIDRegex = new("(guid|GUID): ([0-9a-zA-Z]*)", RegexOptions.Compiled);
        static readonly Regex FolderAssetRegex = new("folderAsset: ([a-zA-Z]*)", RegexOptions.Compiled);
        
        // === Configuration
        public class AnalysisConfig {
            public string[] forbiddenDirectories = {"Assets\\Vendor", "Assets\\Plugins", "Assets\\Code", "Assets\\3DAssets\\Scenario01"};
            public string[] ignoredAssetFormats = {"jpg", "jpeg", "png", "mp3", "wav", "mp4", "webm", "tga", "psd", "cs", "mesh", "zip", "gif", "fbx", "obj", "hdr", "exr", "bytes"};
            public readonly bool excludeResources = true;
            public readonly bool excludeScenes = true;
            public int threadCount = 4;

            public static AnalysisConfig Default => new();
        }
        
        // === Results
        public class DependencyMap {
            public Dictionary<string, string[]> filesToGuids = new();
            public Dictionary<string, List<string>> guidToDependents = new();
            public string[] allFiles = Array.Empty<string>();
            public string[] metaFiles = Array.Empty<string>();
            public string[] processedFiles = Array.Empty<string>();
            
            public virtual List<string> GetDependents(string guid) {
                return guidToDependents.TryGetValue(guid, out var deps) ? deps : new List<string>();
            }
            
            public virtual bool IsGuidUsed(string guid) {
                return guidToDependents.ContainsKey(guid) && guidToDependents[guid].Count > 0;
            }
        }
        
        // === Public API
        
        /// <summary>
        /// Fast dependency analysis using existing GUIDCache.
        /// Recommended for template analysis as it reuses cached data.
        /// </summary>
        public static DependencyMap AnalyzeDependenciesFromCache() {
            GUIDCache.Load();
            
            var result = new CachedDependencyMap();
            return result;
        }
        
        /// <summary>
        /// DependencyMap implementation that uses GUIDCache directly without pre-loading all data.
        /// </summary>
        class CachedDependencyMap : DependencyMap {
            public override List<string> GetDependents(string guid) {
                return GUIDCache.Instance?.GetDependent(guid, true).ToList() ?? new List<string>();
            }
            
            public override bool IsGuidUsed(string guid) {
                var dependents = GetDependents(guid);
                return dependents.Count > 0;
            }
        }
        
        public static DependencyMap AnalyzeDependencies(AnalysisConfig config = null, 
            Func<int, int, string, bool> progressCallback = null) {
            
            config ??= AnalysisConfig.Default;
            
            var result = new DependencyMap();
            
            // Collect files
            string assetsDir = Application.dataPath;
            result.allFiles = Directory.GetFiles(assetsDir, "*.*", SearchOption.AllDirectories)
                .Where(f => !config.forbiddenDirectories.Any(f.Contains))
                .ToArray();
            
            result.processedFiles = result.allFiles
                .Where(f => f.Contains(".meta") || !config.ignoredAssetFormats.Any(format => f.ToLower().Contains($".{format}")))
                .ToArray();
            
            result.metaFiles = result.allFiles
                .Where(f => f.Contains(".meta"))
                .Where(f => !config.excludeResources || !f.Contains("Resources"))
                .Where(f => !config.excludeScenes || !f.Contains(".unity"))
                .ToArray();
            
            // Extract GUIDs with progress reporting
            result.filesToGuids = ExtractGuidsFromFiles(result.processedFiles, config, progressCallback);
            if (result.filesToGuids == null) {
                return null; // Cancelled
            }
            
            // Build reverse mapping (GUID -> dependents)
            BuildGuidToDependentsMap(result);
            
            return result;
        }
        
        public static List<string> FindUnusedAssets(DependencyMap dependencies) {
            var unusedFiles = new List<string>();
            
            foreach (string metaFile in dependencies.metaFiles) {
                string content = File.ReadAllText(metaFile);
                if (IsFolderAsset(content)) {
                    continue;
                }
                
                var guids = ExtractGuidsFromFile(metaFile, content, true);
                if (!guids.Any()) continue;
                
                string guid = guids.First();
                if (!dependencies.IsGuidUsed(guid)) {
                    unusedFiles.Add(metaFile.Replace(".meta", ""));
                }
            }
            
            return unusedFiles;
        }
        
        public static List<string> FindUnusedAssetsFromCache(AnalysisConfig config = null) {
            config ??= AnalysisConfig.Default;
            GUIDCache.Load();
            
            var unusedFiles = new List<string>();
            
            // Collect meta files directly
            string assetsDir = Application.dataPath;
            var metaFiles = Directory.GetFiles(assetsDir, "*.meta", SearchOption.AllDirectories)
                .Where(f => !config.forbiddenDirectories.Any(f.Contains))
                .Where(f => !config.ignoredAssetFormats.Any(format => f.ToLower().Contains($".{format}")))
                .Where(f => !config.excludeResources || !f.Contains("Resources"))
                .Where(f => !config.excludeScenes || !f.Contains(".unity"))
                .ToArray();
            
            foreach (string metaFile in metaFiles) {
                string content = File.ReadAllText(metaFile);
                if (IsFolderAsset(content)) {
                    continue;
                }

                var guids = ExtractGuidsFromFile(metaFile, content, true).ToArray();
                if (!guids.Any()) continue;
                
                string guid = guids.First();
                var dependents = GUIDCache.Instance?.GetDependent(guid, true).ToList() ?? new List<string>();
                
                if (dependents.Count == 0) {
                    unusedFiles.Add(metaFile.Replace(".meta", ""));
                }
            }
            
            return unusedFiles;
        }
        
        public static Dictionary<string, List<string>> BuildUsageMap(DependencyMap dependencies) {
            var usageMap = new Dictionary<string, List<string>>();
            
            foreach (string metaFile in dependencies.metaFiles) {
                string content = File.ReadAllText(metaFile);
                if (IsFolderAsset(content)) {
                    continue;
                }
                
                var guids = ExtractGuidsFromFile(metaFile, content, true);
                if (!guids.Any()) continue;
                
                string guid = guids.First();
                string assetPath = metaFile.Replace(".meta", "");
                
                usageMap[assetPath] = dependencies.GetDependents(guid)
                    .Select(f => f.Replace(".meta", ""))
                    .ToList();
            }
            
            return usageMap;
        }
        
        // === Helpers
        static Dictionary<string, string[]> ExtractGuidsFromFiles(string[] files, AnalysisConfig config, 
            Func<int, int, string, bool> progressCallback) {
            
            var filesToGuids = new ConcurrentDictionary<string, string[]>();
            var tasks = new List<Task>();
            var tokenSource = new CancellationTokenSource();
            var currentFiles = new string[config.threadCount];
            int progress = 0;
            
            for (int i = 0; i < config.threadCount; i++) {
                int threadNo = i;
                var task = Task.Run(() => {
                    for (int j = threadNo; j < files.Length; j += config.threadCount) {
                        if (tokenSource.Token.IsCancellationRequested) break;
                        
                        string file = files[j];
                        currentFiles[threadNo] = file;
                        
                        string content = File.ReadAllText(file);
                        string[] guids = ExtractGuidsFromFile(file, content, false).ToArray();
                        filesToGuids.TryAdd(file, guids);
                        
                        Interlocked.Increment(ref progress);
                    }
                    currentFiles[threadNo] = "";
                }, tokenSource.Token);
                
                tasks.Add(task);
            }
            
            // Progress monitoring
            while (!tasks.All(t => t.IsCompleted)) {
                if (progressCallback != null) {
                    string filesString = string.Join(" ", currentFiles.Select(Path.GetFileName).Where(s => !string.IsNullOrEmpty(s)));
                    bool cancelled = progressCallback(progress, files.Length, filesString);
                    if (cancelled) {
                        tokenSource.Cancel();
                        return null;
                    }
                }
                Thread.Sleep(100);
            }
            
            return filesToGuids.ToDictionary(d => d.Key, d => d.Value);
        }
        
        static void BuildGuidToDependentsMap(DependencyMap result) {
            result.guidToDependents.Clear();
            
            foreach (var kvp in result.filesToGuids) {
                foreach (string guid in kvp.Value) {
                    if (!result.guidToDependents.ContainsKey(guid)) {
                        result.guidToDependents[guid] = new List<string>();
                    }
                    result.guidToDependents[guid].Add(kvp.Key);
                }
            }
        }
        
        public static IEnumerable<string> ExtractGuidsFromFile(string fileName, string content, bool returnSelf) {
            var foundGuids = new HashSet<string>();
            var matches = GUIDRegex.Matches(content).Cast<Match>().Where(m => m.Success);
            
            if (!returnSelf && fileName.Contains(".meta")) {
                // Skip first GUID in meta file (file's own GUID)
                matches = matches.Skip(1);
            }
            
            foreach (Match match in matches) {
                string guid = match.Groups[2].Value;
                if (!string.IsNullOrEmpty(guid)) {
                    foundGuids.Add(guid);
                }
            }
            
            return foundGuids;
        }
        
        public static bool IsFolderAsset(string content) {
            return FolderAssetRegex.IsMatch(content);
        }
    }
}