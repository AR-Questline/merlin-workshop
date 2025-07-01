using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Unity.Mathematics;
using Debug = UnityEngine.Debug;

namespace Awaken.TG.Main.UI.TitleScreen.FileVerification {
    public static class FileChecksum {
        const string Filename = "checksums.bin";
        
        /// <summary> Finds checksums file in directory and verifies all files from it </summary>
        public static FileChecksumErrors Verify(string directory, IVerificationObserver observer = null) {
            var verifyResult = FileChecksumErrors.None;
            
            try {
                ValidateDirectory(ref directory);
                var checksums = DeserializeFromFile<Checksums>(directory + Filename).checksums;
                int processed = 0;
                
                foreach (var result in FileHasher.Hash(checksums.Select(checksum => directory + checksum.relativeFile))) {
                    observer?.OnProgressChanged((float)++processed / checksums.Length);

                    if (result.fileMissing) {
                        observer?.OnMissingFile(result.filename);
                        verifyResult |= FileChecksumErrors.FilesMissing;
                        
                    } else if (!checksums[result.index].hash.Equals(result.hash)) {
                        observer?.OnFailedFile(result.filename);
                        verifyResult |= FileChecksumErrors.HashMismatch;
                    }
                }
                observer?.OnProgressChanged(1);
                return verifyResult;
            } catch (Exception e) {
                InitLoggerProxy.QueueLog("Exception happened when verifying files integrity");
                InitLoggerProxy.QueueLog(e.Message + "\n StackTrace: " + e.StackTrace);
                Debug.LogException(e);
                return FileChecksumErrors.Exception;
            }
        }
        
        /// <summary> Create checksums for all files in directory and subdirectories </summary>
        public static void Create(string directory) {
            ValidateDirectory(ref directory);
            var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
            var results = FileHasher.Hash(files.Where(ShouldCheckFile));
            var checksums = new Checksums {
                checksums = results.Select(result => new Checksum {
                    relativeFile = result.filename[directory.Length..],
                    hash = result.hash
                }).ToArray()
            };
            SerializeToFile(directory + Filename, checksums);
        }

        static void ValidateDirectory(ref string directory) {
            if (!directory.EndsWith('\\') && !directory.EndsWith('/')) {
                directory += '\\';
            }
        }
        
        static bool ShouldCheckFile(string file) {
            if (file.EndsWith(Filename)) {
                return false;
            }
            if (file.EndsWith("UnityCrashHandler64.exe")) {
                return false;
            }
            if (file.EndsWith("Fall of Avalon.exe")) {
                return false;
            }
            if (file.Contains("DoNotShip") || file.Contains("DontShip")) {
                return false;
            }
            if (file.Contains("HLODs/")) {
                return false;
            }
            if (file.EndsWith(".pdb")) {
                return false;
            }
            if (file.EndsWith(".ini")) {
                return false;
            }
            if (file.EndsWith("nvngx_dlss.dll")) {
                return false;
            }
            return true;
        }

        static void SerializeToFile(string filePath, object data) {
            using var fs = new FileStream(filePath, FileMode.Create);
            var formatter = new BinaryFormatter();
            formatter.Serialize(fs, data);
        }

        static T DeserializeFromFile<T>(string filePath) {
            using var fs = new FileStream(filePath, FileMode.Open);
            var formatter = new BinaryFormatter();
            return (T)formatter.Deserialize(fs);
        }

        [Serializable]
        struct Checksum {
            public string relativeFile;
            public uint4 hash;
        }
        
        [Serializable]
        class Checksums {
            public Checksum[] checksums;
        }

        public interface IVerificationObserver {
            void OnProgressChanged(float progress);
            void OnFailedFile(string file);
            void OnMissingFile(string file);
        }
    }
    
    [Flags]
    public enum FileChecksumErrors : byte {
        None = 0,
        Exception = 1 << 0,
        FilesMissing = 1 << 1,
        HashMismatch = 1 << 2
    }
}