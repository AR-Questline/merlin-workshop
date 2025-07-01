using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Awaken.TG.Code.Editor.Tests {
    public class TestLog {
        public string condition { get; private set; }
        public string stacktrace { get; private set; }
        public LogType type { get; private set; }

        public TestLog(string condition = "", string stacktrace = "", LogType type = LogType.Log) {
            this.condition = condition;
            this.stacktrace = stacktrace;
            this.type = type;
        }

        public override string ToString() {
            return $"[{type.ToString()}] {condition}\n{stacktrace}";
        }
    }

    public class TestLogPattern {
        Regex conditionRegex;
        Regex stacktraceRegex;
        LogType[] types;

        public TestLogPattern(Regex conditionRegex = null, Regex stacktraceRegex = null, LogType[] types = null) {
            this.conditionRegex = conditionRegex;
            this.stacktraceRegex = stacktraceRegex;
            this.types = types;
        }

        public bool Match(TestLog log) {
            return Match(log.condition, log.stacktrace, log.type);
        }
        public bool Match(string condition, string stacktrace, LogType type) {
            if (conditionRegex != null && !conditionRegex.IsMatch(condition)) {
                return false;
            }
            if (stacktraceRegex != null && !stacktraceRegex.IsMatch(stacktrace)) {
                return false;
            }
            if (types != null && !types.Contains(type)) {
                return false;
            }
            return true;
        }
    }
}
