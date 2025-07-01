using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Awaken.Utility {
    public class StackTraceUtils {
        
        static readonly Regex RgxFull = new(@"(.*)(\(.*\))(?: \(at )((.*):(\d*))(?:\))", RegexOptions.Compiled);
        // Group[1] - method
        // Group[2] - args
        // Group[3] - path:line
        // Group[4] - path
        // Group[5] - line
        
        static readonly Regex RgxFileReference = new(@"(?:\(at )((.*):(\d*))(?:\))", RegexOptions.Compiled); 
        // Group[1] = path:line
        // Group[2] = path
        // Group[3] = line
        
        
        public static string HrefStackTrace(string stackTrace) {
            var matches = RgxFileReference.Matches(stackTrace);
            for (int i = matches.Count - 1; i >= 0; i--) {
                var all = matches[i].Groups[1];
                var path = matches[i].Groups[2];
                var line = matches[i].Groups[3];
                if (!path.Value.StartsWith("Assets") && !path.Value.StartsWith("./Library")) continue;
                stackTrace = stackTrace.Insert(all.Index + all.Length, "</link></color>");
                stackTrace = stackTrace.Insert(all.Index, $"<color=#40a0ff><link=\"href='{path}' line='{line}'\">");
            }
            return stackTrace;
        }

        public static IEnumerable<FilePtr> StackTraceToFilePointers(string stackTrace) {
            var matches = RgxFull.Matches(stackTrace);
            for (int i = 0; i < matches.Count; i++) {
                var method = matches[i].Groups[1].ToString();
                var args = matches[i].Groups[2].ToString();
                var path = matches[i].Groups[4].ToString();
                var line = int.Parse(matches[i].Groups[5].ToString());
                yield return new FilePtr(method, path, int.Parse(line.ToString()), args);
            }
        }
    }

    [Serializable]
    public struct FilePtr {
        [SerializeField] string name;
        [SerializeField] string tooltip;
        [SerializeField] string path;
        [SerializeField] int line;

        public string Name => name;
        public string Tooltip => tooltip;
        
        public FilePtr(string name, string path, int line, string tooltip = "") {
            this.name = name;
            this.tooltip = tooltip;
            this.path = path;
            this.line = line;
        }
        
        [Conditional("UNITY_EDITOR")]
        public void GoTo() {
#if UNITY_EDITOR
            var file = UnityEditor.AssetDatabase.LoadMainAssetAtPath(path);
            UnityEditor.AssetDatabase.OpenAsset(file, line);
#endif
        }
    }
}