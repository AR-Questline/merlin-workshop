using System;
using System.Collections.Generic;
using System.Linq;

namespace Awaken.TG.Main.Memories.FilePrefs {
    [Serializable]
    public class FileBasedPrefsSaveFileModel {
        public Dictionary<string, bool> boolData = new Dictionary<string, bool>();
        public Dictionary<string, float> floatData = new Dictionary<string, float>();
        public Dictionary<string, int> intData = new Dictionary<string, int>();
        public Dictionary<string, string> stringData = new Dictionary<string, string>();
        public IEnumerable<string> Keys => boolData.Keys.Concat(floatData.Keys).Concat(intData.Keys).Concat(stringData.Keys);

        public object GetValueFromKey(string key, object defaultValue) {
            if (defaultValue is string) {
                return stringData.ContainsKey(key) ? stringData[key] : defaultValue;
            }

            if (defaultValue is int) {
                return intData.ContainsKey(key) ? intData[key] : defaultValue;
            }

            if (defaultValue is float) {
                return floatData.ContainsKey(key) ? floatData[key] : defaultValue;
            }

            if (defaultValue is bool) {
                return boolData.ContainsKey(key) ? boolData[key] : defaultValue;
            }
        
            return defaultValue;
        }
        
        public object GetObjectValueFromKey(string key) {
            if (stringData.TryGetValue(key, out var resultString)) {
                return resultString;
            }

            if (intData.TryGetValue(key, out var resultInt)) {
                return resultInt;
            }
            
            if (floatData.TryGetValue(key, out var resultFloat)) {
                return resultFloat;
            }
            
            if (boolData.TryGetValue(key, out var resultBool)) {
                return resultBool;
            }

            return null;
        }

        public void UpdateOrAddData(string key, object value) {
            if (HasKey(key)) {
                SetValueForExistingKey(key, value);
            } else {
                SetValueForNewKey(key, value);
            }
        }

        void SetValueForNewKey(string key, object value) {
            if (value is string s) {
                stringData.Add(key, s);
            }

            if (value is int i) {
                intData.Add(key, i);
            }

            if (value is float f) {
                floatData.Add(key, f);
            }

            if (value is bool b) {
                boolData.Add(key, b);
            }
        }

        void SetValueForExistingKey(string key, object value) {
            if (value is string s) {
                stringData[key] = s;
            }

            if (value is int i) {
                intData[key] = i;
            }

            if (value is float f) {
                floatData[key] = f;
            }

            if (value is bool b) {
                boolData[key] = b;
            }
        }
        
        public void DeleteKey(string key) {
            stringData.Remove(key);
            intData.Remove(key);
            floatData.Remove(key);
            boolData.Remove(key);
        }

        public bool HasKey(string key) {
            return boolData.ContainsKey(key) || intData.ContainsKey(key) || floatData.ContainsKey(key) || stringData.ContainsKey(key);
        }
    }
}