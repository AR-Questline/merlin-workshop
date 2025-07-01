using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Awaken.Tests.Performance.Preprocessing;
using Awaken.Tests.Performance.Profilers;
using Awaken.Tests.Performance.TestCases;
using Awaken.Utility.Debugging;
using Awaken.Utility.Slack;
using Cysharp.Threading.Tasks;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.Tests.Performance {
    public class PerformanceTestRunner : MonoBehaviour {
        static string OutputDirectory => @$"{Application.persistentDataPath}\benchmarks\performance";
        
        bool _running;
        bool _uploadingFile;

        string _sessionName;
        IPerformanceTestCase _test;
        IPerformanceProfiler[] _profilers;
        IPerformanceMatrix[] _matrices;
        SlackMessenger _slackMessenger;

        float _startTime;
        List<float> _times = new();

        public async UniTask Run(IPerformancePreprocessorVariant[] preprocessors, IPerformanceTestCase test, IPerformanceMatrix[] matrices, SlackMessenger slackMessenger) {
            if (_running) {
                Log.Critical?.Error("Cannot run test while another test is running");
                return;
            }
            
            var sb = new StringBuilder();
            sb.Append(DateTime.Now.ToString("yyMMdd_HHmmss"));
            sb.Append('_');
            sb.Append(test.Name);
            foreach (var preprocessor in preprocessors) {
                sb.Append('_');
                sb.Append(preprocessor.Name);
            }
            _sessionName = sb.ToString();

            _test = test;
            _profilers = matrices.Select(m => m.Profiler).Distinct().ToArray();
            _matrices = matrices;
            
            foreach (var preprocessor in preprocessors) {
                preprocessor.Process();
            }
            await UniTask.Delay(5000);
            foreach(var profiler in _profilers) {
                profiler.Start();
            }
            _test.Run();
            _running = true;
            _startTime = Time.time;
            _slackMessenger = slackMessenger;

            await UniTask.WaitWhile(() => _running || _uploadingFile);
        }

        void Update() {
            if (!_running) {
                return;
            }
            
            _test.Update(out bool ended, out bool capture);
            if (ended) {
                Finish();
                return;
            }
            if (capture) {
                _times.Add(Time.time - _startTime);
                foreach (var profiler in _profilers) {
                    profiler.Update();
                }
            }
        }

        void Finish() {
            if (_times.Count > 0) {
                EnsureDirectoryExists();
                WriteResults();
                WriteSmoothResults(0.1f, 0.15f);
                WriteResultsStats();
            } else {
                Log.Important?.Error("Cannot run test while another test is running");
            }

            _times.Clear();
            foreach (var profiler in _profilers) {
                profiler.End();
            }
            
            _running = false;
            _sessionName = null;
            _test = null;
            _profilers = null;
            _matrices = null;
        }

        void EnsureDirectoryExists() {
            string directory = OutputDirectory;
            if (!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }
        }
        
        void WriteResults() {
            using var stream = new FileStream(@$"{OutputDirectory}\{_sessionName}.tsv", FileMode.Create);
            using var writer = new StreamWriter(stream);

            writer.Write("Time");
            foreach (var matrix in _matrices) {
                writer.Write('\t');
                writer.Write(matrix.Name);
            }
            writer.WriteLine();
            
            for (int i=0; i<_times.Count; i++) {
                writer.Write(_times[i]);
                foreach (var matrix in _matrices) {
                    writer.Write('\t');
                    writer.Write(matrix.RawDouble(i).ToString(CultureInfo.CurrentCulture));
                }
                writer.WriteLine();
            }
        }

        void WriteSmoothResults(float interval, float smoothing) {
            using var stream = new FileStream(@$"{OutputDirectory}\{_sessionName}_smooth.tsv", FileMode.Create);
            using var writer = new StreamWriter(stream);

            writer.Write("Time");
            foreach (var matrix in _matrices) {
                writer.Write('\t');
                writer.Write(matrix.Name);
            }
            writer.WriteLine();

            float time = 0;
            while (time < _times[^1]) {
                writer.Write(time);
                
                int closestIndex = _times.BinarySearch(time);
                // binary search returns the index of the element if it exists, or the bitwise negation of the index of the next element if it doesn't
                if (closestIndex < 0) {
                    closestIndex = ~closestIndex;
                    if (closestIndex > 0 && time - _times[closestIndex - 1] < _times[closestIndex] - time) {
                        closestIndex--;
                    }
                }

                int start = closestIndex - 1;
                while (start >= 0 && time - _times[start] < smoothing) {
                    start--;
                }
                start++;

                int end = closestIndex + 1;
                while (end < _times.Count && _times[end] - time < smoothing) {
                    end++;
                }
                
                int count = end - start;

                foreach (var matrix in _matrices) {
                    double sum = 0;
                    for (int i = start; i < end; i++) {
                        sum += matrix.RawDouble(i);
                    }

                    double average = sum / count;
                    
                    writer.Write('\t');
                    writer.Write(average.ToString(CultureInfo.CurrentCulture));
                }
                
                writer.WriteLine();
                time += interval;
            }
        }

        void WriteResultsStats() {
            string sourceFile = @$"{OutputDirectory}\{_sessionName}.tsv";
            using (var stream = new FileStream(sourceFile, FileMode.Create)) {
                using var writer = new StreamWriter(stream);

                writer.WriteLine(
                    "matrix\t10-trimmed average\taverage\tmin\tmax\t10-th percentile\t25-th percentile\t50 percentile\t75-th percentile\t90-th percentile\t95-th percentile\t99-th percentile\t1-trimmed average\t5-trimmed average");

                foreach (var matrix in _matrices) {
                    var values = new double[_times.Count];
                    for (int i = 0; i < _times.Count; i++) {
                        values[i] = matrix.RawDouble(i);
                    }

                    Array.Sort(values);

                    writer.Write(matrix.Name);

                    WriteTrimmedAverage(0.1f);
                    Write(values.Average());
                    Write(values[0]);
                    Write(values[^1]);
                    WritePercentile(0.1f);
                    WritePercentile(0.25f);
                    WritePercentile(0.5f);
                    WritePercentile(0.75f);
                    WritePercentile(0.9f);
                    WritePercentile(0.95f);
                    WritePercentile(0.99f);
                    WriteTrimmedAverage(0.01f);
                    WriteTrimmedAverage(0.05f);
                    writer.WriteLine();

                    void Write(double value) {
                        writer.Write('\t');
                        writer.Write(value.ToString("F2", CultureInfo.CurrentCulture));
                    }

                    void WritePercentile(float percentile) {
                        Write(values[Mathf.FloorToInt(percentile * values.Length)]);
                    }

                    void WriteTrimmedAverage(float trim) {
                        int skip = Mathf.FloorToInt(trim * values.Length);
                        Write(values.Skip(skip).SkipLast(skip).Average());
                    }
                }
            }
            
            UploadSlackFile(sourceFile);
        }

        async void UploadSlackFile(string sourceFile) {
            if (_slackMessenger != null) {
                _uploadingFile = true;
                await _slackMessenger.UploadFile(sourceFile);
                _uploadingFile = false;
            }
        }
    }
}