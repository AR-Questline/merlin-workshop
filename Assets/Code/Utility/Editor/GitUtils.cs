using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.Utility.Editor {
    public static class GitUtils {
        const string NoBranchName = "none";
        public enum Branch { None, Master, Release, Other, }

        static string s_git;
        
        static string s_branchName;
        static string s_userName;
        static string s_commitHash;

        static string InternalGetBranchName() => GetOutput("rev-parse --abbrev-ref HEAD", NoBranchName);
        static string InternalGetUserName() => GetOutput("config user.name", "user");
        static string InternalGetCommitHash() => GetOutput("rev-parse HEAD", "hash");

        public static string GetBranchName() => s_branchName ??= InternalGetBranchName();
        public static string GetUserName() => s_userName ??= InternalGetUserName();
        public static string GetCommitHash() => s_commitHash ??= InternalGetCommitHash();
        
        public static void Validate() {
            if (s_branchName != null) { // refresh only if it has been needed in this session
                s_branchName = InternalGetBranchName();
            }
        }
        
        public static Branch GetCurrentBranch() {
            return GetBranchName() switch {
                NoBranchName => Branch.None,
                "master" or "main" => Branch.Master,
                "release" => Branch.Release,
                _ => Branch.Other
            };
        }
        
        // Used via reflection in TopDownDepthTextureBaker
        public static void RunGitCommand(string command) {
            GetOutput(command, null);
        }
        
        public static string GetOutput(string arguments, string fallback) {
            var logs = new StringBuilder();

            try {
                if (s_git == null) {
                    string forkGitInstanceDirectory = Environment.ExpandEnvironmentVariables(@"%appdata%\..\Local\Fork\gitInstance") ?? throw new ArgumentNullException("Environment.ExpandEnvironmentVariables(@\"%appdata%\\..\\Local\\Fork\\gitInstance\")");
                    s_git = Directory.EnumerateDirectories(forkGitInstanceDirectory).First() + @"\bin\git.exe";
                }
            } catch (Exception e) {
                Log.Important?.Error(e.Message);
                return fallback;
            }

            var output = TryGetOutput(s_git, arguments, logs);

            if (output != null) {
                return output;
            }

            Log.Important?.Error(logs.ToString());
            return fallback;
        }
        
        static string TryGetOutput(string filename, string arguments, StringBuilder logs) {
            var process = new Process {
                StartInfo = new ProcessStartInfo {
                    WorkingDirectory = Application.dataPath,
                    FileName = filename,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            
            try {
                process.Start();

                process.WaitForExit();
                string output = process.StandardOutput.ReadToEnd().Trim();
                string error = process.StandardError.ReadToEnd().Trim();
                if (string.IsNullOrEmpty(error) == false) {
                    Log.Debug?.Error(error);
                }
                process.Close();
                return output;
            } catch (Exception e) {
                logs.AppendLine();
                logs.AppendLine(e.Message);
                return null;
            }
        }
    }
}