#if !UNITY_GAMECORE && !UNITY_PS5
//#define FORCE_FILE_VERIFICATION

#if FORCE_FILE_VERIFICATION || !UNITY_EDITOR && !DEBUG && !MICROSOFT_GAME_CORE && !UNITY_GAMECORE && !UNITY_PS5
#define VERIFY
using Awaken.TG.Debugging.Logging;
#endif

using System;
using System.Collections.Generic;
using System.Threading;
using Awaken.TG.Main.Utility.Patchers;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using JetBrains.Annotations;
using UnityEngine;
using ThreadPriority = System.Threading.ThreadPriority;

namespace Awaken.TG.Main.UI.TitleScreen.FileVerification {
    public class ApplicationFileIntegrityChecker : FileChecksum.IVerificationObserver {
        const string LastVerifiedVersion = "last.verified.version";
        
        [CanBeNull] public static ApplicationFileIntegrityChecker Instance { get; [UnityEngine.Scripting.Preserve] private set; }
#if VERIFY
        [UnityEngine.Scripting.Preserve] static bool ShouldVerify => PlayerPrefs.GetString(LastVerifiedVersion, string.Empty) != GitDebugData.BuildCommitHash;
#else
        [UnityEngine.Scripting.Preserve] static bool ShouldVerify => false;
#endif

        Thread _thread;

        float _lastProgressChecked;
        float _progress;
        List<string> _failedFiles = new();
        List<string> _missingFiles = new();
        
        public bool Verified { get; private set; }
        public bool Success => ErrorsInfo == FileChecksumErrors.None;
        public FileChecksumErrors ErrorsInfo { get; private set; }

        ApplicationFileIntegrityChecker() {
            _thread = new Thread(Verify) {
                Name = "File Integrity Verification",
                Priority = ThreadPriority.Highest,
            };
            _thread.Start();
        }

#if VERIFY
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void StartVerification() {
            if (ShouldVerify) {
                Instance = new ApplicationFileIntegrityChecker();
                InitLoggerProxy.QueueLog("[Verifier] Started verification");
            } else {
                Instance = null;
                InitLoggerProxy.QueueLog("[Verifier] Version already verified: " + PlayerPrefs.GetString(LastVerifiedVersion, string.Empty));
            }
        }
#endif

        public void ForceEndVerification() {
            if (_thread is { IsAlive: true }) {
                _thread.Abort();
            }
        }

        public bool WasProgressChanged(out float progress) {
            progress = _progress;
            var changed = _lastProgressChecked != progress;
            _lastProgressChecked = progress;
            return changed;
        }

        public void LogFailedFiles() {
            foreach (var failedFile in _failedFiles) {
                Log.Critical?.Error($"[Verifier] Corrupted file: {failedFile}");
            }
            foreach (var missingFile in _missingFiles) {
                Log.Critical?.Error($"[Verifier] Missing file: {missingFile}");
            }
            _failedFiles.Clear();
            _missingFiles.Clear();
        }

        public void MarkVerified() {
            PlayerPrefs.SetString(LastVerifiedVersion, GitDebugData.BuildCommitHash);
        }
        
        void Verify() {
            ErrorsInfo = FileChecksum.Verify(Environment.CurrentDirectory, this);
            Verified = true;
            _thread = null;
        }

        void FileChecksum.IVerificationObserver.OnProgressChanged(float progress) {
            _progress = progress;
        }

        void FileChecksum.IVerificationObserver.OnFailedFile(string file) {
            _failedFiles.Add(file);
        }
        
        void FileChecksum.IVerificationObserver.OnMissingFile(string file) {
            _missingFiles.Add(file);
        }
    }
}
#endif