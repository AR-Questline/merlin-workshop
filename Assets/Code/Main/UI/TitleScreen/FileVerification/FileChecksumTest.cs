#if UNITY_EDITOR

using System.Diagnostics;
using Awaken.Utility.Debugging;
using Unity.VisualScripting;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Awaken.TG.Main.UI.TitleScreen.FileVerification {
    public class FileChecksumTest {
        const string BuildDirectory = @"C:\Program Files (x86)\Steam\steamapps\common\Tainted Grail FoA\";
        
        [MenuItem("TG/Build/Checksum Create")]
        static void TestCreate() {
            var stopwatch = Stopwatch.StartNew();
            FileChecksum.Create(BuildDirectory);
            stopwatch.Stop();
            Log.Important?.Info($"Checksums created in {stopwatch.Elapsed.ToString()}");
        }
        
        [MenuItem("TG/Build/Checksum Verify")]
        static void TestVerify() {
            var stopwatch = Stopwatch.StartNew();
            var result = FileChecksum.Verify(BuildDirectory);
            stopwatch.Stop();
            Log.Important?.Info($"Checksums verified in {stopwatch.Elapsed.ToString()}");
#pragma warning disable CS0618 // Type or member is obsolete
            bool success = result == FileChecksumErrors.None;
            Log.When(success ? LogType.All : LogType.Critical)?.Info($"Verification {(success ? "Succeeded" : "Failed")}");
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
#endif