using System;
using System.Collections.Generic;
using System.Text;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sirenix.Utilities;
using CloudService = Awaken.TG.Main.Saving.Cloud.Services.CloudService;

namespace Awaken.TG.Main.Memories.FilePrefs {
    public static class FileBasedPrefs {
        const Formatting Formatting = Newtonsoft.Json.Formatting.Indented;
        const string SaveFileName = "PlayerPrefs";
        static FileBasedPrefsSaveFileModel s_latestData;
        static FileBasedPrefsSaveFileModel s_latestDataSynchronized;
        static int s_lastSaveFileCheckFrame;
        
        public static void EDITOR_RuntimeReset() {
            s_latestData = null;
            s_latestDataSynchronized = null;
            s_lastSaveFileCheckFrame = 0;
        }
        
        // === Get, Set and Util
        public static void SetString(string key, string value, bool synchronized) {
            AddDataToSaveFile(key, value, synchronized);
        }

        public static string GetString(string key, string defaultValue) {
            string value = (string) GetDataFromSaveFile(key, defaultValue, true);
            if (value == defaultValue) {
                value = (string) GetDataFromSaveFile(key, defaultValue, false);
            }
            return value;
        }

        /// <summary>
        /// Used in special case, during CloudService initialization, when getter is already valid but synchronized values are not loaded yet.
        /// </summary>
        public static string GetStringUnsynchronizedDuringInit(string key, string defaultValue) {
            return (string) GetDataFromSaveFile(key, defaultValue, false);
        }

        public static void SetInt(string key, int value, bool synchronized) {
            AddDataToSaveFile(key, value, synchronized);
        }

        public static int GetInt(string key, int defaultValue) {
            int value = (int) GetDataFromSaveFile(key, defaultValue, true);
            if (value == defaultValue) {
                value = (int) GetDataFromSaveFile(key, defaultValue, false);
            }
            return value;
        }

        public static void SetFloat(string key, float value, bool synchronized) {
            AddDataToSaveFile(key, value, synchronized);
        }

        public static float GetFloat(string key, float defaultValue) {
            float value = (float) GetDataFromSaveFile(key, defaultValue, true);
            if (value == defaultValue) {
                value = (float) GetDataFromSaveFile(key, defaultValue, false);
            }
            return value;
        }

        public static void SetBool(string key, bool value, bool synchronized) {
            AddDataToSaveFile(key, value, synchronized);
        }

        public static bool GetBool(string key, bool defaultValue) {
            bool value = (bool) GetDataFromSaveFile(key, defaultValue, true);
            if (value == defaultValue) {
                value = (bool) GetDataFromSaveFile(key, defaultValue, false);
            }
            return value;
        }

        public static object Get(string key) {
            return GetSaveFile(false).GetObjectValueFromKey(key) ??  GetSaveFile(true).GetObjectValueFromKey(key);
        }

        public static bool HasKey(string key) {
            return GetSaveFile(true).HasKey(key) || GetSaveFile(false).HasKey(key);
        }

        public static void DeleteKey(string key) {
            GetSaveFile(false).DeleteKey(key);
            GetSaveFile(true).DeleteKey(key);
            SaveSaveFile();
        }

        public static void DeleteAll(bool synchronized) {
            CloudService.Get.DeleteGlobalFile(GetSaveFilePath(synchronized), SaveFileName, synchronized);
            if (synchronized) {
                s_latestDataSynchronized = new FileBasedPrefsSaveFileModel();
            } else {
                s_latestData = new FileBasedPrefsSaveFileModel();
            }
        }

        [UnityEngine.Scripting.Preserve]
        public static void OverwriteLocalSaveFile(FileBasedPrefsSaveFileModel data, bool synchronized) {
            WriteToSaveFile(data, synchronized);
            if (synchronized) {
                s_latestDataSynchronized = null;
            } else {
                s_latestData = null;
            }
        }

        // === Read data

        static FileBasedPrefsSaveFileModel GetSaveFile(bool synchronized) {
            if ((!synchronized && s_latestData == null) || (synchronized && s_latestDataSynchronized == null)) {
                var data = LoadFromFile(synchronized);
                if (synchronized) {
                    s_latestDataSynchronized = data;
                } else {
                    s_latestData = data;
                }
            }

            return synchronized ? s_latestDataSynchronized : s_latestData;
        }

        static string GetSaveFilePath(bool synchronized) {
            return synchronized ? CloudService.SavedGamesDirectory : CloudService.UnsynchronizedSavedGamesDirectory;
        }

        static object GetDataFromSaveFile(string key, object defaultValue, bool synchronized) {
            return GetSaveFile(synchronized).GetValueFromKey(key, defaultValue);
        }
        
        public static IEnumerable<string> Keys {
            get {
                foreach (var key in GetSaveFile(false).Keys) {
                    yield return key;
                }
                foreach (var key in GetSaveFile(true).Keys) {
                    yield return key;
                }
            }
        }

        // === Writing Data

        static void AddDataToSaveFile(string key, object value, bool synchronized) {
            GetSaveFile(synchronized).UpdateOrAddData(key, value);
        }

        public static void SaveAll() {
            SaveSaveFile();
        }

        static void SaveSaveFile() {
            // We need to write synchronized first, because Unsynchronized stores Steam Sync data about synchronized file
            WriteToSaveFile(GetSaveFile(true), true);
            WriteToSaveFile(GetSaveFile(false), false);
        }
        
        // === Helpers
        static void WriteToSaveFile(FileBasedPrefsSaveFileModel data, bool synchronized) {
            JObject strings = new JObject();
            JObject ints = new JObject();
            JObject floats = new JObject();
            JObject bools = new JObject();
            data.stringData.ForEach(s => strings[s.Key] = s.Value);
            data.intData.ForEach(i => ints[i.Key] = i.Value);
            data.floatData.ForEach(f => floats[f.Key] = f.Value);
            data.boolData.ForEach(b => bools[b.Key] = b.Value);
            JObject parentObject = new JObject();
            parentObject.Add("strings", strings);
            parentObject.Add("ints", ints);
            parentObject.Add("floats", floats);
            parentObject.Add("bools", bools);
            string json = parentObject.ToString(Formatting);
            var bytes = Encoding.ASCII.GetBytes(json);
            CloudService.Get.SaveGlobalFile(GetSaveFilePath(synchronized), SaveFileName, bytes, synchronized);
        }
        
        static FileBasedPrefsSaveFileModel LoadFromFile(bool synchronized) {
            try {
                if (!CloudService.Get.TryLoadSingleFile(GetSaveFilePath(synchronized), SaveFileName, out var jsonEncoded, synchronized)) {
                    return new FileBasedPrefsSaveFileModel();
                }
                string json = Encoding.ASCII.GetString(jsonEncoded);
                var dataModel = new FileBasedPrefsSaveFileModel();
                JObject parentObject = JObject.Parse(json);
                JObject strings = (JObject) parentObject["strings"];
                JObject ints = (JObject) parentObject["ints"];
                JObject floats = (JObject) parentObject["floats"];
                JObject bools = (JObject) parentObject["bools"];
                strings.Properties().ForEach(p => dataModel.stringData.Add(p.Name, (string) p.Value));
                ints.Properties().ForEach(p => dataModel.intData.Add(p.Name, (int) p.Value));
                floats.Properties().ForEach(p => dataModel.floatData.Add(p.Name, (float) p.Value));
                bools.Properties().ForEach(p => dataModel.boolData.Add(p.Name, (bool) p.Value));
                return dataModel;
            } catch (Exception e) {
                Log.Important?.Error("Failed to parse PlayerPrefs");
                Log.Important?.Info(e.ToString());
                return new FileBasedPrefsSaveFileModel();
            }
        }
    }
}