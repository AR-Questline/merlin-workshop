using System;
using System.Linq;
using System.Text;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEditor.Build;

namespace Awaken.TG.Editor.Utility {
    public static class ScriptingDefinesUtils {
        static readonly char[] DefineSeparators = new char[3] {
            ';',
            ',',
            ' '
        };

        public static void AddScriptingDefine(string defineSymbol) {
            if (string.IsNullOrEmpty(defineSymbol)) {
                Log.Minor?.Error($"Trying to add empty define symbol");
                return;
            }

            GetDefinesStringAndBuildTarget(out var definesString, out var buildTarget);
            var defines = GetDefinesArray(definesString);
            if (defines.Contains(defineSymbol)) {
                return;
            }

            var sb = new StringBuilder((definesString?.Length ?? 0) + defineSymbol.Length + 1);
            if (string.IsNullOrEmpty(definesString) == false) {
                sb.Append(definesString);
                // Remove all trailing whitespaces
                while (char.IsWhiteSpace(sb[^1])) {
                    sb.Length--;
                }

                // If separator was not added in existing string - add separator
                if (DefineSeparators.Contains(sb[^1]) == false) {
                    sb.Append(DefineSeparators[0]);
                }
            }

            sb.Append(defineSymbol);
            var newDefines = sb.ToString();
            PlayerSettings.SetScriptingDefineSymbols(buildTarget, newDefines);
        }

        public static void RemoveScriptingDefine(string defineSymbol) {
            if (string.IsNullOrEmpty(defineSymbol)) {
                Log.Minor?.Error($"Trying to remove empty define symbol");
                return;
            }

            GetDefinesStringAndBuildTarget(out var definesString, out var buildTarget);
            var defines = GetDefinesArray(definesString);
            var indexOfDefineToRemove = defines.IndexOf(defineSymbol);
            if (indexOfDefineToRemove == -1) {
                return;
            }

            var sb = new StringBuilder(definesString.Length - defineSymbol.Length);
            for (int i = 0; i < defines.Length; i++) {
                if (i == indexOfDefineToRemove) {
                    continue;
                }

                sb.Append(defines[i]);
                sb.Append(DefineSeparators[0]);
            }

            sb.Length--;
            var newDefines = sb.ToString();
            PlayerSettings.SetScriptingDefineSymbols(buildTarget, newDefines);
        }

        public static void GetDefinesStringAndBuildTarget(out string definesString, out NamedBuildTarget buildTarget) {
            buildTarget = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
            definesString = PlayerSettings.GetScriptingDefineSymbols(buildTarget);
        }

        public static string[] GetDefinesArray(string definesString) {
            return string.IsNullOrEmpty(definesString) ? Array.Empty<string>() : definesString.Split(DefineSeparators, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}