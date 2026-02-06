using System.Diagnostics;
using System.IO;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.Assets {
    public class FindUnusedAssets {
        static readonly int ThreadCount = 4;
        static readonly string[] ForbiddenDirectories = {"Assets\\Vendor", "Assets\\Plugins", "Assets\\Code", "Assets\\3DAssets\\Scenario01"};
        static readonly string[] IgnoredAssetFormats = {"jpg", "jpeg", "png", "mp3", "wav", "mp4", "webm", "tga", "psd", "cs", "mesh", "zip", "gif", "fbx", "obj", "hdr", "exr", "bytes"};
        public static readonly string UnusedAssetsFileName = "UnusedAssets.txt";
        static readonly string AssetsWithUsagesFileName = "AssetsUsages.txt";


        [MenuItem("TG/Assets/Find Unused Assets", priority = -100)]
        public static void FindUnused() {
            FindUnused(true);
        }

        [MenuItem("TG/Assets/Find Unused Assets (Fast Cache)", priority = -99)]
        static void FindUnusedFast() {
            FindUnusedUsingCache(true);
        }

        static void FindUnused(bool showInfo) {
            Stopwatch watch = new();
            watch.Start();

            // Configure analysis
            var config = new AssetDependencyAnalyzer.AnalysisConfig {
                forbiddenDirectories = ForbiddenDirectories,
                ignoredAssetFormats = IgnoredAssetFormats,
                threadCount = ThreadCount,
            };

            // Analyze dependencies with progress callback
            var dependencies = AssetDependencyAnalyzer.AnalyzeDependencies(config, 
                (progress, total, currentFiles) => {
                    return EditorUtility.DisplayCancelableProgressBar(
                        "Finding assets", 
                        $"{progress}/{total} {currentFiles}", 
                        (float)progress / total
                    );
                });

            if (dependencies == null) {
                EditorUtility.ClearProgressBar();
                return; // Cancelled
            }

            // Find unused assets
            EditorUtility.DisplayProgressBar("Find Unused Assets", "Processing results", 0.8f);
            var unusedFiles = AssetDependencyAnalyzer.FindUnusedAssets(dependencies);

            // Save results
            EditorUtility.DisplayProgressBar("Find Unused Assets", "Saving data", 0.9f);
            string unusedAssetsPath = $"{Application.dataPath}/{UnusedAssetsFileName}"; 
            File.Delete(unusedAssetsPath);
            File.WriteAllText(unusedAssetsPath, string.Join("\n", unusedFiles));
            
            EditorUtility.ClearProgressBar();
            
            // Show info
            if (showInfo) {
                EditorUtility.DisplayDialog("Success", $"Unused assets saved in {unusedAssetsPath}", "Ok");
            }
            
            // Debug info
            watch.Stop();
            if (showInfo) {
                Log.Important?.Info("Time: " + watch.ElapsedMilliseconds);
                Log.Important?.Info("All count: " + dependencies.allFiles.Length);
                Log.Important?.Info("Assets count: " + dependencies.processedFiles.Length);
                Log.Important?.Info("Metas count: " + dependencies.metaFiles.Length);
                Log.Important?.Info("Unused count: " + unusedFiles.Count);
            }
        }

        [MenuItem("TG/Assets/Find Assets Usages", priority = -100)]
        public static void FindUsages() {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            // Configure analysis
            var config = new AssetDependencyAnalyzer.AnalysisConfig {
                forbiddenDirectories = ForbiddenDirectories,
                ignoredAssetFormats = IgnoredAssetFormats,
                threadCount = ThreadCount,
            };

            // Analyze dependencies with progress callback
            var dependencies = AssetDependencyAnalyzer.AnalyzeDependencies(config, 
                (progress, total, currentFiles) => {
                    return EditorUtility.DisplayCancelableProgressBar(
                        "Analyzing asset usages", 
                        $"{progress}/{total} {currentFiles}", 
                        (float)progress / total
                    );
                });

            if (dependencies == null) {
                EditorUtility.ClearProgressBar();
                return; // Cancelled
            }

            // Build usage map
            var usageMap = AssetDependencyAnalyzer.BuildUsageMap(dependencies);

            // Write to file
            string assetsUsagesPath = $"{Application.dataPath}/{AssetsWithUsagesFileName}";
            File.Delete(assetsUsagesPath);

            using (StreamWriter writer = new StreamWriter(assetsUsagesPath)) {
                foreach (var kvp in usageMap) {
                    writer.WriteLine(kvp.Key);
                    foreach (string value in kvp.Value) {
                        writer.WriteLine($"\t{value}");
                    }
                }
            }

            EditorUtility.ClearProgressBar();

            // Show info
            EditorUtility.DisplayDialog("Success", $"Assets usages saved in {assetsUsagesPath}", "Ok");

            // Debug info
            watch.Stop();
            Log.Important?.Info("Time: " + watch.ElapsedMilliseconds);
            Log.Important?.Info("All count: " + dependencies.allFiles.Length);
            Log.Important?.Info("Assets count: " + dependencies.processedFiles.Length);
            Log.Important?.Info("Metas count: " + dependencies.metaFiles.Length);
        }

        public static void FindUnusedUsingCache(bool showInfo) {
            Stopwatch watch = new();
            watch.Start();

            // Use fast cache-based analysis
            EditorUtility.DisplayProgressBar("Find Unused Assets (Cache)", "Loading dependency cache...", 0.5f);
            
            var config = new AssetDependencyAnalyzer.AnalysisConfig {
                forbiddenDirectories = ForbiddenDirectories,
                ignoredAssetFormats = IgnoredAssetFormats
            };

            // Find unused assets directly from cache
            EditorUtility.DisplayProgressBar("Find Unused Assets (Cache)", "Finding unused assets...", 0.8f);
            var unusedFiles = AssetDependencyAnalyzer.FindUnusedAssetsFromCache(config);

            // Save results
            EditorUtility.DisplayProgressBar("Find Unused Assets (Cache)", "Saving results...", 0.9f);
            string unusedAssetsPath = $"{Application.dataPath}/{UnusedAssetsFileName}";
            File.Delete(unusedAssetsPath);
            File.WriteAllText(unusedAssetsPath, string.Join("\n", unusedFiles));

            EditorUtility.ClearProgressBar();

            // Show info
            if (showInfo) {
                EditorUtility.DisplayDialog("Success", $"Unused assets saved in {unusedAssetsPath}\n\nFound {unusedFiles.Count} unused assets.", "Ok");
            }

            // Debug info
            watch.Stop();
            if (showInfo) {
                Log.Important?.Info("Time (Cache): " + watch.ElapsedMilliseconds + "ms");
                Log.Important?.Info("Unused count: " + unusedFiles.Count);
            }
        }
    }
}