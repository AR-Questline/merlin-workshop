using System;
using System.IO;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.AssetManager {
    public static class AssetVerificationJob {
        const string HeavyVerifyLocation = "Assets/Plugins/Sirenix/Odin Validator/Editor/Profiles/HeavyVerify.asset";
        const string ResultFilePath = "Assets/Logs/AssetVerification.json";

        [MenuItem("TG/Build/Run Verification")]
        public static void Run() {
            // var profile = AssetDatabase.LoadAssetAtPath<ValidationProfile>(HeavyVerifyLocation);
            //
            // using (var session = new ValidationSession(profile)) {
            //     session.PopulateQueue(true, false, queueGlobalValidators: false);
            //     int workQueueCount = session.WorkQueue.Count;
            //     while (workQueueCount > 0) {
            //         try {
            //             session.ValidateQueuedUpWorkNow();
            //         } catch (Exception e) {
            //             Debug.LogException(e);
            //         }
            //
            //         int newCount = session.WorkQueue.Count;
            //         if (newCount == workQueueCount) {
            //             Log.Important?.Error("Validation session is stuck!");
            //             break;
            //         }
            //         workQueueCount = newCount;
            //     }
            //
            //     string directory = ResultFilePath[..ResultFilePath.LastIndexOf('/')];
            //     Directory.CreateDirectory(directory);
            //     
            //     using (StreamWriter dumpFile = new StreamWriter(ResultFilePath)) {
            //         dumpFile.Write(session.ToJson(true));
            //         dumpFile.Flush();
            //         dumpFile.Close();
            //     }
            // }
        }
    }
}