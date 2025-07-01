using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Saving.Utils {
    public static class AtomicDirectoryWriter {
        static readonly List<string> ProcessedDirectories = new();

        public static void Begin(string directory) {
            directory = UnifyPath(directory);
            if (ProcessedDirectories.Contains(directory)) {
                Log.Critical?.Error($"Trying to do atomic directory write on the same directory twice. Directory: {directory}");
                return;
            }
            
            var tempDirectoryDirty = TempDirectoryPathDirty(directory);
            
            if (Directory.Exists(tempDirectoryDirty)) {
                Directory.Delete(tempDirectoryDirty, true);
            }
            
            Directory.CreateDirectory(tempDirectoryDirty);
            ProcessedDirectories.Add(directory);
        }

        public static async UniTask<bool> End(string directory) {
            directory = UnifyPath(directory);
            if (!ProcessedDirectories.Remove(directory)) {
                Log.Critical?.Error($"Ending atomic directory write to directory without beginning it. Directory: {directory}");
                return false;
            }
            var tempDirectoryDirty = TempDirectoryPathDirty(directory);
            if (!Directory.Exists(tempDirectoryDirty)) {
                if (Directory.Exists(directory)) {
                    await TryDeleteDirectory(directory);
                }
                return false;
            }
            
            bool hasAnyFile = Directory.EnumerateFileSystemEntries(tempDirectoryDirty).Any();
            if (!hasAnyFile) {
                var dirtyTask = TryDeleteDirectory(tempDirectoryDirty);
                if (Directory.Exists(directory)) {
                    var cleanTask = TryDeleteDirectory(directory);
                    await UniTask.WhenAll(dirtyTask, cleanTask);
                } else {
                    await dirtyTask;
                }
                return false;
            }

            bool success;
            if (Directory.Exists(directory)) {
                var tempDirectoryClean = TempDirectoryPathClean(directory);
                success = await TryMoveDirectory(tempDirectoryDirty, tempDirectoryClean);
                if (!success) {
                    return false;
                }
                success = await TryDeleteDirectory(directory);
                if (!success) {
                    return false;
                }
                success = await TryMoveDirectory(tempDirectoryClean, directory);
            } else {
                success = await TryMoveDirectory(tempDirectoryDirty, directory);
            }

            return success;
        }
        
        public static string AdjustPath(string path) {
            if (ProcessedDirectories.Count == 0) {
                return path;
            }
            path = UnifyPath(path);
            foreach (var processedDirectory in ProcessedDirectories) {
                if (path.StartsWith(processedDirectory)) {
                    return AdjustPath(path, processedDirectory);
                }
            }
            return path;
        }

        public static void EnsureNoTempDirectories(string parentDirectory) {
            if (!Directory.Exists(parentDirectory)) {
                return;
            }
            var directories = Directory.GetDirectories(parentDirectory, "*", SearchOption.AllDirectories);
            foreach (var directory in directories) {
                if (IsTempDirectoryClean(directory)) {
                    var original = OriginalDirectoryFromTempClean(directory);
                    Directory.Delete(original, true);
                    Directory.Move(directory, original);
                } else if (IsTempDirectoryDirty(directory)) {
                    Directory.Delete(directory, true);
                }
            }
        }

        static string TempDirectoryPathDirty(string directory) {
            return directory + "~";
        }

        static string TempDirectoryPathClean(string directory) {
            return directory + "~~";
        }

        static bool IsTempDirectoryDirty(string directory) {
            return directory.EndsWith('~');
        }
        
        static bool IsTempDirectoryClean(string directory) {
            return directory.EndsWith("~~");
        }

        [UnityEngine.Scripting.Preserve]
        static string OriginalDirectoryFromTempDirty(string tempDirectoryDirty) {
            return tempDirectoryDirty[..^1];
        }
        
        static string OriginalDirectoryFromTempClean(string tempDirectoryClean) {
            return tempDirectoryClean[..^2];
        }
        
        static string AdjustPath(string path, string processedDirectory) {
            int processedDirectoryLength = processedDirectory.Length;
            Span<char> adjusted = stackalloc char[path.Length + 1];
            processedDirectory.AsSpan().CopyTo(adjusted);
            adjusted[processedDirectoryLength] = '~';
            if (processedDirectoryLength < path.Length) {
                path.AsSpan()[processedDirectoryLength..].CopyTo(adjusted[(processedDirectoryLength + 1)..]);
            }
            return new string(adjusted);
        }

        static string UnifyPath(string path) {
            return path.Replace('/', '\\');
        }

        static async UniTask<bool> TryMoveDirectory(string sourceDirName, string destDirName, int maxAttempts = 5) {
            bool success = false;
            int counter = 0;
            do {
                try {
                    Directory.Move(sourceDirName, destDirName);
                    success = true;
                } catch (Exception e) {
                    Log.Critical?.Error(
                        $"Exception below happened while moving directory from {sourceDirName} to {destDirName}");
                    Debug.LogException(e);
                }

                if (!success) {
                    counter++;
                    if (counter < maxAttempts) {
                        await UniTask.Delay(400, true);
                    }
                }
            } while (!success && counter < maxAttempts);

            return success;
        }
        
        static async UniTask<bool> TryDeleteDirectory(string dirName, int maxAttempts = 5) {
            bool success = false;
            int counter = 0;
            do {
                try {
                    Directory.Delete(dirName, true);
                    success = true;
                } catch (Exception e) {
                    Log.Critical?.Error($"Exception below happened while deleting directory {dirName}");
                    Debug.LogException(e);
                }

                if (!success) {
                    counter++;
                    if (counter < maxAttempts) {
                        await UniTask.Delay(400, true);
                    }
                }
            } while (!success && counter < maxAttempts);

            return success;
        }
    }
}