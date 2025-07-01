using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Awaken.TG.Editor.SimpleTools;
using UnityEditor;
using UnityEngine;
using ThreadPriority = System.Threading.ThreadPriority;

namespace Awaken.TG.Editor.Debugging.GUIDSearching {
    public static class GUIDSearchUtils {
        static readonly string[] IgnoredAssetFormats = {
            ".jpg", ".jpeg", ".png", ".mp3", ".wav", ".mp4", ".webm", ".tga", ".psd", ".cs", ".zip", ".gif",
            ".hdr", ".exr", ".bytes", ".shader", ".fbx", ".obj", ".tif", ".dll", ".pdf", ".unitypackage", ".bmp", ".dylib",
            ".a", ".mesh", ".pp", ".c", ".xml", ".ttf", ".compute", ".renderTexture", ".bank"
        };

        // == Path Validation
        
        public static string[] GetValidPaths(int threadNum = 8) {
            var files = AllFiles();
            int count = files.Length;

            int index = -1;
            
            var threads = new Thread[threadNum];
            var threadLists = new List<string>[threadNum];
            for (int i = 0; i < threadNum; i++) {
                threadLists[i] = new List<string>();
                threads[i] = new Thread(Process(threadLists[i]));
                threads[i].Priority = ThreadPriority.Highest;
                threads[i].Start();
            }
            var mainList = new List<string>();
            Process(mainList)();
            while (threads.Any(thread => thread.IsAlive)) {
                Thread.Sleep(100);
            }

            foreach (var list in threadLists) {
                mainList.AddRange(list);
            }

            return mainList.ToArray();

            ThreadStart Process(List<string> list) => () => {
                while (true) {
                    var myIndex = Interlocked.Increment(ref index);
                    if (myIndex >= count) {
                        break;
                    }
                    var path = files[myIndex];
                    if (IsValidPath(path)) {
                        list.Add(ToProjectPath(path));
                    }
                }
            };
        }
        
        static string[] AllFiles() {
            string assetsDir = Application.dataPath;
            return Directory.GetFiles(assetsDir, "*.*", SearchOption.AllDirectories);
        }
        
        public static bool IsValidPath(string path) {
            path = path.ToLower();
            // Scan metas because FBX meta files might contain references to other assets
            /*var pathLength = path.Length;
            // ends with meta
            if (path[pathLength - 5] == '.' && path[pathLength - 4] == 'm' && path[pathLength - 3] == 'e' && path[pathLength - 2] == 't' && path[pathLength - 1] == 'a') {
                path = path.Substring(0, pathLength - 5);
            }*/
            return !IgnoredAssetFormats.Any(format => EndsWith(path, format));
        }
        
        // faster than string.EndsWith
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool EndsWith(string input, string pattern) {
            var patternLength = pattern.Length;
            var inputLength = input.Length;
            bool ends = true;
            int i = 0;
            while (ends & i < patternLength) {
                ends = input[inputLength - patternLength + i] == pattern[i];
                i++;
            }
            return ends;
        }

        static string ToProjectPath(string path) {
            return path.Substring(path.IndexOf("Assets", StringComparison.Ordinal));
        }
        
        // == Parallelization
        
        public static bool TryComputeParallel<TIn, TOut>(ParallelComputeData<TIn, TOut> computeData, TIn[] data, out List<TOut> result) {
            result = new List<TOut>();
            
            int waveSize = data.Length / computeData.waveCount;
            int waveComplement = data.Length % waveSize;
            int waveStart = 0;

            List<Task<IEnumerable<TOut>>> tasks = new();
                
            for (int wave = 0; wave < computeData.waveCount; wave++) {
                CancellationTokenSource cancellationToken = new();
                tasks.Clear();
                if (computeData.DisplayCancellableBar(wave, 0)) {
                    return false;
                }

                int currentWaveSize = wave < waveComplement ? waveSize + 1 : waveSize;
                int threadSize = currentWaveSize / computeData.threadCount;
                int threadComplement = currentWaveSize % threadSize;
                int threadStart = waveStart;
                
                for (int thread = 0; thread < computeData.threadCount; thread++) {
                    int currentThreadSize = thread < threadComplement ? threadSize + 1 : threadSize;
                    var arraySegment = new ArraySegment<TIn>(data, threadStart, currentThreadSize);
                    var task = Task.Run(() => computeData.func(arraySegment), cancellationToken.Token);
                    tasks.Add(task);
                    threadStart += currentThreadSize;
                }

                int taskCompleted;
                do {
                    taskCompleted = tasks.Count(t => t.IsCompleted);
                    if (computeData.DisplayCancellableBar(wave, taskCompleted)) {
                        cancellationToken.Cancel();
                        return false;
                    }
                } while (taskCompleted < computeData.threadCount);

                foreach (var task in tasks) {
                    result.AddRange(task.Result);
                }
                
                waveStart += currentWaveSize;
            }
            EditorUtility.ClearProgressBar();
            return true;
        }
    }

    public class ParallelComputeData<TIn, TOut> {
        public ProgressBar progressBar;
        
        public int waveCount;
        public int threadCount;
        
        public Func<IEnumerable<TIn>, IEnumerable<TOut>> func;

        public bool DisplayCancellableBar(int wavesCompleted, int threadsCompleted) {
            float percent = wavesCompleted + (float) threadsCompleted / threadCount;
            percent /= waveCount;
            return progressBar.DisplayCancellable(percent);
        }
    }
}