using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Awaken.TG.Utility;

namespace Awaken.TG.Editor.Utility {
    public class BenchmarkRunner {
        public uint Iterations = 1000;
        public uint Repeat = 5;
        public bool CollectBetweenCases = true;
        public bool CollectBetweenRepeat = true;
        public bool Warmup = true;
        public uint WarmupCycles = 3;
        public SortMode Sort = SortMode.Memory;
        
        List<Case> _casesList = new List<Case>();
        Case[] _cases;

        public BenchmarkRunner AddCase(Action action, string name = "") {
            _casesList.Add(new Case() {
                Action = action,
                Name = string.IsNullOrWhiteSpace(name) ? action.Method.Name : name,
                Memory = new long[Repeat],
                Time = new TimeSpan[Repeat],
            });;
            return this;
        }

        public BenchmarkRunner DoTest() {
            _cases = _casesList.ToArray();
            int casesCount = _cases.Length;
            if (Warmup) {
                for (int i = 0; i < WarmupCycles; i++) {
                    for (int c = 0; c < casesCount; c++) {
                        _cases[c].Action();
                    }
                }
            }
            GC.Collect();

            Stopwatch stopwatch = new Stopwatch();
            for (int c = 0; c < casesCount; c++) {
                var currentCase = _cases[c];
                if (CollectBetweenCases) {
                    GC.Collect();
                }
                for (int r = 0; r < Repeat; r++) {
                    stopwatch.Restart();
                    long startMemory = GC.GetTotalMemory(CollectBetweenRepeat);
                    for (int i = 0; i < Iterations; i++) {
                        currentCase.Action();
                    }
                    stopwatch.Stop();
                    long endMemory = GC.GetTotalMemory(false);
                    currentCase.Memory[r] = endMemory - startMemory;
                    currentCase.Time[r] = stopwatch.Elapsed;
                }
                
            }
            
            return this;
        }

        public void DumpResults(TextWriter writer) {
            IEnumerable<Case> cases = _casesList;
            if (Sort == SortMode.Memory) {
                cases = _casesList.OrderBy(c => c.Memory.Max());
            } else if (Sort == SortMode.Time) {
                cases = _casesList.OrderBy(c => c.Time.Max(t => t.Ticks));
            }
            
            foreach (var currentCase in cases) {
                writer.Write(" --- ");
                writer.Write(currentCase.Name);
                writer.Write(" --- ");
                writer.WriteLine();
                for (int index = 0; index < currentCase.Memory.Length; index++) {
                    long currentCaseMemory = currentCase.Memory[index];
                    TimeSpan currentCaseTime= currentCase.Time[index];
                    writer.Write(index);
                    writer.Write(". ");
                    writer.Write(HumanReadableTimeSpan(currentCaseTime));
                    writer.Write("  --  ");
                    writer.Write(M.HumanReadableBytes((ulong)currentCaseMemory));
                    writer.WriteLine();
                }
                writer.WriteLine(" ------ ");
                writer.WriteLine();
            }
        }

        static string HumanReadableTimeSpan(TimeSpan timeSpan) {
            if (timeSpan.Ticks == 0) return "0 ns"; 

            StringBuilder sb = new StringBuilder(); 
  
            Action<long, StringBuilder, int, bool> addActionToSB = 
                (val, displayunit, zeroplaces, skipZero) => 
                {if (val > 0 || !skipZero) 
                    sb.AppendFormat(
                        " {0:DZ}X".Replace("X", displayunit.ToString())
                            .Replace("Z",zeroplaces.ToString())        
                        ,val
                    ); 
                };
            
            addActionToSB((long)timeSpan.Seconds, new StringBuilder("s"), 1, true);
            addActionToSB((long)timeSpan.Milliseconds, new StringBuilder("ms"), 3, false);
            addActionToSB((long)Convert.ToUInt64((int)((timeSpan.TotalMilliseconds - (int)timeSpan.TotalMilliseconds) * 1000)), new StringBuilder("µs"), 3, false);
            addActionToSB((long)Convert.ToUInt64((((decimal)(timeSpan.Ticks * 100) % 1000) )), new StringBuilder("ns"), 3, false);

            return sb.ToString().TrimStart();
        }

        class Case {
            public Action Action;
            public string Name;
            public long[] Memory;
            public TimeSpan[] Time;
        }

        public enum SortMode {
            Memory, Time, Order
        }
    }
}