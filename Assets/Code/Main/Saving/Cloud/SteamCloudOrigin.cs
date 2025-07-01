using Awaken.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Memories.FilePrefs;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility.Threads;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Awaken.TG.Main.Saving.Cloud {
    /// <summary>
    /// Represents state of files in the moment of sending them to steam cloud on this machine,
    /// so we can recognize if user changed the local files manually.
    /// </summary>
    public partial class SteamCloudOrigin {
        public ushort TypeForSerialization => SavedTypes.SteamCloudOrigin;

        const string PrefsKey = "SteamCloudOrigin";
        
        static SteamCloudOrigin s_origin;
        public static SteamCloudOrigin Get => s_origin ??= Retrieve();
        
        [Saved] ConcurrentDictionary<string, SteamOriginFile> _fileHistory = new();

        static SteamCloudOrigin Retrieve() {
            string originJson = FileBasedPrefs.GetStringUnsynchronizedDuringInit(PrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(originJson)) {
                return new SteamCloudOrigin();
            }
            var jObject = JObject.Parse(originJson);
            SteamCloudOrigin cloudOrigin = jObject.ToObject<SteamCloudOrigin>(LoadSave.Get.serializer);
            return cloudOrigin;
        }
        
        public static void EDITOR_RuntimeReset() {
            s_origin = null;
        }

        public void NotifyWrite(string fileName, DateTime windowsTimeStamp, DateTime steamTimeStamp) {
            if (!_fileHistory.TryGetValue(fileName, out SteamOriginFile history)) {
                history = new SteamOriginFile {
                    steamPath = fileName,
                };
                history = _fileHistory.GetOrAdd(fileName, history);
            }

            history.windowsTimeStamp = windowsTimeStamp;
            history.steamTimeStamp = steamTimeStamp;
        }

        public void NotifyDelete(string fileName) {
            _fileHistory.TryRemove(fileName, out _);
        }

        public IEnumerable<SteamOriginFile> GetFiles() => _fileHistory.Values;

        public void Serialize() {
            ThreadSafeUtils.AssertMainThread();
            string json = JObject.FromObject(this, LoadSave.Get.serializer).ToString(Formatting.None, LoadSave.Converters);
            PrefMemory.Set(PrefsKey, json, false);
        }
    }
    
    public partial class SteamOriginFile {
        public ushort TypeForSerialization => SavedTypes.SteamOriginFile;

        [Saved] public string steamPath;
        [Saved] public DateTime windowsTimeStamp;
        [Saved] public DateTime steamTimeStamp;
        /// <summary>
        /// Forgotten means - not synced on cloud but exists locally.
        /// </summary>
        [Saved] [UnityEngine.Scripting.Preserve] public bool isForgotten;
    }
}