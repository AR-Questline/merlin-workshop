using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Editor.Utility.Assets {
    public class FindUnusedAssets {
        static readonly int ThreadCount = 4;
        static readonly string[] ForbiddenDirectories = {"Assets\\Vendor", "Assets\\Plugins", "Assets\\Code", "Assets\\3DAssets\\Scenario01"};
        static readonly string[] IgnoredAssetFormats = {"jpg", "jpeg", "png", "mp3", "wav", "mp4", "webm", "tga", "psd", "cs", "mesh", "zip", "gif", "fbx", "obj", "hdr", "exr", "bytes"};
        public static readonly string UnusedAssetsFileName = "UnusedAssets.txt";
        static readonly string AssetsWithUsagesFileName = "AssetsUsages.txt";

        static int s_progress = 0;
        static int s_count = 0;

        [MenuItem("TG/Assets/Find Unused Assets", priority = -100)]
        public static void FindUnused() {
            FindUnused(true);
        }

        public static void FindUnused(bool showInfo) {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            const float operationsCount = 6f;
            EditorUtility.DisplayProgressBar("Find Unused Assets", "Collecting data", 0);
            // construct collections
            string assetsDir = Application.dataPath;
            string[] files = Directory.GetFiles(assetsDir, "*.*", SearchOption.AllDirectories)
                .Where(f => !ForbiddenDirectories.Any(f.Contains)).Where( f => !f.Contains(UnusedAssetsFileName) ).ToArray();
            EditorUtility.DisplayProgressBar("Find Unused Assets", "Collecting data", 1/operationsCount);
            string[] filesWithGuids = files
                .Where(f => f.Contains(".meta") || !IgnoredAssetFormats.Any(format => f.ToLower().Contains($".{format}")))
                .ToArray();
            EditorUtility.DisplayProgressBar("Find Unused Assets", "Collecting data", 2/operationsCount);
            string[] metaFiles = files
                .Where(m => m.Contains(".meta") && !m.Contains("Resources") && !m.Contains(".unity"))
                .ToArray();
            EditorUtility.DisplayProgressBar("Find Unused Assets", "Collecting data", 3/operationsCount);
            List<string> unusedFiles = new List<string>();

            // reading guids
            var filesToGuids = ExtractGuids(filesWithGuids);
            if (filesToGuids == null) {
                EditorUtility.ClearProgressBar();
                return;
            }
            
            EditorUtility.DisplayProgressBar("Find Unused Assets", "Processing data", 4/operationsCount);
            HashSet<string> usedGuids = filesToGuids.Values.Aggregate(new HashSet<string>(), (a,b) => {
                foreach (string str in b) {
                    a.Add(str);
                }
                return a;
            });

            EditorUtility.DisplayProgressBar("Find Unused Assets", "Processing data", 5/operationsCount);
            // iterate meta files to find unused
            foreach (string metaFile in metaFiles) {
                string content = File.ReadAllText(metaFile);
                if (IsFolderAsset(content)) {
                    continue;
                }
                string guid = ExtractGuidsFromFile(metaFile, content, true).First();
                bool exists = usedGuids.Contains(guid);
                if (!exists) {
                    unusedFiles.Add(metaFile.Replace(".meta", ""));
                }
            }

            EditorUtility.DisplayProgressBar("Find Unused Assets", "Saving data", 6/operationsCount);
            // write to file
            string unusedAssetsPath = $"{Application.dataPath}/{UnusedAssetsFileName}"; 
            File.Delete(unusedAssetsPath);
            File.WriteAllText(unusedAssetsPath, string.Join("\n", unusedFiles));
            
            EditorUtility.ClearProgressBar();
            
            // show info
            if (showInfo) {
                EditorUtility.DisplayDialog("Success", $"Unused assets saved in {unusedAssetsPath}", "Ok");
            }
            
            // debug
            watch.Stop();
            if (showInfo) {
                Log.Important?.Info("Time: " + watch.ElapsedMilliseconds);

                Log.Important?.Info("All count: " + files.Length);
                Log.Important?.Info("Assets count: " + filesWithGuids.Length);
                Log.Important?.Info("Metas count: " + metaFiles.Length);
                Log.Important?.Info("Unused count: " + unusedFiles.Count);
            }
        }

        [MenuItem("TG/Assets/Find Assets Usages", priority = -100)]
        public static void FindUsages() {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            // construct collections
            string assetsDir = Application.dataPath;
            string[] files = Directory.GetFiles(assetsDir, "*.*", SearchOption.AllDirectories)
                .Where(f => !ForbiddenDirectories.Any(f.Contains)).Where(f => !f.Contains(AssetsWithUsagesFileName)).ToArray();
            string[] filesWithGuids = files
                .Where(f => f.Contains(".meta") || !IgnoredAssetFormats.Any(format => f.ToLower().Contains($".{format}")))
                .ToArray();
            string[] metaFiles = files
                .Where(m => m.Contains(".meta") && !m.Contains("Resources") && !m.Contains(".unity"))
                .ToArray();
            Dictionary<string, List<string>> filesWithUsages = new Dictionary<string, List<string>>();

            // reading guids
            var filesToGuids = ExtractGuids(filesWithGuids);
            if (filesToGuids == null) {
                EditorUtility.ClearProgressBar();
                return;
            }

            // iterate meta files to construct usages dictionary
            foreach (string metaFile in metaFiles) {
                string content = File.ReadAllText(metaFile);
                if (IsFolderAsset(content)) {
                    continue;
                }

                string guid = ExtractGuidsFromFile(metaFile, content, true).First();
                string key = metaFile.Replace(".meta", "");
                filesWithUsages[key] = new List<string>();

                foreach (var kvp in filesToGuids) {
                    if (kvp.Value.Contains(guid)) {
                        filesWithUsages[key].Add(kvp.Key.Replace(".meta", ""));
                    }
                }
            }

            // write to file
            string assetsUsagesPath = $"{Application.dataPath}/{AssetsWithUsagesFileName}";
            File.Delete(assetsUsagesPath);

            using (StreamWriter writer = new StreamWriter(assetsUsagesPath)) {
                foreach (var kvp in filesWithUsages) {
                    writer.WriteLine(kvp.Key);
                    foreach (string value in kvp.Value) {
                        writer.WriteLine($"\t{value}");
                    }
                }
            }

            EditorUtility.ClearProgressBar();

            // show info
            EditorUtility.DisplayDialog("Success", $"Assets usages saved in {assetsUsagesPath}", "Ok");

            // debug
            watch.Stop();
            Log.Important?.Info("Time: " + watch.ElapsedMilliseconds);

            Log.Important?.Info("All count: " + files.Length);
            Log.Important?.Info("Assets count: " + filesWithGuids.Length);
            Log.Important?.Info("Metas count: " + metaFiles.Length);
        }

        
        // === Helpers
        
        static Dictionary<string, string[]> ExtractGuids(string[] files) {
            ConcurrentDictionary<string, string[]> filesToGuids = new ConcurrentDictionary<string, string[]>();
            List<Task> tasks = new List<Task>();
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            bool canceled = false;
            s_progress = 0;
            s_count = files.Length;
            
            string[] currentFiles = new string[ThreadCount];

            for (int i = 0; i < ThreadCount; i++) {
                int threadNo = i;
                Task task = Task.Run(() => {
                    for (int j = threadNo; j < files.Length; j += ThreadCount) {
                        string file = files[j];
                        currentFiles[threadNo] = file;
                        string[] guids = ExtractGuidsFromFile(file, File.ReadAllText(file), false).ToArray();
                        filesToGuids.TryAdd(file, guids);
                        s_progress++;
                    }

                    currentFiles[threadNo] = "";
                }, tokenSource.Token);
                tasks.Add(task);
            }

            while (!tasks.All(t => t.IsCompleted) && !canceled) {
                string filesString = string.Join(" ", currentFiles.Select(Path.GetFileName));
                canceled = EditorUtility.DisplayCancelableProgressBar("Finding assets", $"{s_progress}/{s_count} {filesString}", (float) s_progress / s_count);
            }
            
            if (canceled) {
                tokenSource.Cancel();
                return null;
            }

            return filesToGuids.ToDictionary(d => d.Key, d => d.Value);
        }

        static readonly Regex GUIDRegex = new Regex(@"(guid|GUID): ([0-9a-zA-Z]*)", RegexOptions.Compiled);
        static readonly Regex FolderAssetRegex = new Regex(@"folderAsset: ([a-zA-Z]*)", RegexOptions.Compiled);

        static IEnumerable<string> ExtractGuidsFromFile(string fileName, string content, bool returnSelf) {
            HashSet<string> foundGuids = new HashSet<string>();

            IEnumerable<Match> matches = GUIDRegex.Matches(content).Cast<Match>().Where(m => m.Success);

            if (!returnSelf && fileName.Contains(".meta")) {
                // first guid in meta file is file's own guid
                matches = matches.Skip(1);
            }
            
            foreach (Match match in matches) {
                string guid = match.Groups[2].Value;
                foundGuids.Add(guid);
            }

            return foundGuids;
        }

        static bool IsFolderAsset(string content) {
            return FolderAssetRegex.IsMatch(content);
        }
    }
}