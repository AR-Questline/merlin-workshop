using System;
using System.Globalization;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.General.Caches {
    public abstract class BaseCache : ScriptableObject {
        [SerializeField, HideInInspector] public long lastBakeTimestamp;

        [ShowInInspector, ReadOnly, PropertyOrder(-1)]
        public string LastBake => DateTime.FromBinary(lastBakeTimestamp).ToString(CultureInfo.CurrentCulture);

        public abstract void Clear();
        
        protected static T LoadFromResources<T>(string path) where T : Object {
            return Resources.Load<T>(path);
        }

#if UNITY_EDITOR
        protected static T LoadFromAssets<T>(string guid) where T : class {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<BaseCache>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid)) as T;
        }

        public void MarkBaked() {
            lastBakeTimestamp = DateTime.Now.ToBinary();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
        }
#else
        protected static T LoadFromAssets<T>(string guid) where T : class {
            return null;
        }
#endif
    }
}