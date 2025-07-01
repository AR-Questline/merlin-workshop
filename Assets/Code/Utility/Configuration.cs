using System.Collections.Generic;
using System.IO;
using Awaken.Utility.Debugging;
using Newtonsoft.Json.Linq;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

namespace Awaken.Utility {
    [Il2CppEagerStaticClassConstruction]
    public static class Configuration {
        public static readonly string ConfigFileName = "config.ini";

        static string ConfigFilePrimaryPath => Path.Combine(Application.persistentDataPath, ConfigFileName);
        static string ConfigFileSecondaryPath => Path.Combine(Application.streamingAssetsPath, ConfigFileName);

        static readonly Dictionary<string, string> ConfigData = new Dictionary<string, string>();

#if UNITY_EDITOR
        [UnityEditor.MenuItem("TG/Configuration/ReInitialize Data", priority = 1000)]
#endif
        public static void InitializeData() {
            ConfigData.Clear();

            JObject primaryJson = null;
            JObject secondaryJson = null;

            try {
                if (File.Exists(ConfigFilePrimaryPath)) {
                    Log.Debug?.Info($"Loading config file from {ConfigFilePrimaryPath}");
                    primaryJson = JObject.Parse(File.ReadAllText(ConfigFilePrimaryPath));
                }
                if (File.Exists(ConfigFileSecondaryPath)) {
                    Log.Debug?.Info($"Loading config file from {ConfigFileSecondaryPath}");
                    secondaryJson = JObject.Parse(File.ReadAllText(ConfigFileSecondaryPath));
                }

                string finalJson;
                if ((primaryJson, secondaryJson) is (not null, not null)) {
                    secondaryJson.Merge(primaryJson, new JsonMergeSettings {
                        MergeArrayHandling = MergeArrayHandling.Union
                    });
                    finalJson = secondaryJson.ToString();
                } else if ((primaryJson, secondaryJson) is (null, not null)) {
                    finalJson = secondaryJson.ToString();
                } else if ((primaryJson, secondaryJson) is (not null, null)) {
                    finalJson = primaryJson.ToString();
                } else {
                    Log.Minor?.Error($"Cannot load configuration file from {ConfigFilePrimaryPath} nor {ConfigFileSecondaryPath}");
                    return;
                }

                Log.Debug?.Info($"Configuration from json:\n{finalJson}");

                var serializedConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(finalJson);
                if (serializedConfig == null) {
                    Log.Critical?.Error($"Cannot deserialize configuration file:\n{finalJson}");
                } else {
                    foreach (var (key, value) in serializedConfig) {
                        ConfigData.SetValueLowerKey(key, value);
                    }
                }
            } catch (System.Exception e) {
                Log.Critical?.Error("Failed to load configuration file");
                Debug.LogException(e);
                ConfigData.Clear();
            }
        }

        public static bool GetBool(string key, bool defaultValue = false) {
            if (ConfigData.TryGetValueLowerKey(key, out var value)) {
                return bool.TryParse(value, out var result) ? result : defaultValue;
            }
            return defaultValue;
        }

        public static int GetInt(string key, int defaultValue = 0) {
            if (ConfigData.TryGetValueLowerKey(key, out var value)) {
                return int.TryParse(value, out var result) ? result : defaultValue;
            }
            return defaultValue;
        }

        public static float GetFloat(string key, float defaultValue = 0) {
            if (ConfigData.TryGetValueLowerKey(key, out var value)) {
                return float.TryParse(value, out var result) ? result : defaultValue;
            }
            return defaultValue;
        }

        [UnityEngine.Scripting.Preserve]
        public static string GetString(string key, string defaultValue = "") {
            return ConfigData.GetValueOrDefaultLowerKey(key, defaultValue);
        }

        public static T Get<T>(string key, T defaultValue = default) {
            if (ConfigData.TryGetValueLowerKey(key, out var value)) {
                try {
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(value);
                } catch (System.Exception e) {
                    Log.Critical?.Error($"Failed to deserialize configuration value for key {key}");
                    Debug.LogException(e);
                }
            }
            return defaultValue;
        }
    }
}
