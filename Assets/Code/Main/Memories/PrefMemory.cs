using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Memories.FilePrefs;
using Awaken.TG.Main.Templates;
using UnityEngine;

namespace Awaken.TG.Main.Memories {
    public static class PrefMemory {
        public static IEnumerable<string> Keys => FileBasedPrefs.Keys;

        public static void Save() {
            FileBasedPrefs.SaveAll();
            PlayerPrefs.Save();
        }
        
        public static bool HasKey(string key) {
            return FileBasedPrefs.HasKey(key);
        }

        public static void DeleteKey(string key) {
            FileBasedPrefs.DeleteKey(key);
        }

        // === Getters
        public static bool GetBool(string key, bool fallback = false) {
            return FileBasedPrefs.GetBool(key, fallback);
        }

        public static int GetInt(string key, int fallback = 0) {
            return FileBasedPrefs.GetInt(key, fallback);
        }

        public static float GetFloat(string key, float fallback = 0f) {
            return FileBasedPrefs.GetFloat(key, fallback);
        }

        public static string GetString(string key, string fallback = "") {
            return FileBasedPrefs.GetString(key, fallback);
        }
        
        public static object Get(string key) {
            return FileBasedPrefs.Get(key);
        }

        [UnityEngine.Scripting.Preserve]
        public static ShareableSpriteReference GetSpriteRef(string key, ShareableSpriteReference fallback = null) {
            var json = GetString(key);
            if (string.IsNullOrWhiteSpace(json)) {
                return fallback;
            } else {
                return (ShareableSpriteReference) JsonUtility.FromJson(json, typeof(ShareableSpriteReference));
            }
        }
        
        [UnityEngine.Scripting.Preserve]
        public static T GetTemplate<T>(string key, T fallback = null) where T : Object, ITemplate {
            var guid = GetString(key);
            if (string.IsNullOrWhiteSpace(guid)) {
                return fallback;
            }

            var template = TemplatesUtil.Load<T>(guid);
            if (template is T casted) {
                return casted;
            }
            return fallback;
        }

        // === Setters
        public static void Set(string key, bool value, bool synchronize) {
            FileBasedPrefs.SetBool(key, value, synchronize);
        }

        public static void Set(string key, int value, bool synchronize) {
            FileBasedPrefs.SetInt(key, value, synchronize);
        }

        public static void Set(string key, string value, bool synchronize) {
            FileBasedPrefs.SetString(key, value, synchronize);
        }

        public static void Set(string key, float value, bool synchronize) {
            FileBasedPrefs.SetFloat(key, value, synchronize);
        }

        [UnityEngine.Scripting.Preserve]
        public static void Set(string key, ShareableSpriteReference value, bool synchronize) {
            var json = JsonUtility.ToJson(value);
            Set(key, json, synchronize);
        }
        
        [UnityEngine.Scripting.Preserve]
        public static void Set(string key, ITemplate value, bool synchronize) {
            if (string.IsNullOrWhiteSpace(value.GUID)) {
                return;
            }
            Set(key, value.GUID, synchronize);
        }
    }
}